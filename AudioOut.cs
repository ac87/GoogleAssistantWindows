using System;
using System.IO;
using System.Media;
using System.Resources;
using System.Windows.Media;
using NAudio.Wave;

namespace GoogleAssistantWindows
{
    public class AudioOut
    {
        public delegate void AudioPlaybackStateChangeDelegate(bool started);
        public event AudioPlaybackStateChangeDelegate OnAudioPlaybackStateChanged;

        private static readonly WaveFormat WaveFormat = new WaveFormat(Const.SampleRateHz, 1);

        private WaveOut _waveOut;

        private MemoryStream _ms;
        private RawSourceWaveStream _waveStream;       

        public void AddBytesToPlay(byte[] bytes)
        {
            if (_waveOut == null)
            {
                _waveOut = new WaveOut();
                _waveOut.PlaybackStopped += (sender, args) =>
                {
                    _waveStream.Dispose();
                    _ms.Dispose();
                    _ms = null;

                    OnAudioPlaybackStateChanged?.Invoke(false);
                };
            }
            if (_ms == null)
                _ms = new MemoryStream();

            _ms.Write(bytes, 0, bytes.Length);            
        }

        public void Play()
        {
            if (_ms != null && _ms.Length > 0)
            {
                _ms.Position = 0;
                _waveStream = new RawSourceWaveStream(_ms, WaveFormat);
                _waveOut.Init(_waveStream);
                _waveOut.Play();

                OnAudioPlaybackStateChanged?.Invoke(true);
            }
        }

        public void PlayNotification()
        {
            PlayNotification(AppDomain.CurrentDomain.BaseDirectory + "Resources\\positive.wav");
        }

        public void PlayNegativeNotification()
        { 
            PlayNotification(AppDomain.CurrentDomain.BaseDirectory + "Resources\\negative.wav");
        }

        public void PlayNotification(string notificationFile)
        {
            using (var soundPlayer = new SoundPlayer(notificationFile))
            {
                soundPlayer.Play();
            }            
        }
    }
}
