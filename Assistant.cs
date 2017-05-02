using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Timers;
using Google.Assistant.Embedded.V1Alpha1;
using Google.Protobuf;
using Grpc.Core;
using NAudio.Wave;

namespace GoogleAssistantWindows
{
    public class Assistant
    {
        public delegate void DebugOutputDelegate(string debug);
        public event DebugOutputDelegate OnDebug;

        public delegate void StoppedListeningDelegate();
        public event StoppedListeningDelegate OnStoppedListening;

        private Channel _channel;
        private EmbeddedAssistant.EmbeddedAssistantClient _assistant;

        private IClientStreamWriter<ConverseRequest> _requestStream;
        private IAsyncStreamReader<ConverseResponse> _responseStream;

        private bool _writing;
        private readonly List<byte[]> _writeBuffer = new List<byte[]>();

        private WaveIn _waveIn;

        private Timer _recordTimer;

        private readonly AudioOut _audioOut = new AudioOut();

        private bool _requestStreamAvailable = false;

        public void InitAssistantForUser(ChannelCredentials channelCreds)
        {
            _channel = new Channel(Const.AssistantEndpoint, channelCreds);
            _assistant = new EmbeddedAssistant.EmbeddedAssistantClient(_channel);
        }

        public async void NewConversation()
        {
            try
            {
                _audioOut.ClearPrevious();

                AsyncDuplexStreamingCall<ConverseRequest, ConverseResponse> converse = _assistant.Converse();

                _requestStream = converse.RequestStream;
                _responseStream = converse.ResponseStream;

                OnDebug?.Invoke("New Request");

                // Once this opening request is issued if its not followed by audio an error of 'code: 14, message: Service Unavaible.' comes back, really not helpful Google!
                await _requestStream.WriteAsync(CreateNewRequest());

                _requestStreamAvailable = true;

                _waveIn = new WaveIn { WaveFormat = new WaveFormat(Const.SampleRateHz, 1) };            
                _waveIn.DataAvailable += ProcessInAudio;
                _waveIn.StartRecording();

                _recordTimer = new Timer { Interval = Const.MaxRecordMillis }; // stop recording after a given amount of time
                _recordTimer.Elapsed += (sender, args) =>
                {
                    OnDebug?.Invoke("Max Record Time Reached");
                    StopRecording();
                };
                _recordTimer.Start();

                await WaitForResponse();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                OnDebug?.Invoke($"Error {ex.Message}");

                OnDebug?.Invoke("Stopping WaveIn");
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
                _waveIn = null;
                _recordTimer.Stop();

                OnStoppedListening?.Invoke();

                OnDebug?.Invoke("Send Request Complete");
                _requestStreamAvailable = false;
                _requestStream.CompleteAsync();                
            }
        }

        private void ProcessInAudio(object sender, WaveInEventArgs e)
        {
            // cannot do more than one write at a time so if its writing already add the new data to the queue
            if (_writing)
                _writeBuffer.Add(e.Buffer);
            else
                WriteAudioData(ByteString.CopyFrom(e.Buffer));
        }

        private async Task WriteAudioData(ByteString bytes)
        {
            _writing = true;
            var request = new ConverseRequest() { AudioIn = bytes };

            await _requestStream.WriteAsync(request);

            while (_writeBuffer.Count > 0)
            {
                var buffer = _writeBuffer[0];
                _writeBuffer.RemoveAt(0);

                if (_requestStreamAvailable)
                {
                    // don't write after the RequestComplete is sent or get an gRPC error.
                    request = new ConverseRequest() {AudioIn = ByteString.CopyFrom(buffer)};
                    await _requestStream.WriteAsync(request);
                }
            }

            _writing = false;
        }

        private bool _nonCloseResponseReceived = false;

        private async Task WaitForResponse()
        {           
            var response = await _responseStream.MoveNext();
            if (response)
            {
                ConverseResponse currentResponse = _responseStream.Current;

                OnDebug?.Invoke(ResponseToOutput(currentResponse));

                if (!String.IsNullOrEmpty(currentResponse.Result?.SpokenRequestText))
                    _nonCloseResponseReceived = true; // if the assistant has recognised something this stops the failure notification playing

                if (currentResponse.AudioOut != null)
                    _audioOut.Play(currentResponse.AudioOut.AudioData.ToByteArray());

                if ((currentResponse.Result != null && currentResponse.Result.MicrophoneMode == ConverseResult.Types.MicrophoneMode.CloseMicrophone)
                    || currentResponse.EventType == ConverseResponse.Types.EventType.EndOfUtterance)
                    StopRecording();

                if (currentResponse.Result != null && currentResponse.Result.MicrophoneMode == ConverseResult.Types.MicrophoneMode.CloseMicrophone)
                {
                    // play failure notification if nothing recognised.
                    if (!_nonCloseResponseReceived)
                        _audioOut.PlayNegativeNotification(); 
                }            

                await WaitForResponse();
            }
            else
                OnDebug?.Invoke("Response End");
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
                return "Response - AudioOut {currentResponse.AudioOut.AudioData.Length}";
            if (currentResponse.Error != null)
                return "Response - Error:{currentResponse.Error}";
            if (currentResponse.Result != null)
                return "Response - Result:{currentResponse.Result}";
            if (currentResponse.EventType != ConverseResponse.Types.EventType.Unspecified)
                return "Response - EventType:{currentResponse.EventType}";

            return "Response Empty?";
        }

        public bool IsInitialised() => _assistant != null;
    }
}
