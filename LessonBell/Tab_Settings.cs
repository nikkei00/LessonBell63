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
    // Вкладка настройки
    public partial class MainWindow
    {
        private string FolderMusic = ""; // Папка с музыкой (полный путь)
        private TimeSpan MinPlayMusicAfterLastBell = new TimeSpan(0, 6, 30); // Сколько минут играть музыку
        private string MelodyStartBell = ""; // Мелодия звонка на урок
        private string MelodyEndBell = ""; // Мелодия звонка с урока

        public List<double> EqFilters = new List<double>();
        public byte NeedSetVolumePC = 100; // Какую громкость компьютеру установить
        public int MusicStartBeforeBellEndEnded = 1500; // Старт музыки за сколько секунд до конца звучания звонка с урока

        private System.Windows.Forms.Timer timerLongPressNumeric = new System.Windows.Forms.Timer();
        private byte longT = 0;
        private bool UpOrDown = false;


        private void cbxAutoStart_Checked(object sender, RoutedEventArgs e) // Автозагрузка -- [ПОРЯДОК 22 МАРТА]
        {
            if (!NowLoadSettings)
            {
                if ((bool)cbxAutoStart.IsChecked)
                {
                    // включен
                    Log.Write("Создание ярлыка программы в папке 'Автозагрузка'");
                    try
                    {
                        WshShell shell = new WshShell();

                        //путь к ярлыку/создаем объект ярлыка
                        IWshShortcut shortcut = (IWshShortcut)shell.CreateShortcut(Environment.GetFolderPath(Environment.SpecialFolder.Startup) + @"\LessonBell.lnk");

                        //задаем свойства для ярлыка
                        //Рабочая папка
                        shortcut.WorkingDirectory = Environment.CurrentDirectory;

                        //описание ярлыка в всплывающей подсказке
                        shortcut.Description = "Ярлык LessonBell - Авто подача звонков и музыка на переменах";

                        //путь к самой программе
                        shortcut.TargetPath = System.Windows.Forms.Application.ExecutablePath;

                        //Создаем ярлык
                        shortcut.Save();
                        Thread.Sleep(20);
                        if (!System.IO.File.Exists(Environment.GetFolderPath(Environment.SpecialFolder.Startup) + @"\LessonBell.lnk"))
                        {
                            Log.Write("Ярлык в папке 'Автозагрузка' не был создан по какой то причине....");
                            Process.Start(Directory.GetCurrentDirectory());
                            Process.Start(Environment.GetFolderPath(Environment.SpecialFolder.Startup));
                            ShowMBinNewThread(MessageBoxIcon.Information, "Ярлык в папке 'Автозагрузка' не был создан!\n\nСоздайте ярлык для 'LessonBell' из папки приложения в папке Автозагрузка вручную!");
                        }
                        else
                        {
                            SettingsINI.WriteINI("Main", "AutoRun", cbxAutoStart.IsChecked.ToString());
                        }
                    }
                    catch (Exception l)
                    {
                        ShowErrorMB("Ошибка при создании ярлыка программы в папке 'Автозагрузка'", l.Message, l.ToString());
                    }
                }
                else
                {
                    // выключен
                    try
                    {
                        Log.Write("Удаление ярлыка программы из папки 'Автозагрузка'");
                        System.IO.File.Delete(Environment.GetFolderPath(Environment.SpecialFolder.Startup) + @"\LessonBell.lnk");
                        SettingsINI.WriteINI("Main", "AutoRun", "false");
                    }
                    catch (Exception l)
                    {
                        ShowErrorMB("Ошибка при удалении ярлыка программы из папки 'Автозагрузка'", l.Message, l.ToString());
                    }
                }
            }
        }

        private void cbxRunMinimized_Checked(object sender, RoutedEventArgs e) // Запускать свернутой -- [ПОРЯДОК 22 МАРТА]
        {
            if (!NowLoadSettings)
            {
                SettingsINI.WriteINI("Main", "RunMinimized", ((bool)cbxRunMinimized.IsChecked).ToString());
            }
        }
        
        private void btnEditMusicFolder_Click(object sender, RoutedEventArgs e) // Изменить папку с музыкой -- [ПОРЯДОК 19 МАРТА]
        {
            FolderBrowserDialog folderBrowserDialog1 = new FolderBrowserDialog();
            folderBrowserDialog1.Description = "Выберите папку с МУЗЫКОЙ для воспроизведения на переменах между занятиями";
            folderBrowserDialog1.ShowNewFolderButton = false;
            folderBrowserDialog1.RootFolder = Environment.SpecialFolder.MyComputer;
            

            if (folderBrowserDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string ReservFolder = FolderMusic;
                try
                {
                    FolderMusic = folderBrowserDialog1.SelectedPath;
                    Log.Write($"Изменена папка с музыкой на [{FolderMusic}]");
                    tbxMusicFolder.Text = Path.GetFileName(FolderMusic);
                    SettingsINI.WriteINI("Main", "FolderMusic", FolderMusic);
                }
                catch (Exception w)
                {
                    FolderMusic = ReservFolder;
                    tbxMusicFolder.Text = Path.GetFileName(FolderMusic);
                    SettingsINI.WriteINI("Main", "FolderMusic", ReservFolder);
                    ShowErrorMB("Ошибка при изменении папки с музыкой", w.Message, w.ToString());
                }
            }
        }

        #region Сколько минут играт после последнгего звонка
        private void btnMinMusUp_PreviewMouseDown(object sender, MouseButtonEventArgs e) // Плюс нажат
        {
            // click
            timerLongPressNumeric.Start();
            UpOrDown = true;
        }

        private void btnMinMusUp_PreviewMouseUp(object sender, MouseButtonEventArgs e) // Плюс отпущен
        {
            timerLongPressNumeric.Stop();
            longT = 0;
        }

        private void btnMinMusDown_PreviewMouseDown(object sender, MouseButtonEventArgs e)// Минус нажат
        {
            // click
            timerLongPressNumeric.Start();
            UpOrDown = false;
        }

        private void btnMinMusDown_PreviewMouseUp(object sender, MouseButtonEventArgs e)// Минус отпущен
        {
            timerLongPressNumeric.Stop();
            longT = 0;
        }

        private void btnMinMusUp_Click(object sender, RoutedEventArgs e) // Добавить время
        {
            byte current = byte.Parse(tbxMinMusAfterBells.Text);
            if (current <= 58 && current >= 0)
            {
                current++;
                tbxMinMusAfterBells.Text = current.ToString();
                MinPlayMusicAfterLastBell = new TimeSpan(0, int.Parse(tbxMinMusAfterBells.Text), 0);
                SettingsINI.WriteINI("Main", "MinPlayMusicAfterLastBell", MinPlayMusicAfterLastBell.Minutes.ToString());

                if (AllBells.Count > 0)
                    TimeOffMusic = AllBells[AllBells.Count - 1].TimeEnd + new TimeSpan(0, current, 0);
                CalcTimeYsilOnOff();
            }
        }

        private void btnMinMusDown_Click(object sender, RoutedEventArgs e) // Убрать время
        {
            byte current = byte.Parse(tbxMinMusAfterBells.Text);
            if (current <= 59 && current >= 1)
            {
                current--;
                tbxMinMusAfterBells.Text = current.ToString();
                MinPlayMusicAfterLastBell = new TimeSpan(0, int.Parse(tbxMinMusAfterBells.Text), 0);
                SettingsINI.WriteINI("Main", "MinPlayMusicAfterLastBell", MinPlayMusicAfterLastBell.Minutes.ToString());
                if (AllBells.Count > 0)
                    TimeOffMusic = AllBells[AllBells.Count - 1].TimeEnd + new TimeSpan(0, current, 0);
                CalcTimeYsilOnOff();
            }
        }

        private void TimerLongPressNumeric_Tick(object sender, EventArgs e) // Таймер долгого нажатия
        {
            longT++; // каждые 80 мс добавить 1

            if (longT > 5)
            {
                longT = 6; // чтобы не уйти за тип данных

                if (UpOrDown)
                {
                    // Plus
                    btnMinMusUp_Click(btnMinMusUp, null);
                }
                else
                {
                    // Minus
                    btnMinMusDown_Click(btnMinMusDown, null);
                }
            }
        }
        #endregion

        private void btnEditMelodyStart_Click(object sender, RoutedEventArgs e) // Изменить звонок НА урок -- [ПОРЯДОК 19 МАРТА]
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();

            openFileDialog1.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            openFileDialog1.Title = "Выберите мелодию звонка НА НАЧАЛО ЗАНЯТИЯ";
            openFileDialog1.Filter = "All files (*.*)|*.*|Аудио файлы (*.mp3, *.wav)|*.mp3;*.wav";
            openFileDialog1.InitialDirectory = FolderSelectedBells;
            openFileDialog1.FilterIndex = 2;
            openFileDialog1.Multiselect = false;

            if (!Directory.Exists(FolderSelectedBells))
            {
                Directory.CreateDirectory(FolderSelectedBells);
            }

            if (openFileDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string ReservMelody = MelodyStartBell;
                try
                {
                    if (openFileDialog1.FileName != System.IO.Path.GetFullPath(MelodyStartBell)) // если полный путь до нового файла равен пути старого файла
                    {
                        string Melody = FolderSelectedBells + @"\" + openFileDialog1.SafeFileName;
                        if (!System.IO.File.Exists(Melody)) // если этого файла еще нет в папке
                        {
                            System.IO.File.Copy(openFileDialog1.FileName, Melody, true); // скопировать с заменой
                        }
                        Thread.Sleep(50);

                        if (System.IO.File.Exists(Melody)) // если файл скопировался
                        {
                            ShowMBinNewThread(MessageBoxIcon.Information, 
                                "Выбранная Вами мелодия звонка была скопирована в директорию программы.\n\nВыбрано: " + openFileDialog1.SafeFileName);

                            MelodyStartBell = Melody; // Мелодия НА УРОК
                            tbxMelodyStart.Text = Path.GetFileName(MelodyStartBell);
                            Log.Write($"Изменена мелодия звонка на урок на [{System.IO.Path.GetFileName(MelodyStartBell)}]");
                            SettingsINI.WriteINI("Main", "MelodyStartBell", Melody);
                        }
                        else // Файл не скопировался
                        {
                            ShowMBinNewThread(MessageBoxIcon.Error,
                                "Файл звонка не скопировался в директорию программы!\nВозможно у программы нет доступа или файл занят другой программой.\nИзменения отменены.");
                        }
                    }
                    else
                    {
                        ShowMBinNewThread(MessageBoxIcon.Error,  "Вы выбрали уже выбранную мелодию!\nИзменения отменены.");
                    }
                }
                catch (Exception w)
                {
                    MelodyStartBell = ReservMelody;
                    tbxMelodyStart.Text = Path.GetFileName(MelodyStartBell);
                    SettingsINI.WriteINI("Main", "MelodyStartBell", ReservMelody);
                    ShowErrorMB("Ошибка при установке новой мелодии звонка НА НАЧАЛО ЗАНЯТИЯ", w.Message, w.ToString());
                }
            }
        }

        private void btnEditMelodyEnd_Click(object sender, RoutedEventArgs e) // Изменить звонок С урока -- [ПОРЯДОК 19 МАРТА]
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();

            openFileDialog1.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            openFileDialog1.Title = "Выберите мелодию звонка НА ОКОНЧАНИЕ ЗАНЯТИЯ";
            openFileDialog1.Filter = "All files (*.*)|*.*|Аудио файлы (*.mp3, *.wav)|*.mp3;*.wav";
            openFileDialog1.InitialDirectory = FolderSelectedBells;
            openFileDialog1.FilterIndex = 2;
            openFileDialog1.Multiselect = false;

            if (!Directory.Exists(FolderSelectedBells))
            {
                Directory.CreateDirectory(FolderSelectedBells);
            }

            if (openFileDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string ReservMelody = MelodyEndBell;
                try
                {
                    if (openFileDialog1.FileName != System.IO.Path.GetFullPath(MelodyEndBell)) // если полный путь до нового файла равен пути старого файла
                    {
                        string Melody = FolderSelectedBells + @"\" + openFileDialog1.SafeFileName;
                        if (!System.IO.File.Exists(Melody)) // если этого файла еще нет в папке
                        {
                            System.IO.File.Copy(openFileDialog1.FileName, Melody, true); // скопировать с заменой
                        }
                        Thread.Sleep(50);
                        if (System.IO.File.Exists(Melody)) // если файл скопировался
                        {
                            ShowMBinNewThread(MessageBoxIcon.Information,
                                "Выбранная Вами мелодия звонка была скопирована в директорию программы.\n\nВыбрано: " + openFileDialog1.SafeFileName);

                            MelodyEndBell = Melody; // Мелодия НА УРОК
                            tbxMelodyEnd.Text = Path.GetFileName(MelodyEndBell);
                            Log.Write($"Изменена мелодия звонка с урока на [{System.IO.Path.GetFileName(MelodyEndBell)}]");
                            SettingsINI.WriteINI("Main", "MelodyEndBell", Melody);
                            var source = CodecFactory.Instance.GetCodec(MelodyEndBell); // Считали информацию
                            DurationEndBell = source.GetLength();
                            Log.Write("Длительность мелодии звонка с урока: " + DurationEndBell);
                        }
                        else // Файл не скопировался
                        {
                            ShowMBinNewThread(MessageBoxIcon.Error,
                                "Файл звонка не скопировался в директорию программы!\nВозможно у программы нет доступа или файл занят другой программой.\nИзменения отменены.");
                        }
                    }
                    else
                    {
                        ShowMBinNewThread(MessageBoxIcon.Error, "Вы выбрали уже выбранную мелодию!\nИзменения отменены.");
                    }
                }
                catch (Exception w)
                {
                    MelodyEndBell = ReservMelody;
                    tbxMelodyEnd.Text = Path.GetFileName(MelodyEndBell);
                    SettingsINI.WriteINI("Main", "MelodyEndBell", ReservMelody);
                    ShowErrorMB("Ошибка при установке новой мелодии звонка НА ОКОНЧАНИЕ ЗАНЯТИЯ", w.Message, w.ToString());
                }
            }
        }


        private void btnBellStartNow_Click(object sender, RoutedEventArgs e) // Подать звонок НА урок -- [ПОРЯДОК 19 МАРТА]
        {
            Thread thread = new Thread(delegate () { PlayBell("на НАЧАЛО занятия, вручную", MelodyStartBell); });
            thread.IsBackground = true;
            thread.Start();
        }

        private void btnBellEndNow_Click(object sender, RoutedEventArgs e) // Подать звонок С урока -- [ПОРЯДОК 19 МАРТА]
        {
            Thread thread = new Thread(delegate () { PlayBell("на ОКОНЧАНИЕ занятия, вручную", MelodyEndBell); });
            thread.IsBackground = true;
            thread.Start();
        }

        private void btnSpecSettings_Click(object sender, RoutedEventArgs e) // Спец настройки - эквалайзер, громкости
        {
            Window_Settings amm = new Window_Settings(this, EqFilters, MusicStartBeforeBellEndEnded, NeedSetVolumePC);
            amm.Show();
        }
       }
}
