using System;
using CSCore;
using CSCore.CoreAudioAPI;
using CSCore.SoundOut;

namespace LessonBell
{
    class SysVolumeConfigurator
    {
        readonly MMDevice device;
        readonly AudioEndpointVolume endpointVolume;

        public SysVolumeConfigurator() // Объявление
        {
            using (var enumerator = new MMDeviceEnumerator())
            {
                device = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Console);
            }
            endpointVolume = AudioEndpointVolume.FromDevice(device);
        }

        public float Volume // Громкость - получить или задать
        {
            get { return endpointVolume.MasterVolumeLevelScalar; }
            set { endpointVolume.MasterVolumeLevelScalar = value; }
        }

        public void UnmuteAndSetVolume(int NeedVolume) // Размутить комп и установить нужную громкость
        {
            if (NeedVolume < 0 || NeedVolume > 100)
                return;

            if (endpointVolume.GetMute()) // если мут включен
            {
                endpointVolume.SetMute(false, Guid.Empty); // выключаем
            }

            endpointVolume.MasterVolumeLevelScalar = NeedVolume / 100.0f;
        }

        public bool Muted // Мут - получить или задать
        {
            get { return endpointVolume.GetMute(); }
            set { endpointVolume.SetMute(value, Guid.Empty); }
        }
        /*
        public int ChannelCount
        {
            get { return (int)endpointVolume.ChannelCount; }
        }

        public float GetChannelVolume(int channel)
        {
            return endpointVolume.GetChannelVolumeLevelScalar(channel);
        }

        public void SetChannelVolume(int channel, float value)
        {
            endpointVolume.SetChannelVolumeLevelScalar(channel, value, Guid.Empty);
        }

        
        ISoundOut CreateSoundOut(ref IWaveSource source)
        {
            ISoundOut soundOut;
            if (WasapiOut.IsSupportedOnCurrentPlatform)
                soundOut = new WasapiOut(true, AudioClientShareMode.Shared, 50);
            else
            {
                soundOut = new DirectSoundOut() { Latency = 100 };
                if (source.WaveFormat.BitsPerSample > 16)
                    source = source.ToSampleSource().ToWaveSource(16);
            }
            return soundOut;
        }

        protected void Dispose(bool disposing)
        {
            endpointVolume.Dispose();
            device.Dispose();

            //base.Dispose(disposing);
        } */
    }
}
