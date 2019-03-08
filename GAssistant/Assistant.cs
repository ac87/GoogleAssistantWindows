using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Google.Assistant.Embedded.V1Alpha2;
using Google.Protobuf;
using Grpc.Core;
using NAudio.Wave;
using static Google.Assistant.Embedded.V1Alpha2.ScreenOutConfig.Types;

namespace GoogleAssistantWindows
{
    public class Assistant
    {
        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        public delegate void DebugOutputDelegate(string debug, bool consoleOnly = false);
        public event DebugOutputDelegate OnDebug;

        public delegate void AssistantWorkingDelegate(AssistantState state);
        public event AssistantWorkingDelegate OnAssistantStateChanged;

        public delegate void AssistantDialogResult(string message);
        public event AssistantDialogResult OnAssistantDialogResult;

        public delegate void AssistantSpeechResult(string message);
        public event AssistantSpeechResult OnAssistantSpeechResult;
        
        private Channel _channel;
        private EmbeddedAssistant.EmbeddedAssistantClient _assistant;

        private IClientStreamWriter<AssistRequest> _requestStream;
        private IAsyncStreamReader<AssistResponse> _responseStream;

        // todo this doesn't seem to be needed anymore...
        private bool _writing;
        private readonly List<byte[]> _writeBuffer = new List<byte[]>();

        private WaveIn _waveIn;

        private readonly AudioOut _audioOut = new AudioOut();

        private readonly Settings settings;

        // todo tidy this mess of flags up
        private bool _requestStreamAvailable = false;
        private bool _assistantResponseReceived = false;
        private bool _sendSpeech = false;
        private bool _followOn = false;

        // If this documentation was a flow chart it would have been much better
        // https://developers.google.com/assistant/sdk/reference/rpc/google.assistant.embedded.v1alpha1#google.assistant.embedded.v1alpha1.EmbeddedAssistant

        public Assistant(Settings settings)
        {
            this.settings = settings;

            _audioOut.OnAudioPlaybackStateChanged += OnAudioPlaybackStateChanged;
        }        

        public void InitAssistantForUser(ChannelCredentials channelCreds)
        {
            _channel = new Channel(Const.AssistantEndpoint, channelCreds);
            _assistant = new EmbeddedAssistant.EmbeddedAssistantClient(_channel);
        }

        public async void NewConversation()
        {
            try
            {
                OnAssistantStateChanged?.Invoke(AssistantState.Listening);

                _followOn = false;
                _assistantResponseReceived = false;

                AsyncDuplexStreamingCall<AssistRequest, AssistResponse> assist = _assistant.Assist();

                _requestStream = assist.RequestStream;
                _responseStream = assist.ResponseStream;

                logger.Debug("New Conversation - New Config Request");
                OnDebug?.Invoke("New Conversation - New Config Request");

                // Once this opening request is issued if its not followed by audio an error of 'code: 14, message: Service Unavaible.' comes back, really not helpful Google!
                await _requestStream.WriteAsync(CreateNewRequest());

                _requestStreamAvailable = true;
                ResetSendingAudio(true);

                // note recreating the WaveIn each time otherwise the recording just stops on follow ups
                _waveIn = new WaveIn { WaveFormat = new WaveFormat(Const.SampleRateHz, 1) };
                _waveIn.DataAvailable += ProcessInAudio;
                _waveIn.StartRecording();

                await WaitForResponse();
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
                Console.WriteLine(ex.Message);
                OnDebug?.Invoke($"Error {ex.Message}");                
                StopRecording();
            }
        }       

        private AssistRequest CreateNewRequest()
        {
            // initial request is the config this then gets followed by all out audio

            var converseRequest = new AssistRequest();

            var audioIn = new AudioInConfig()
            {
                Encoding = AudioInConfig.Types.Encoding.Linear16,
                SampleRateHertz = Const.SampleRateHz
            };

            var audioOut = new AudioOutConfig()
            {
                Encoding = AudioOutConfig.Types.Encoding.Linear16,
                SampleRateHertz = Const.SampleRateHz,
                VolumePercentage = 75
            };

            var screenOut = new ScreenOutConfig()
            {
                ScreenMode = ScreenMode.Playing
            };

            DialogStateIn state = new DialogStateIn() { ConversationState = ByteString.Empty, LanguageCode = this.settings.LanguageCode };
            DeviceConfig device = new DeviceConfig() { DeviceModelId = this.settings.DeviceModelId,  DeviceId = this.settings.DeviceId };
            converseRequest.Config = new AssistConfig() { AudioInConfig = audioIn, AudioOutConfig = audioOut, DialogStateIn = state, DeviceConfig = device, ScreenOutConfig = screenOut };

            return converseRequest;
        }        

        private void StopRecording()
        {
            if (_waveIn != null)
            {
                logger.Debug("Stop Recording");
                OnDebug?.Invoke("Stop Recording");                
                _waveIn.StopRecording();
                _waveIn.Dispose();
                _waveIn = null;

                OnAssistantStateChanged?.Invoke(AssistantState.Processing);

                logger.Debug("Send Request Complete");
                OnDebug?.Invoke("Send Request Complete");
                _requestStreamAvailable = false;
                _requestStream.CompleteAsync();                
            }
        }

