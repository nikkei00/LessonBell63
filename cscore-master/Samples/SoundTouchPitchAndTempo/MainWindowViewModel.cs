﻿using CSCore;
using CSCore.Codecs;
using CSCore.SoundOut;
using System.Windows.Forms;
using System.Windows.Input;

namespace SoundTouchPitchAndTempo
{
    public interface IMainWindowViewModel
    {
        ICommand OpenCommand { get; set; }
        ICommand PlayCommand { get; set; }
        ICommand StopCommand { get; set; }

        ICommand TempoUpCommand { get; set; }
        ICommand TempoCenterCommand { get; set; }
        ICommand TempoDownCommand { get; set; }

        ICommand PitchUpCommand { get; set; }
        ICommand PitchCenterCommand { get; set; }
        ICommand PitchDownCommand { get; set; }

        int TempoSliderValue { get; set; }
        int PitchSliderValue { get; set; }
        string TempoValue { get; }
        string PitchValue { get; }
    }

    public class MainWindowViewModel : PropertyChangedBase, IMainWindowViewModel
    {
        private ISoundOut _soundOut;
        private SoundTouchSource _soundTouchSource;

        public ICommand OpenCommand { get; set; }
        public ICommand PlayCommand { get; set; }
        public ICommand StopCommand { get; set; }

        public ICommand TempoUpCommand { get; set; }
        public ICommand TempoCenterCommand { get; set; }
        public ICommand TempoDownCommand { get; set; }

        public ICommand PitchUpCommand { get; set; }
        public ICommand PitchCenterCommand { get; set; }
        public ICommand PitchDownCommand { get; set; }

        private int _tempoSliderValue;
        public int TempoSliderValue
        {
            get
            {
                return _tempoSliderValue;
            }
            set
            {
                _tempoSliderValue = value;
                OnPropertyChanged();
                OnPropertyChanged("TempoValue");

                if(_soundTouchSource != null)
                {
                    _soundTouchSource.SetTempo(value);
                }
            }
        }

        private int _pitchSliderValue;
        public int PitchSliderValue
        {
            get
            {
                return _pitchSliderValue;
            }
            set
            {
                _pitchSliderValue = value;
                OnPropertyChanged();
                OnPropertyChanged("PitchValue");

                if(_soundTouchSource != null)
                {
                    _soundTouchSource.SetPitch(value / 2.0f);
                }
            }
        }

        public string TempoValue
        {
            get
            {
                return $"{TempoSliderValue}%";
            }
        }

        public string PitchValue
        {
            get
            {
                return $"{PitchSliderValue / 2.0f}";
            }
        }

        public MainWindowViewModel()
        {
            OpenCommand = new Command(OpenHandler);
            PlayCommand = new Command(PlayHandler);
            StopCommand = new Command(StopHandler);

            TempoUpCommand = new Command(TempoUpHandler);
            TempoCenterCommand = new Command(TempoCenterHandler);
            TempoDownCommand = new Command(TempoDownHandler);

            PitchUpCommand = new Command(PitchUpHandler);
            PitchCenterCommand = new Command(PitchCenterHandler);
            PitchDownCommand = new Command(PitchDownHandler);

            TempoSliderValue = 0;
            PitchSliderValue = 0;
        }

        private void OpenHandler()
        {
            var fileName = OpenFileDialog("MP3 Files|*.mp3");
            if(string.IsNullOrEmpty(fileName))
            {
                return;
            }

            var waveSource = CodecFactory.Instance.GetCodec(fileName)
                .AppendSource(x => new SoundTouchSource(x), out _soundTouchSource)
                .ToSampleSource()
                .ToWaveSource();

            _soundOut = new WasapiOut();
            _soundOut.Initialize(waveSource);

            TempoSliderValue = 0;
            PitchSliderValue = 0;
        }

        private void PlayHandler()
        {
            _soundOut.Play();
        }

        private void StopHandler()
        {
            _soundOut.Stop();
        }

        private string OpenFileDialog(string filter, string initialDirectory = "")
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = filter,
                InitialDirectory = initialDirectory
            };

            return openFileDialog.ShowDialog() == DialogResult.OK
                ? openFileDialog.FileName
                : string.Empty;
        }

        private void TempoUpHandler()
        {
            if(TempoSliderValue == 52)
            {
                return;
            }

            TempoSliderValue += 1;
        }

        private void TempoCenterHandler()
        {
            TempoSliderValue = 0;
        }

        private void TempoDownHandler()
        {
            if(TempoSliderValue == -52)
            {
                return;
            }

            TempoSliderValue -= 1;
        }

        private void PitchUpHandler()
        {
            if(PitchSliderValue == 12)
            {
                return;
            }

            PitchSliderValue += 1;
        }

        private void PitchCenterHandler()
        {
            PitchSliderValue = 0;
        }

        private void PitchDownHandler()
        {
            if(PitchSliderValue == -12)
            {
                return;
            }

            PitchSliderValue -= 1;
        }
    }
}