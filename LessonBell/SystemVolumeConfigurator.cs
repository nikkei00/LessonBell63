using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CSCore.CoreAudioAPI;

namespace LessonBell
{
    class SystemVolumeConfigurator
    { /*
        private readonly MMDeviceEnumerator _deviceEnumerator = new MMDeviceEnumerator();
        private readonly MMDevice _playbackDevice;

        public SystemVolumeConfigurator()
        {
            _playbackDevice = _deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
            //_playbackDevice = _deviceEnumerator.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eMultimedia);
        }

        



        public int GetVolume()
        {
            return (int)(_playbackDevice.AudioEndpointVolume.MasterVolumeLevelScalar * 100);
        }

        public void SetVolume(int volumeLevel)
        {
            


            if (volumeLevel < 0 || volumeLevel > 100)
                throw new ArgumentException("Volume must be between 0 and 100!");

            if (_playbackDevice.AudioEndpointVolume.Mute) // снимаем мут
            {
                _playbackDevice.AudioEndpointVolume.Mute = false;
            }

            _playbackDevice.AudioEndpointVolume.MasterVolumeLevelScalar = volumeLevel / 100.0f; // устанавливаем громкость

        }

        public bool GetMute()
        {
            return _playbackDevice.AudioEndpointVolume.Mute;
        }*/
    }
}