        private void ProcessInAudio(object sender, WaveInEventArgs e)
        {
            logger.Debug($"Process Audio {e.Buffer.Length} SendSpeech={_sendSpeech} Writing={_writing}");
            OnDebug?.Invoke($"Process Audio {e.Buffer.Length} SendSpeech={_sendSpeech} Writing={_writing}", true);

            if (_sendSpeech)
            {                
                // cannot do more than one write at a time so if its writing already add the new data to the queue
                if (_writing)
                    _writeBuffer.Add(e.Buffer);
                else
                    WriteAudioData(e.Buffer);
            }
        }

        private async Task WriteAudioData(byte[] bytes)
        {
            _writing = true;
            await WriteAudioIn(bytes);

            while (_writeBuffer.Count > 0)
            {
                var buffer = _writeBuffer[0];
                _writeBuffer.RemoveAt(0);

                if (_requestStreamAvailable && _sendSpeech)
                {
                    // don't write after the RequestComplete is sent or get an gRPC error.
                    await WriteAudioIn(buffer);
                }
            }

            _writing = false;
        }

        private async Task WriteAudioIn(byte[] buffer)
        {
            logger.Debug("Write Audio " + buffer.Length);
            OnDebug?.Invoke("Write Audio " + buffer.Length, true);
            var request = new AssistRequest() {AudioIn = ByteString.CopyFrom(buffer)};
            await _requestStream.WriteAsync(request);
        }

        private async Task WaitForResponse()
        {           
            var response = await _responseStream.MoveNext();
            if (response)
            {
                // multiple response elements are received per response, each can contain one of the Result, AudioOut or EventType fields
                AssistResponse currentResponse = _responseStream.Current;

                // Debug output the whole response, useful for.. debugging.
                logger.Debug(ResponseToOutput(currentResponse));
                OnDebug?.Invoke(ResponseToOutput(currentResponse));

                // EndOfUtterance, Assistant has recognised something so stop sending audio 
                if (currentResponse.EventType == AssistResponse.Types.EventType.EndOfUtterance)
                    ResetSendingAudio(false);

                if (currentResponse.AudioOut != null)
                    _audioOut.AddBytesToPlay(currentResponse.AudioOut.AudioData.ToByteArray());

                if(currentResponse.ScreenOut != null)
                {

                }

                if(currentResponse.SpeechResults.Count == 1 && currentResponse.SpeechResults[0].Stability == 1.0)
                {
                    OnAssistantSpeechResult?.Invoke(currentResponse.SpeechResults[0].Transcript);
                }

                if (currentResponse.DialogStateOut != null)
                {
                    // if the assistant has recognised something, flag this so the failure notification isn't played
                    if (!String.IsNullOrEmpty(currentResponse.DialogStateOut.SupplementalDisplayText))
                    {
                        _assistantResponseReceived = true;

                        OnAssistantDialogResult?.Invoke(currentResponse.DialogStateOut.SupplementalDisplayText);
                    }

                    switch (currentResponse.DialogStateOut.MicrophoneMode)
                    {
                        // this is the end of the current conversation
                        case DialogStateOut.Types.MicrophoneMode.CloseMicrophone:
                            StopRecording();

                            // play failure notification if nothing recognised.
                            if (!_assistantResponseReceived)
                            {
                                _audioOut.PlayNegativeNotification();
                                OnAssistantStateChanged?.Invoke(AssistantState.Inactive);
                            }
                            break;
                        case DialogStateOut.Types.MicrophoneMode.DialogFollowOn:
                            // stop recording as the follow on is in a whole new conversation, so may as well restart the same flow
                            StopRecording();
                            _followOn = true;
                            break;
                    }
                }

                await WaitForResponse();
            }
            else
            {
                logger.Debug("Response End");
                OnDebug?.Invoke("Response End");
                // if we've received any audio... play it.
                _audioOut.Play();
            }
        }

        private void ResetSendingAudio(bool send)
        {
            _writing = false;            
            _writeBuffer.Clear();
            _sendSpeech = send;
        }

        private void OnAudioPlaybackStateChanged(bool started)
        {
            if (started)
                OnAssistantStateChanged?.Invoke(AssistantState.Speaking);
            else
            {
                // stopped
                if (_followOn)
                    NewConversation();
                else
                    OnAssistantStateChanged?.Invoke(AssistantState.Inactive);
            }
        }

        public void Shutdown()
        {
            if (_channel != null)
            {
                _channel.ShutdownAsync();
                _requestStream = null;
                _responseStream = null;
                _assistant = null;
            }
        }

        private string ResponseToOutput(AssistResponse currentResponse)
        {
            if (currentResponse.AudioOut != null)
                return $"Response - AudioOut {currentResponse.AudioOut.AudioData.Length}";
            //if (currentResponse..Error != null)
            //    return $"Response - Error:{currentResponse.Error}";
            if (currentResponse.DialogStateOut != null)
                return $"Response - Result:{currentResponse.DialogStateOut}";
            if (currentResponse.EventType != AssistResponse.Types.EventType.Unspecified)
                return $"Response - EventType:{currentResponse.EventType}";

            return "Response Empty?";
        }

        public bool IsInitialised() => _assistant != null;
    }

    public enum AssistantState
    {
        Inactive,
        Processing,
        Listening,
        Speaking,
    }
}
