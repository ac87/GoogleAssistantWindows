using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
    }
}
