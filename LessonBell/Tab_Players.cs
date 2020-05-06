using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using IWshRuntimeLibrary;
using System.Windows.Forms;
using System.Reflection;
using CSCore;
using CSCore.Streams.Effects;
using CSCore.SoundOut;
using CSCore.Codecs;
using System.Windows.Interop;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Json;
using System.Runtime.Serialization;

namespace LessonBell
{
    // Вкладка Плееры
    public partial class MainWindow
    {
        private bool ActiveMuzNaPeremenax = false;
        private bool ActiveDops = false;
        private bool ActiveOtherPlayer = false;
        private string FileOtherPlayer = ""; // Сторонний плеер (полный путь)

        Process MusicPlayer;
        bool MuzPlayerOn = false;

        private void MusicPlayer_Exited(object sender, EventArgs e)
        {
            MuzPlayerOn = false;
        }


        private int reservVolume = 30; // Резервная громкость (для плавного уменьшения и возвращения)
        private int reservVolumeForDop = 80;

        private void cbxMuzNaPeremenax_Checked(object sender, RoutedEventArgs e) // Активность музыки на переменах || ИЮЛЬСКОЕ
        {
            ActiveMuzNaPeremenax = (bool)cbxPlayMusic.IsChecked;
            
            if (!NowLoadSettings)
            {
                if (ActiveMuzNaPeremenax) // если включили
                {
                    OnMusicNow(); // проверить нужна ли музыка сейчас и включить
                }
                else
                {
                    StopAndClearPlayer(false); // убрать из плеера всё
                }
                SettingsINI.WriteINI("Main", "PlayMusic", ActiveMuzNaPeremenax.ToString());
            }
        }

        private void cbxOtherPlayer_Checked(object sender, RoutedEventArgs e) // Активность стороннего плеера || ИЮЛЬСКОЕ
        {
            btnChangeOtherPlayer.IsEnabled = tbxOtherPlayer.IsEnabled = ActiveOtherPlayer = (bool)cbxOtherPlayer.IsChecked;

            lbModeGetMusic.IsEnabled = comboModePlayMusic.IsEnabled = !ActiveOtherPlayer;

            if (!NowLoadSettings && AllBells.Count > 0)
            {
                CalcTimeYsilOnOff();
            }

            if (!NowLoadSettings)
            {
                if (ActiveOtherPlayer) // если включили
                {
                    StopAndClearPlayer(false); // убрать из плеера всё
                }
                else
                {
                    if (MuzPlayerOn)
                    {
                        MusicPlayer.CloseMainWindow();
                        MusicPlayer.Kill();
                    }
                }
                OnMusicNow(); // проверить нужна ли музыка сейчас и включить
                SettingsINI.WriteINI("Main", "ActiveOtherPlayer", ActiveOtherPlayer.ToString());
            }
        }

        private void cbxDops_Checked(object sender, RoutedEventArgs e) // Активность доп.сигналов || ИЮЛЬСКОЕ
        {
            ActiveDops = (bool)cbxPlayDops.IsChecked;

            if (!NowLoadSettings)
            {
                SettingsINI.WriteINI("Main", "PlayDops", ActiveDops.ToString());
            }

            if (!ActiveDops && axWmpDops.playState == WMPLib.WMPPlayState.wmppsPlaying) // если допы выключили и плеер играет
            {
                axWmpDops.Ctlcontrols.stop();
                axWmpDops.currentPlaylist = axWmpDops.playlistCollection.newPlaylist("NullLessonBellplst"); // впендюрили пустой плейлист
            }

        }

        private void btnChangeOtherPlayer_Click(object sender, RoutedEventArgs e) // Изменить сторонний плеер || ИЮЛЬСКОЕ
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();

            openFileDialog1.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            openFileDialog1.Title = "Выберите сторонний ПЛЕЕР МУЗЫКИ";
            openFileDialog1.Filter = "All files (*.*)|*.*|Плееры (*.exe)|*.exe";
            openFileDialog1.FilterIndex = 2;
            openFileDialog1.Multiselect = false;

            if (openFileDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                FileOtherPlayer = openFileDialog1.FileName;
                tbxOtherPlayer.Text = openFileDialog1.SafeFileName;
                SettingsINI.WriteINI("Main", "FileOtherPlayer", FileOtherPlayer);
            }
        }

        private void comboModePlayMusic_SelectionChanged(object sender, SelectionChangedEventArgs e) // Режим проигрывания музыки || ИЮЛЬСКОЕ
        {
            try
            {
                if (comboModePlayMusic.SelectedIndex == 0)
                {
                    axWmpMusic.settings.setMode("shuffle", true); // Рандом ВКЛ
                    if (!NowLoadSettings)
                        SettingsINI.WriteINI("Main", "ModePlayMusic", "0");
                }
                else
                {
                    axWmpMusic.settings.setMode("shuffle", false); // Рандом ВЫКЛ
                    if (!NowLoadSettings)
                        SettingsINI.WriteINI("Main", "ModePlayMusic", "1");
                }
            }
            catch
            {
                Log.Write(DateTime.Now.ToLongTimeString() + "  >> error set player random mode");
            }
        }

        private void AxWmpMusic_PlayStateChange(object sender, AxWMPLib._WMPOCXEvents_PlayStateChangeEvent e) // || ИЮЛЬСКОЕ
        {
            if (e.newState == 3) // если новый статус - проигрывание
            {
                // Записать название проигрываемой песни в файл
                PlstLog.Write("New Playing: " + Path.GetFileName((sender as AxWMPLib.AxWindowsMediaPlayer).currentMedia.sourceURL));
            }
          /*  if (sender == axWmpDops)
            {
                switch (e.newState)
                {
                    case 0:    // Undefined
                        PlstLog.Write("[DOPS PLAYER] - Undefined");
                        break;

                    case 1:    // Stopped
                        PlstLog.Write("[DOPS PLAYER] - Stopped");
                        break;

                    case 2:    // Paused
                        PlstLog.Write("[DOPS PLAYER] - Paused");
                        break;

                    case 3:    // Playing
                        PlstLog.Write("[DOPS PLAYER] - Playing");
                        break;

                    case 4:    // ScanForward
                        PlstLog.Write("[DOPS PLAYER] - ScanForward");
                        break;

                    case 5:    // ScanReverse
                        PlstLog.Write("[DOPS PLAYER] - ScanReverse");
                        break;

                    case 6:    // Buffering
                        PlstLog.Write("[DOPS PLAYER] - Buffering");
                        break;

                    case 7:    // Waiting
                        PlstLog.Write("[DOPS PLAYER] - Waiting");
                        break;

                    case 8:    // MediaEnded
                        PlstLog.Write("[DOPS PLAYER] - MediaEnded");
                        break;

                    case 9:    // Transitioning
                        PlstLog.Write("[DOPS PLAYER] - Transitioning");
                        break;

                    case 10:   // Ready
                        PlstLog.Write("[DOPS PLAYER] - Ready");
                        break;

                    case 11:   // Reconnecting
                        PlstLog.Write("[DOPS PLAYER] - Reconnecting");
                        break;

                    case 12:   // Last
                        PlstLog.Write("[DOPS PLAYER] - Last");
                        break;

                    default:
                        PlstLog.Write("[DOPS PLAYER] - Unknown State: " + e.newState.ToString());
                        break;
                }
            }*/
        }
    }
}
