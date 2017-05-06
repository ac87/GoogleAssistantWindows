using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Google.Assistant.Embedded.V1Alpha1;
using Google.Protobuf;
using Grpc.Core;
using NAudio.Wave;

namespace GoogleAssistantWindows
{
    public class Assistant
    {
        public delegate void DebugOutputDelegate(string debug, bool consoleOnly = false);
        public event DebugOutputDelegate OnDebug;

        public delegate void AssistantWorkingDelegate(AssistantState state);
        public event AssistantWorkingDelegate OnAssistantStateChanged;

        private Channel _channel;
        private EmbeddedAssistant.EmbeddedAssistantClient _assistant;

        private IClientStreamWriter<ConverseRequest> _requestStream;
        private IAsyncStreamReader<ConverseResponse> _responseStream;

        // todo this doesn't seem to be needed anymore...
        private bool _writing;
        private readonly List<byte[]> _writeBuffer = new List<byte[]>();

        private WaveIn _waveIn;

        private readonly AudioOut _audioOut = new AudioOut();

        // todo tidy this mess of flags up
        private bool _requestStreamAvailable = false;
        private bool _assistantResponseReceived = false;
        private bool _sendSpeech = false;
        private bool _followOn = false;

        // If this documentation was a flow chart it would have been much better
        // https://developers.google.com/assistant/sdk/reference/rpc/google.assistant.embedded.v1alpha1#google.assistant.embedded.v1alpha1.EmbeddedAssistant

        public Assistant()
        {
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

                AsyncDuplexStreamingCall<ConverseRequest, ConverseResponse> converse = _assistant.Converse();

                _requestStream = converse.RequestStream;
                _responseStream = converse.ResponseStream;

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
                Console.WriteLine(ex.Message);
                OnDebug?.Invoke($"Error {ex.Message}");                
                StopRecording();
            }
        }       

        private ConverseRequest CreateNewRequest()
        {
            // initial request is the config this then gets followed by all out audio

            var converseRequest = new ConverseRequest();

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

            ConverseState state = new ConverseState() { ConversationState = ByteString.Empty };
            converseRequest.Config = new ConverseConfig() { AudioInConfig = audioIn, AudioOutConfig = audioOut, ConverseState = state };

            return converseRequest;
        }        

        private void StopRecording()
        {
            if (_waveIn != null)
            {
                OnDebug?.Invoke("Stop Recording");                
                _waveIn.StopRecording();
                _waveIn.Dispose();
                _waveIn = null;

                OnAssistantStateChanged?.Invoke(AssistantState.Processing);

                OnDebug?.Invoke("Send Request Complete");
                _requestStreamAvailable = false;
                _requestStream.CompleteAsync();                
            }
        }

        private void ProcessInAudio(object sender, WaveInEventArgs e)
        {
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
            OnDebug?.Invoke("Write Audio " + buffer.Length, true);
            var request = new ConverseRequest() {AudioIn = ByteString.CopyFrom(buffer)};
            await _requestStream.WriteAsync(request);
        }

        private async Task WaitForResponse()
        {           
            var response = await _responseStream.MoveNext();
            if (response)
            {
                // multiple response elements are received per response, each can contain one of the Result, AudioOut or EventType fields
                ConverseResponse currentResponse = _responseStream.Current;

                // Debug output the whole response, useful for.. debugging.
                OnDebug?.Invoke(ResponseToOutput(currentResponse));

                // EndOfUtterance, Assistant has recognised something so stop sending audio 
                if (currentResponse.EventType == ConverseResponse.Types.EventType.EndOfUtterance)                
                    ResetSendingAudio(false);

                if (currentResponse.AudioOut != null)
                    _audioOut.Play(currentResponse.AudioOut.AudioData.ToByteArray());

                if (currentResponse.Result != null)
                {
                    // if the assistant has recognised something, flag this so the failure notification isn't played
                    if (!String.IsNullOrEmpty(currentResponse.Result.SpokenRequestText))
                        _assistantResponseReceived = true;

                    switch (currentResponse.Result.MicrophoneMode)
                    {                        
                        // this is the end of the current conversation
                        case ConverseResult.Types.MicrophoneMode.CloseMicrophone:
                            StopRecording();

                            // play failure notification if nothing recognised.
                            if (!_assistantResponseReceived)
                            {
                                _audioOut.PlayNegativeNotification();
                                OnAssistantStateChanged?.Invoke(AssistantState.Inactive);
                            }
                            break;
                        case ConverseResult.Types.MicrophoneMode.DialogFollowOn:
                            // stop recording as the follow on is in a whole new conversation, so may as well restart the same flow
                            StopRecording();
                            _followOn = true;
                            break;
                    }
                }

                await WaitForResponse();
            }
            else
                OnDebug?.Invoke("Response End");
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

        private string ResponseToOutput(ConverseResponse currentResponse)
        {
            if (currentResponse.AudioOut != null)
                return $"Response - AudioOut {currentResponse.AudioOut.AudioData.Length}";
            if (currentResponse.Error != null)
                return $"Response - Error:{currentResponse.Error}";
            if (currentResponse.Result != null)
                return $"Response - Result:{currentResponse.Result}";
            if (currentResponse.EventType != ConverseResponse.Types.EventType.Unspecified)
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
