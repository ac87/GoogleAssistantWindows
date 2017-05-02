using System;
using NAudio.Wave;

namespace GoogleAssistantWindows
{
    public class AudioOut
    {
        private WaveOut _waveOut;
        private BufferedWaveProvider _audioProvider;

        public void Play(byte[] bytes)
        {
            if (_waveOut == null)
            {
                // Long responses need a large buffer, i.e. asking "Where is Google"
                // might be better to get all the bytes and play it without a BufferedWaveProvider 

                WaveFormat format = new WaveFormat(Const.SampleRateHz, 1);
                _audioProvider = new BufferedWaveProvider(format) { BufferDuration = TimeSpan.FromSeconds(15) };

                _waveOut = new WaveOut();                
                _waveOut.Init(_audioProvider);
            }

            if (_waveOut.PlaybackState != PlaybackState.Playing)
            {
                _waveOut.Play();                    
                // TODO figure out when the wave out stops getting data and stop it.
            }

            _audioProvider.AddSamples(bytes, 0, bytes.Length);
        }

        /// <summary>
        /// Clears the previous audio buffer for the new incoming speech.
        /// </summary>
        public void ClearPrevious()
        {
            if (_waveOut?.PlaybackState == PlaybackState.Playing)
                _waveOut.Stop();
            _audioProvider?.ClearBuffer();
        }

        public void PlayNotification()
        {
            PlayNotification(AppDomain.CurrentDomain.BaseDirectory + "Resources\\thegertz__notification-sound.wav");
        }

        public void PlayNegativeNotification()
        {
            PlayNotification(AppDomain.CurrentDomain.BaseDirectory + "Resources\\cameronmusic__oh-no-1.wav");
        }

        public void PlayNotification(string notificationFile)
        {
            if (_waveOut == null)
                _waveOut = new WaveOut();

            _waveOut.Init(new WaveFileReader(notificationFile));
            _waveOut.Play();
        }
    }
}
