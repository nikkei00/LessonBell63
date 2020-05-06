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
    /// <summary>
    /// Логика взаимодействия для Window_Settings.xaml
    /// </summary>
    public partial class Window_Settings : Window
    {
        private bool NowLoadSett = true;

        private int RezervMs = 0;
        private byte RezervVolumePC = 0;
        private List<double> RezervEq = new List<double>();
        MainWindow mainForm;
        public Window_Settings(MainWindow f, List<double> EqualizerFilters, int MsBeforeEndBell, byte SetVolumePC)
        {
            InitializeComponent();

            this.Closing += Window_Settings_Closing;

            this.Owner = f;
            mainForm = Owner as MainWindow;

            mainForm.btnSpecSettings.IsEnabled = false;

            RezervMs = MsBeforeEndBell;
            RezervVolumePC = SetVolumePC;
            
            for (int i = 0; i < EqualizerFilters.Count; i++)
            {
                RezervEq.Add(EqualizerFilters[i]);
            }
            tbxMsBeforeMusic.Text = MsBeforeEndBell.ToString();
            tbxVolumePC.Text = SetVolumePC.ToString();

            s0.Value = (EqualizerFilters[0] * 100) / 30;
            s1.Value = (EqualizerFilters[1] * 100) / 30;
            s2.Value = (EqualizerFilters[2] * 100) / 30;
            s3.Value = (EqualizerFilters[3] * 100) / 30;
            s4.Value = (EqualizerFilters[4] * 100) / 30;
            s5.Value = (EqualizerFilters[5] * 100) / 30;
            s6.Value = (EqualizerFilters[6] * 100) / 30;
            s7.Value = (EqualizerFilters[7] * 100) / 30;
            s8.Value = (EqualizerFilters[8] * 100) / 30;
            s9.Value = (EqualizerFilters[9] * 100) / 30;

            NowLoadSett = false;
        }

        private void Window_Settings_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //mainForm.EqFilters = RezervEq;
            mainForm.btnSpecSettings.IsEnabled = true;
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            var slider = sender as Slider;
            if (slider != null && !NowLoadSett)
            {
                double value = (slider.Value / 100) * 30;
                //the tag of the trackbar contains the index of the filter
                int filterIndex = int.Parse((string)slider.Tag);
                value = Math.Round(value, 3);
                mainForm.EqFilters[filterIndex] = value;

                if (mainForm._equalizer != null)
                {
                    EqualizerFilter filter2 = mainForm._equalizer.SampleFilters[filterIndex];
                    filter2.AverageGainDB = value;
                }
            }
        }
        

        private void tbxMsBeforeMusic_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = "0123456789".IndexOf(e.Text) < 0;
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (tbxMsBeforeMusic.Text.Length < 0 || (int.Parse(tbxMsBeforeMusic.Text) > 5000 || int.Parse(tbxMsBeforeMusic.Text) < 0) ||
                tbxVolumePC.Text.Length < 0 || (byte.Parse(tbxVolumePC.Text) > 100 || byte.Parse(tbxVolumePC.Text) < 0))
            {
                System.Windows.Forms.MessageBox.Show("Некорректный ввод данных!\n\nЛибо количество миллисекунд старта музыки больше 5000\n\nЛибо заданная громкость компьютера меньше 0 или больше 100", "LessonBell - Подача звонков и музыка на переменах", MessageBoxButtons.OK, MessageBoxIcon.Exclamation,
                MessageBoxDefaultButton.Button1, System.Windows.Forms.MessageBoxOptions.ServiceNotification);
            }
            else
            {
                mainForm.MusicStartBeforeBellEndEnded = int.Parse(tbxMsBeforeMusic.Text);
                mainForm.NeedSetVolumePC = byte.Parse(tbxVolumePC.Text);
                mainForm.SaveSettingsEqualizerBells();
                mainForm.btnSpecSettings.IsEnabled = true;
                Close();
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            mainForm.MusicStartBeforeBellEndEnded = RezervMs;
            mainForm.NeedSetVolumePC = RezervVolumePC;
            if (mainForm._equalizer != null)
            {
                for (int i = 0; i < RezervEq.Count; i++)
                {
                    EqualizerFilter filter = mainForm._equalizer.SampleFilters[i];
                    filter.AverageGainDB = RezervEq[i];
                }
            }
            mainForm.EqFilters = RezervEq;
            mainForm.btnSpecSettings.IsEnabled = true;
            Close();
        }
        

        private void btn0_Click(object sender, RoutedEventArgs e)
        {
            s0.Value = 0;
        }

        private void btn1_Click(object sender, RoutedEventArgs e)
        {
            s1.Value = 0;
        }

        private void btn2_Click(object sender, RoutedEventArgs e)
        {
            s2.Value = 0;
        }

        private void btn3_Click(object sender, RoutedEventArgs e)
        {
            s3.Value = 0;
        }

        private void btn4_Click(object sender, RoutedEventArgs e)
        {
            s4.Value = 0;
        }

        private void btn5_Click(object sender, RoutedEventArgs e)
        {
            s5.Value = 0;
        }

        private void btn6_Click(object sender, RoutedEventArgs e)
        {
            s6.Value = 0;
        }

        private void btn7_Click(object sender, RoutedEventArgs e)
        {
            s7.Value = 0;
        }

        private void btn8_Click(object sender, RoutedEventArgs e)
        {
            s8.Value = 0;
        }

        private void btn9_Click(object sender, RoutedEventArgs e)
        {
            s9.Value = 0;
        }
    }
}
