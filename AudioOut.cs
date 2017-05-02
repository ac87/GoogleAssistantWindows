using System;
using System.IO;
using NAudio.Wave;

namespace GoogleAssistantWindows
{
    public class AudioOut
    {
        private static readonly WaveFormat WaveFormat = new WaveFormat(Const.SampleRateHz, 1);

        private WaveOut _waveOut;

        private MemoryStream _ms;
        private RawSourceWaveStream _waveStream;

        public void Play(byte[] bytes)
        {
            if (_waveOut == null)
            {
                _waveOut = new WaveOut();
                _waveOut.PlaybackStopped += (sender, args) =>
                {
                    _waveStream.Dispose();
                    _ms.Dispose();
                    _ms = null;
                };
            }
            if (_ms == null)
                _ms = new MemoryStream();

            _ms.Write(bytes, 0, bytes.Length);

            // cheat, I know at the bitrate requested it splits it in this size chunks, if its not this size its the end
            if (bytes.Length != 1600) 
            {
                _ms.Position = 0;
                _waveStream = new RawSourceWaveStream(_ms, WaveFormat);
                _waveOut.Init(_waveStream);
                _waveOut.Play();                                
            }
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
