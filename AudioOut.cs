using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
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
                _waveOut = new WaveOut();

            if (_audioProvider == null)
            {
                WaveFormat format = new WaveFormat(Const.SampleRateHz, 1);
                _audioProvider = new BufferedWaveProvider(format);

                _waveOut.Init(_audioProvider);
                _waveOut.Play();
            }

            _audioProvider.AddSamples(bytes, 0, bytes.Length);
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
