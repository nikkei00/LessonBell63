using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Forms;
using CSCore;
using CSCore.Streams.Effects;
using CSCore.SoundOut;
using CSCore.Codecs;
using System.Collections.ObjectModel;
using System.Runtime.Serialization.Json;
using Microsoft.Win32;
using System.Windows.Input;
using System.Drawing;

namespace LessonBell
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // ------------------
        #region Объявление переменных
        Bell NextBell;
        Bell PrevBell;
        private string FolderSettings = Directory.GetCurrentDirectory() + @"\Settings"; // Папка настройки
        private IniFile SettingsINI = new IniFile(Directory.GetCurrentDirectory() + @"\Settings\mainSettings.ini"); // Файл с настройками
        private string FolderSelectedBells = Directory.GetCurrentDirectory() + @"\SelectedBells"; // Папка звонки
        private string FolderSelectedDops = Directory.GetCurrentDirectory() + @"\SelectedDops"; // Папка Доп сигналы
        private DataContractJsonSerializer jsonSerializerRasps = new DataContractJsonSerializer(typeof(ObservableCollection<RaspZvonkov>));
        public ObservableCollection<RaspZvonkov> AllRasps = new ObservableCollection<RaspZvonkov>(); // Список расписаний звонков
        public ObservableCollection<RaspLesson> AllBells = new ObservableCollection<RaspLesson>(); // Список звонков на сегодня
        public ObservableCollection<DopSignal> AllDops = new ObservableCollection<DopSignal>(); // Список доп.сигналов на сегодня
        public List<MuzDoPar> MusicDoPar = new List<MuzDoPar>(); // Сегодняшняя музыка до уроков
        public RaspZvonkov RaspVr = new RaspZvonkov(); // создали расписание с значениями по умолчанию

        System.Windows.Threading.DispatcherTimer timer = new System.Windows.Threading.DispatcherTimer(); // Главный таймер
        
        private const double MaxDB = 40; // Эквалайзер понижение\повышение частот
        public Equalizer _equalizer; // Эквалайзер мелодий звонков
        private ISoundOut _soundOut; // Аудио выход мелодий звонков
        private TimeSpan DurationEndBell = new TimeSpan(0, 0, 8); // Длительность звучания мелодии звонка

        public Logger Log; // Ведение лога
        private Logger PlstLog; // Лог на плейлист
        
        private double totalDlitDops = 0; // Общая длительность доп.сигналов
        bool CloseWithoutQuetsion = false;
        private TimeSpan TimeOffMusic = new TimeSpan(0, 0, 0); // задается но не используется
        
        public bool NowLoadSettings = true;
        private bool NowZvonitZvonok = false;

        private TimeSpan TimeNow = TimeSpan.Parse(DateTime.Now.ToLongTimeString());
        private string Date = DateTime.Now.ToShortDateString();
        private string DateNow = "";
        private System.Timers.Timer timerEndMusic = new System.Timers.Timer();
        #endregion
        // ------------------


        public delegate void SettingsLoaded();
        public event SettingsLoaded onSettingsLoaded; // событие при изменении 

        SettingsLoaded Loaded;

        public MainWindow() // К О Н С Т Р У К Т О Р
        {
            //Loaded = Loadedje;SettingsLoaded Loadedje

            AppDomain currentDomain = AppDomain.CurrentDomain;
            currentDomain.UnhandledException += new UnhandledExceptionEventHandler(MyHandler);

            string thisprocessname = Process.GetCurrentProcess().ProcessName;

            if (Process.GetProcesses().Count(p => p.ProcessName == thisprocessname) > 1)
            {
                ShowMBinNewThread(MessageBoxIcon.Asterisk, "LessonBell уже запущена! Скорее всего, программа свернута в трей!");
                CloseWithoutQuetsion = true;
                Close();
                return;
            }
            try
            {
                InitializeComponent(); // Инициализация всех объектов на форме
                Hide();
                CheckFoldersSettingsDops();
                NewLoadSettings();
                CheckFolderBells();
                SetNotifyIcon(); // Установка значка в трее
                InitializeTimersAndSettings();
            }
            catch (Exception e)
            {
                ShowErrorMB("Ошибка при инициализации", e.Message, e.ToString());
                Close();
            }
        }

        static void MyHandler(object sender, UnhandledExceptionEventArgs args) // [2020] Ловим критическую ошибку
        {
            Exception e = (Exception)args.ExceptionObject;
            
            new System.Threading.Thread(delegate (object obj) {
                
                System.Windows.MessageBox.Show($"CRITICAL ERROR\n-\n {e.Message} \n-\n {e.ToString()}", "LessonBell - CRITICAL", MessageBoxButton.OK);
                
            }).Start();

            
            System.Windows.Forms.Application.Restart();
            System.Environment.Exit(1);
        }

        private void SystemEvents_SessionEnding(object sender, SessionEndingEventArgs e) // [2020] Ловим выключение компа и выводим страшное сообщение
        {
            e.Cancel = true; // задержали выключение компа
            
            WindowExitQuestion weq = new WindowExitQuestion();
            if ((bool)weq.ShowDialog())
            {
                // Выйти из программы
                CloseWithoutQuetsion = true;
                Close();
            }
        }


        private void TimerEndMusic_Tick(object sender, EventArgs e) // Таймер выключения музыки через 7 мин
        {
            timerEndMusic.Enabled = false;
            StopAndClearPlayer(true);
            
            if (MuzPlayerOn)
            {
                MusicPlayer.CloseMainWindow();
                MuzPlayerOn = false;
            }
        }

        private void timerTick(object sender, EventArgs e) // Главный таймер
        {
            DateNow = lbCurrentDate.Text = DateTime.Now.ToShortDateString();
            labelCurrentTime.Text = lbCurrentTime.Text = DateTime.Now.ToString("HH:mm:ss");
            TimeNow = TimeSpan.Parse(DateTime.Now.ToLongTimeString());

            #region Дата изменилась - Пересобрать GetTimeBells
            if (Date != DateNow)
            {
                // Дата изменилась
                Date = DateNow;
                GetTimeBells("Дата изменилась");
            }
            #endregion


            if (AllBells.Count > 0 || AllDops.Count > 0) // если сегодня работать
            {
                ControlYsil();
                if (AllBells.Count > 0)
                {
                    NewCheckPlayBell();
                }

                if (MusicDoPar.Count > 0)
                {
                    ChechMuzDoYrokov(); // Проверка на музыку перед уроками
                }

                if (AllDops.Count > 0 && ActiveDops) // Если нужно давать доп сигналы
                {
                    CheckNaDopSignal(); // Проверка на доп.сигналы
                }
            }
            else
            {
                if (workComPort != null && StateYsil && (YsilAuto || YsilNoControl)) // Если усилитель включен и стоит автоматический режим или не управлять (чтобы оффнуть)
                {
                    OffYsil("Сегодня нет звонков");
                }
            }
        }

        // -----------------------------------
        #region Загрузка и сохранение настроек
        private void CheckFoldersSettingsDops()
        {
            if (!Directory.Exists(FolderSettings)) // если папки нет НАСТРОЙКИ
            {
                Directory.CreateDirectory(FolderSettings); // создали папку
            }

            if (!System.IO.File.Exists(FolderSettings + @"\Log.txt")) // если файла нет ЛОГ
            {
                System.IO.File.Create(FolderSettings + @"\Log.txt").Close(); // создать файл
            }
            Log = new Logger(FolderSettings + @"\Log.txt"); // работаем с этим файлом
            Log.onWrited += Log_Writed;

            if (!Directory.Exists(FolderSelectedDops)) // если папки нет ВЫБРАННЫЕ ДОП
            {
                Directory.CreateDirectory(FolderSelectedDops); // создали папку
            }

            if (!System.IO.File.Exists(FolderSettings + @"\mainSettings.ini")) // если файла нет ГЛАВН НАСТРОЙКИ
            {
                System.IO.File.Create(FolderSettings + @"\mainSettings.ini").Close(); // создать файл
                SettingsINI = new IniFile(FolderSettings + @"\mainSettings.ini");
                SettingsINI.WriteINI("Main", "PlayMusic", "false");
                SettingsINI.WriteINI("Main", "VolumeMusic", "35");
                SettingsINI.WriteINI("Main", "ModePlayMusic", "0");
                SettingsINI.WriteINI("Main", "FolderMusic", "Не выбрана");
                SettingsINI.WriteINI("Main", "PlayDops", "false");
                SettingsINI.WriteINI("Main", "VolumeDops", "85");
                SettingsINI.WriteINI("Main", "AutoRun", "false");
                SettingsINI.WriteINI("Main", "RunMinimized", "false");
                SettingsINI.WriteINI("Main", "MelodyStartBell", FolderSelectedBells + @"\Dzz_Niz_10sek.mp3");
                SettingsINI.WriteINI("Main", "MelodyEndBell", FolderSelectedBells + @"\Dzz_Niz_7sek.mp3");
                SettingsINI.WriteINI("Main", "MinPlayMusicAfterLastBell", "9");
                SettingsINI.WriteINI("Main", "ActiveOtherPlayer", "false");
                SettingsINI.WriteINI("Main", "FileOtherPlayer", "Не выбран");
                SettingsINI.WriteINI("Main", "YsilNoControl", "false");
                SettingsINI.WriteINI("Main", "YsilAuto", "true");
                SettingsINI.WriteINI("Main", "YsilHands", "false");
                SettingsINI.WriteINI("Main", "YsilTime", "false");
                SettingsINI.WriteINI("Main", "YsilFixTimeOn", "07:59");
                SettingsINI.WriteINI("Main", "YsilFixTimeOff", "17:10");
                SettingsINI.WriteINI("Main", "SelectedCOM", "null");
                SettingsINI.WriteINI("Main", "NeedSetVolumePC", "100");
                SettingsINI.WriteINI("Main", "StartMusicBeforeEndBell", "1500");
                SettingsINI.WriteINI("EqualizerBells", "filter0", "-20");
                SettingsINI.WriteINI("EqualizerBells", "filter1", "-10");
                SettingsINI.WriteINI("EqualizerBells", "filter2", "0");
                SettingsINI.WriteINI("EqualizerBells", "filter3", "0");
                SettingsINI.WriteINI("EqualizerBells", "filter4", "0");
                SettingsINI.WriteINI("EqualizerBells", "filter5", "0");
                SettingsINI.WriteINI("EqualizerBells", "filter6", "0");
                SettingsINI.WriteINI("EqualizerBells", "filter7", "0");
                SettingsINI.WriteINI("EqualizerBells", "filter8", "0");
                SettingsINI.WriteINI("EqualizerBells", "filter9", "0");
            }

            

            if (!System.IO.File.Exists(FolderSettings + @"\HolidaysOneDate.json")) // если файла нет ВЫХОДНЫЕ
            {
                System.IO.File.Create(FolderSettings + @"\HolidaysOneDate.json").Close(); // создать файл
            }

            if (!System.IO.File.Exists(FolderSettings + @"\HolidaysTwoDate.json")) // если файла нет ВЫХОДНЫЕ
            {
                System.IO.File.Create(FolderSettings + @"\HolidaysTwoDate.json").Close(); // создать файл
            }

            if (!System.IO.File.Exists(FolderSettings + @"\Played_music.txt")) // если файла нет ИСТОРИЯ МУЗЫКИ
            {
                System.IO.File.Create(FolderSettings + @"\Played_music.txt").Close(); // создать файл
            }
            PlstLog = new Logger(FolderSettings + @"\Played_music.txt"); // работаем с этим файлом
        }
        
        private void CheckFolderBells()
        {
            if (!Directory.Exists(FolderSelectedBells)) // если папки нет ВЫБРАННЫЕ МЕЛОДИИ ЗВОНКОВ
            {
                Directory.CreateDirectory(FolderSelectedBells); // создали папку

                ShowMBinNewThread(MessageBoxIcon.Exclamation,
                     "Не найдена папка с выбранными мелодиями звонков!\n\nРаспакованы и установлены стандартные мелодии звонков на начало и окончание занятия.");
                UnpackMelodyStartBell(); // Выгрузить и применить стандартную мелодию звонка НА УРОК
                UnpackMelodyEndBell(); // Выгрузить и применить стандартную мелодию звонка С УРОКА
                using (Stream waveFile = Properties.Resources.Bell_StartLesson95db)
                {
                    using (var fileStream = new FileStream(FolderSelectedBells + @"\Bell_StartLesson.wav", FileMode.Create, FileAccess.Write))
                    {
                        waveFile.CopyTo(fileStream);
                    }
                }

                using (Stream waveFile = Properties.Resources.Bell_EndLesson95db)
                {
                    using (var fileStream = new FileStream(FolderSelectedBells + @"\Bell_EndLesson.wav", FileMode.Create, FileAccess.Write))
                    {
                        waveFile.CopyTo(fileStream);
                    }
                }
                return; // Выходим из метода
            }

            if (!System.IO.File.Exists(FolderSelectedBells + @"\Bell_StartLesson.wav")) // Если нет мелодии НА УРОК
            {
                using (Stream waveFile = Properties.Resources.Bell_StartLesson95db)
                {
                    using (var fileStream = new FileStream(FolderSelectedBells + @"\Bell_StartLesson.wav", FileMode.Create, FileAccess.Write))
                    {
                        waveFile.CopyTo(fileStream);
                    }
                }
            }

            if (!System.IO.File.Exists(FolderSelectedBells + @"\Bell_EndLesson.wav")) // Если нет мелодии НА УРОК
            {
                using (Stream waveFile = Properties.Resources.Bell_EndLesson95db)
                {
                    using (var fileStream = new FileStream(FolderSelectedBells + @"\Bell_EndLesson.wav", FileMode.Create, FileAccess.Write))
                    {
                        waveFile.CopyTo(fileStream);
                    }
                }
            }

            if (!System.IO.File.Exists(MelodyStartBell)) // Если нет мелодии НА УРОК
            {
                UnpackMelodyStartBell(); // Выгрузить и применить стандартную мелодию звонка НА УРОК
                ShowMBinNewThread(MessageBoxIcon.Exclamation,
                    "Не найдена мелодия звонка на НАЧАЛО занятия\n\nРаспакована и установлена стандартная мелодия звонка на начало занятия.");
            }

            if (!System.IO.File.Exists(MelodyEndBell))  // Если нет мелодии С УРОКА
            {
                UnpackMelodyEndBell(); // Выгрузить и применить стандартную мелодию звонка С УРОКА
                ShowMBinNewThread(MessageBoxIcon.Exclamation,
                    "Не найдена мелодия звонка на ОКОНЧАНИЕ занятия\n\nРаспакована и установлена стандартная мелодия звонка на окончание занятия.");
            }
        }

        private void UnpackMelodyStartBell() // [NEW NEW NEW]  Выгрузить и применить стандартную мелодию звонка НА УРОК
        {
            using (Stream waveFile = Properties.Resources.Dzz_Niz_10sek)
            {
                using (var fileStream = new FileStream(FolderSelectedBells + @"\Dzz_Niz_10sek.wav", FileMode.Create, FileAccess.Write))
                {
                    waveFile.CopyTo(fileStream);
                }
            }
            //System.IO.File.WriteAllBytes(FolderSelectedBells + @"\Dzz_Niz_10sek.mp3", Properties.Resources.Dzz_Niz_10sek); // Выгружаем мелодии
            MelodyStartBell = FolderSelectedBells + @"\Dzz_Niz_10sek.wav"; // Применяем ссылки на них
            tbxMelodyStart.Text = Path.GetFileName(MelodyStartBell); // Выводим выбранное
            SettingsINI.WriteINI("Main", "MelodyStartBell", MelodyStartBell.ToString());
        }
        private void UnpackMelodyEndBell() // [NEW NEW NEW]  Выгрузить и применить стандартную мелодию звонка С УРОКА
        {
            using (Stream waveFile = Properties.Resources.Dzz_Niz_7sek)
            {
                using (var fileStream = new FileStream(FolderSelectedBells + @"\Dzz_Niz_7sek.wav", FileMode.Create, FileAccess.Write))
                {
                    waveFile.CopyTo(fileStream);
                }
            }
            //System.IO.File.WriteAllBytes(FolderSelectedBells + @"\Dzz_Niz_7sek.mp3", Properties.Resources.Dzz_Niz_7sek); // Выгружаем мелодии
            MelodyEndBell = FolderSelectedBells + @"\Dzz_Niz_7sek.wav"; // Применяем ссылки на них
            tbxMelodyEnd.Text = Path.GetFileName(MelodyEndBell); // Выводим выбранное
            SettingsINI.WriteINI("Main", "MelodyEndBell", MelodyStartBell.ToString());
            try
            {
                IWaveSource source = CodecFactory.Instance.GetCodec(MelodyEndBell); // Считали информацию
                DurationEndBell = source.GetLength();
                Log.Write("Длительность мелодии звонка с урока: " + DurationEndBell);
            }
            catch (Exception e)
            {
                ShowErrorMB("Невозможно считать длительность звучания мелодии звонка на окончание занятия в InitializeTimersAndSettings", e.Message, e.ToString());
            }
        }

        private void NewLoadSettings() // [NEW NEW NEW]  Загрузка настроек
        {
            Log.Write("=============================================================");
            Log.Write($"Запуск программы № [{Properties.Settings.Default.NumberStart}].");
            Log.Write("=============================================================");
            Properties.Settings.Default.NumberStart += 1;
            Properties.Settings.Default.Save();
            try
            {
                cbxRunMinimized.IsChecked = Convert.ToBoolean(SettingsINI.ReadINI("Main", "RunMinimized")); // Convert.ToString()

                if (!(bool)cbxRunMinimized.IsChecked) // если запускать НЕ свернутой
                {
                    Show();
                    ShowInTaskbar = true;
                }

                NewLoadAllRasps(); // Загрузить настройки расписаний звонков

                cbxPlayMusic.IsChecked = Convert.ToBoolean(SettingsINI.ReadINI("Main", "PlayMusic"));
                axWmpMusic.settings.volume = Convert.ToInt32(SettingsINI.ReadINI("Main", "VolumeMusic"));
                comboModePlayMusic.SelectedIndex = Convert.ToInt32(SettingsINI.ReadINI("Main", "ModePlayMusic"));
                FolderMusic = Convert.ToString(SettingsINI.ReadINI("Main", "FolderMusic")); // Удачно считали SettingsINI.ReadINI("Main", "")
                tbxMusicFolder.Text = Path.GetFileName(FolderMusic);

                cbxPlayDops.IsChecked = Convert.ToBoolean(SettingsINI.ReadINI("Main", "PlayDops"));
                axWmpDops.settings.volume = Convert.ToInt32(SettingsINI.ReadINI("Main", "VolumeDops"));

                cbxAutoStart.IsChecked = Convert.ToBoolean(SettingsINI.ReadINI("Main", "AutoRun"));
                cbxRunMinimized.IsChecked = Convert.ToBoolean(SettingsINI.ReadINI("Main", "RunMinimized")); // Convert.ToString()

                MelodyStartBell = Convert.ToString(SettingsINI.ReadINI("Main", "MelodyStartBell")); // Удачно считали Convert.ToBoolean(SettingsINI.ReadINI("Main", ""))
                tbxMelodyStart.Text = Path.GetFileName(MelodyStartBell);

                MelodyEndBell = Convert.ToString(SettingsINI.ReadINI("Main", "MelodyEndBell")); // Удачно считали Convert.ToInt32()
                tbxMelodyEnd.Text = Path.GetFileName(MelodyEndBell);


                MinPlayMusicAfterLastBell = new TimeSpan(0, Convert.ToInt32(SettingsINI.ReadINI("Main", "MinPlayMusicAfterLastBell")), 0);
                tbxMinMusAfterBells.Text = MinPlayMusicAfterLastBell.Minutes.ToString();

                cbxOtherPlayer.IsChecked = Convert.ToBoolean(SettingsINI.ReadINI("Main", "ActiveOtherPlayer"));
                FileOtherPlayer = Convert.ToString(SettingsINI.ReadINI("Main", "FileOtherPlayer"));

                if (System.IO.File.Exists(FileOtherPlayer)) // если файл стороннего плеера существует
                {
                    tbxOtherPlayer.Text = Path.GetFileName(FileOtherPlayer);
                }
                else
                {
                    FileOtherPlayer = "Не выбран";
                    tbxOtherPlayer.Text = "Не выбран, либо файл удалён";
                }


                rbYsilNoControl.IsChecked = YsilNoControl = Convert.ToBoolean(SettingsINI.ReadINI("Main", "YsilNoControl"));
                rbYsilAuto.IsChecked = YsilAuto = Convert.ToBoolean(SettingsINI.ReadINI("Main", "YsilAuto"));
                rbYsilHands.IsChecked = YsilHands = Convert.ToBoolean(SettingsINI.ReadINI("Main", "YsilHands"));
                rbYsilTime.IsChecked = YsilTime = Convert.ToBoolean(SettingsINI.ReadINI("Main", "YsilTime"));
                YsilFixTimeOn = TimeSpan.Parse(SettingsINI.ReadINI("Main", "YsilFixTimeOn"));
                YsilFixTimeOff = TimeSpan.Parse(SettingsINI.ReadINI("Main", "YsilFixTimeOff"));
                tbxYsilTimeOn.Text = YsilFixTimeOn.ToString("hh':'mm");
                tbxYsilTimeOff.Text = YsilFixTimeOff.ToString("hh':'mm");
                SelectedCOM = Convert.ToString(SettingsINI.ReadINI("Main", "SelectedCOM"));

                NeedSetVolumePC = Convert.ToByte(SettingsINI.ReadINI("Main", "NeedSetVolumePC"));
                MusicStartBeforeBellEndEnded = Convert.ToInt32(SettingsINI.ReadINI("Main", "StartMusicBeforeEndBell"));

            }
            catch (Exception f)
            {
                ShowErrorMB("Ошибка при загрузке настроек!", f.Message, f.ToString());
            }
            new System.Threading.Thread(delegate (object obj) {
                LoadComPorts(0);
            }).Start();

            lbYsilCom.Text = "Подключение к модулю управления электропитанием...";
            LoadHolidaysOneDate();
            LoadHolidaysTwoDate();
            LoadSettingsEqualizerBells();
            GetTimeBells("Инициализация приложения");
        }


        private void InitializeTimersAndSettings()
        {
            timerEndMusic.Elapsed += TimerEndMusic_Tick; // Событие окончания времени
            NewlistViewRaspsZvonkov.ItemsSource = AllRasps; // Список всех расписаний
            NewlistViewBells.ItemsSource = AllBells; // Список звонков на сегодня
            NewlistViewDops.ItemsSource = AllDops; // Список доп.сигналов на сегодня
            
            ListViewHolidaysOneDate.ItemsSource = HolidaysOneDate; // Список выходных дней
            ListViewHolidaysTwoDate.ItemsSource = HolidaysTwoDate; // Список каникул
            
            //MusicPlayer.Exited += MusicPlayer_Exited;
            try
            {
                IWaveSource source = CodecFactory.Instance.GetCodec(MelodyEndBell); // Считали информацию
                DurationEndBell = source.GetLength();
                Log.Write("Длительность мелодии звонка с урока: " + DurationEndBell);
            }
            catch (Exception e)
            {
                ShowErrorMB("Невозможно считать длительность звучания мелодии звонка на окончание занятия в InitializeTimersAndSettings", e.Message, e.ToString());
            }

            axWmpMusic.settings.autoStart = false; // Автостарт плеера выключен
            axWmpDops.settings.autoStart = false;
            reservVolumeForDop = reservVolume = axWmpMusic.settings.volume; // Резев громкости
            labelCurrentTime.Text = DateTime.Now.ToString("HH:mm:ss"); // Текущее время
            Date = DateTime.Now.ToShortDateString(); // Текущая дата

            timerLongPressNumeric.Tick += TimerLongPressNumeric_Tick;
            timerLongPressNumeric.Interval = 70; // Таймер для долгого нажатия на кнопку + -

            timer.Tick += new EventHandler(timerTick); // Главный таймер
            timer.Interval = new TimeSpan(0, 0, 1); // Интервал 1 секунда
            timer.Start(); // запуск таймера
            timerTick(timer, null); // Пересчёт всего
            axWmpMusic.PlayStateChange += AxWmpMusic_PlayStateChange;
            //axWmpDops.PlayStateChange += AxWmpMusic_PlayStateChange;
            SystemEvents.SessionEnding += SystemEvents_SessionEnding;
            //Visibility = Visibility.Visible;
            //Loaded();
            //if (onSettingsLoaded != null)
            //{
            //    onSettingsLoaded();
            //}
            //else
            //{
            //    System.Windows.MessageBox.Show("MAINWINDOW\n\n\n OnsettingsLoaded = NULL!!!");
            //}
            NowLoadSettings = false;
        }


        public class PlayerItem
        {
            public int Number { get; set; }

            public string Melody { get; set; }

            public string FullMelody { get; set; }
        }

        private void Log_Writed(string DateTime, string msg)
        {
            LogItem logged = new LogItem();
            logged.Date = DateTime;
            logged.Error = msg;

            Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal, (ThreadStart)delegate
            {
                if (ListViewLog.Items.Count == 200) // если строк больше 300
                {
                    ListViewLog.Items.RemoveAt(199); // удалили самую старую
                }
                ListViewLog.Items.Insert(0, logged); // Пишем снизу вверх
            });
        }

        public class LogItem
        {
            public string Date { get; set; }

            public string Error { get; set; }
        }


        private void MainForm_Closing(object sender, System.ComponentModel.CancelEventArgs e) // Выход
        {
            

            if (CloseWithoutQuetsion) // Выход без вопроса
            {
                // да выйти
                if (!NowLoadSettings)
                {
                    timer.Stop();
                    Stop();
                    NewSaveSettings();
                }
            }
            else // Выход с вопросом
            {
                System.Windows.MessageBoxResult rez = System.Windows.MessageBox.Show("Вы действительно хотите выйти из программы?\n\nЗвонки подаваться не будут!", "LessonBell - выход",
                    MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No, System.Windows.MessageBoxOptions.ServiceNotification);
                //MessageBoxResult rez = MessageBoxResult.Yes;
                if (rez == MessageBoxResult.Yes)
                {
                    // да выйти
                    if (!NowLoadSettings)
                    {
                        timer.Stop();
                        Stop();
                        if (workComPort != null && workComPort.IsOpen)
                        {
                            workComPort.Write("0");
                            workComPort.Close();
                        }
                        
                        NewSaveSettings();
                    }
                }
                else
                {
                    e.Cancel = true;
                }
            }
        }

        private void NewSaveSettings() // [NEW NEW NEW]  Сохранение настроек
        {
            Hide();
            notifyIcon.Visible = false;
            Log.Write("Завершение работы программы. Сохранение настроек");
            Log.Write("=============================================================");
            SettingsINI.WriteINI("Main", "VolumeMusic", axWmpMusic.settings.volume.ToString());
            SettingsINI.WriteINI("Main", "VolumeDops", axWmpDops.settings.volume.ToString());
            Stop();
            NewSaveAllRasps();
            SaveAllHolidays();
            SaveSettingsEqualizerBells();
        }



        public void SaveSettingsEqualizerBells() // [NEW NEW NEW]  Сохранить настройки эквалайзера звонков
        {
            SettingsINI.WriteINI("Main", "NeedSetVolumePC", NeedSetVolumePC.ToString());
            SettingsINI.WriteINI("Main", "StartMusicBeforeEndBell", MusicStartBeforeBellEndEnded.ToString());
            for (int i = 0; i < EqFilters.Count; i++)
            {
                SettingsINI.WriteINI("EqualizerBells", "filter" + i, Math.Round(EqFilters[i]).ToString());
            }
        }

        private void LoadSettingsEqualizerBells() // [NEW NEW NEW]  Загрузить настройки эквалайзера звонков
        {
            for (int i = 0; i < 10; i++)
            {
                EqFilters.Add(sbyte.Parse(SettingsINI.ReadINI("EqualizerBells", "filter" + i)));
            }
        }

        #endregion
        // -----------------------------------







        // -----------------------------------
        #region Все по звонкам

        public void GetTimeBells(string kem)//  - [ПОРЯДОК 30 МАРТА 2018]
        {
            Log.Write(" ");
            Log.Write($"[GetTimeBells] Составляем рабочее раписание звонков [{kem}]");

            labelTimeNextBell.Text = "——";
            labelLeftTimeToNextBell.Text = "——";
            labelTimeMusicBeforeBells.Text = "——";
            lbLeftTimeToNextBell.Text = "——";
            lbTimeNextBell.Text = "——";
            MusicDoPar.Clear();
            AllBells.Clear();
            AllDops.Clear();

            NewlistViewBells.ItemsSource = null; // Список звонков на сегодня
            NewlistViewDops.ItemsSource = null; // Список доп.сигналов на сегодня
            NewlistViewBells.ItemsSource = AllBells; // Список звонков на сегодня
            NewlistViewDops.ItemsSource = AllDops; // Список доп.сигналов на сегодня
            PrevBell = null;
            NextBell = null;

            if (!NowHoliday()) // если сейчас НЕ выходной - добавляем время звонков
            {
                bool PoSpec = false;
                
                for (byte i = 0; i < AllRasps.Count; i++) // Ищем ПРИОРИТЕТНЫЕ РАСПИСАНИЯ
                {
                    if (AllRasps[i].Active && CheckZvonit(i) && AllRasps[i].Priority == 1)
                    {
                        PoSpec = true;
                        Log.Write("[GetTimeBells] Работаем по спец.расписанию: " + AllRasps[i].NameRasp);
                        AddTimeBells(i); // добавляем время ТОЛЬКО спец.расписаний
                    }
                }

                if (!PoSpec) // добавляем ОБЫЧНЫЕ
                {
                    for (byte i = 0; i < AllRasps.Count; i++)
                    {
                        if (AllRasps[i].Active && CheckZvonit(i) && AllRasps[i].Priority == 0) // если активно и сегодня звонится
                        {
                            Log.Write("[GetTimeBells] Работаем по обычному расписанию: " + AllRasps[i].NameRasp);
                            AddTimeBells(i);
                        }
                    }
                }

                if (MusicDoPar.Count > 0)
                {
                    MusicDoPar.Sort(new MuzDoParComparer());
                    labelTimeMusicBeforeBells.Text = MusicDoPar[0].Time.ToString("hh':'mm");
                }

                if (AllBells.Count > 0)
                {
                    AllBells = new ObservableCollection<RaspLesson>(AllBells.OrderBy(p => p.TimeStart)); // сортируем список
                    for (int i = 0; i < AllBells.Count; i++) // нумерация
                    {
                        AllBells[i].Number = i + 1;
                    }
                    FoundNextBell();
                }
                NewlistViewBells.Items.Refresh();
                NewlistViewDops.Items.Refresh();

                timerTick(null, null);
                OnMusicNow();
            }
            else
            {
                OnMusicNow();
                if (StateYsil && workComPort != null) // если усилитель включен и порт есь
                {
                    OffYsil("Сегодня выходной"); // выключаем усилитель
                }
                lbLeftTimeToNextBell.Text = labelLeftTimeToNextBell.Text = "Сегодня ВЫХОДНОЙ!";
                Log.Write("[GetTimeBells] Сегодня ВЫХОДНОЙ!");
            }
            CalcTimeYsilOnOff();
            Log.Write(" ");
        }

        private bool CheckZvonit(byte k) // Проверка звонится ли сегодня расписание - [ПОРЯДОК 19 МАРТА]
        {
            string Tekdn = DateTime.Now.ToString("ddd");
            // если ((Звонки == дням И Сегодняшний день активен) ИЛИ (Звонки == дате И Дата сегодня == Дате звонков))
            if ((AllRasps[k].ZvonDniNedeli && (Tekdn == "Пн" && AllRasps[k].PN || Tekdn == "Вт" && AllRasps[k].VT ||
                       Tekdn == "Ср" && AllRasps[k].SR || Tekdn == "Чт" && AllRasps[k].CT || Tekdn == "Пт" && AllRasps[k].PT ||
                       Tekdn == "Сб" && AllRasps[k].SB || Tekdn == "Вс" && AllRasps[k].VS)) ||
                       (AllRasps[k].ZvonDate && AllRasps[k].Date.ToLongDateString() == DateTime.Now.ToLongDateString()))
            {
                // Расписание звонится
                return true;
            }
            else
            {
                // Расписание НЕ звонится
                return false;
            }
        }

        private void AddTimeBells(byte k) // Добавить время по которому звонить в Bells для инфо когда звонок - [ПОРЯДОК 30 МАРТА 2018]
        {
            for (byte i = 1; i < AllRasps[k].Uroks.Count; i++) // повтор 
            {
                AllRasps[k].Uroks[i].PozvonilStart = false;
                AllRasps[k].Uroks[i].PozvonilEnd = false;
                AllBells.Add(AllRasps[k].Uroks[i]);
            }

            for (byte i = 1; i < AllRasps[k].Dops.Count; i++) // повтор
            {
                AllRasps[k].Dops[i].Pozvonil = false;
                AllDops.Add(AllRasps[k].Dops[i]);
            }

            if (AllRasps[k].Uroks[0].MuzActive)
            {
                MusicDoPar.Add(new MuzDoPar(AllRasps[k].Uroks[0].TimeStart, false));
            }
        }

        private void FoundNextBell() // FOUND FOUND FOUND AA Ищем ближайший звонок
        {
            NextBell = PrevBell = null;
            TimeSpan MinInterval = new TimeSpan(23, 59, 59);
            int urok = -1;

            // ищем следующий звонок
            for (int i = 0; i < AllBells.Count; i++)
            {
                if (!AllBells[i].PozvonilStart && AllBells[i].TimeStart > TimeNow && AllBells[i].TimeStart - TimeNow < MinInterval)
                {
                    NextBell = new Bell(AllBells[i].TimeStart, true, AllBells[i].MuzActive, false, i);
                    MinInterval = AllBells[i].TimeStart - TimeNow; // минимальный интервал
                    urok = i; // номер урока
                }
                if (!AllBells[i].PozvonilEnd && AllBells[i].TimeEnd > TimeNow && AllBells[i].TimeEnd - TimeNow < MinInterval)
                {
                    NextBell = new Bell(AllBells[i].TimeEnd, false, AllBells[i].MuzActive, i == AllBells.Count - 1, i);
                    MinInterval = AllBells[i].TimeEnd - TimeNow; // минимальный интервал
                    urok = i; // номер урока
                }
            }

            if (urok == -1) // если уроков больше нет
            {
                // звонков нет
                lbTimeNextBell.Text = labelTimeNextBell.Text = "——";
                lbLeftTimeToNextBell.Text = "——";
                labelLeftTimeToNextBell.Text = "уроки ЗАКОНЧИЛИСЬ!";
            }
            else // уроки еще есть
            {
                Log.Write($"[FoundNextBell] Следующий звонок: Урок: [{urok + 1}], На урок: [{NextBell.NaPary}], Время: [{NextBell.Time}], Muz [{NextBell.MuzActive}], Last [{NextBell.Last}]");
                if (NextBell.NaPary) // если звонок на урок
                {
                    lbTimeNextBell.Text = labelTimeNextBell.Text = "на урок в " + NextBell.Time.ToString("hh':'mm");
                }
                else
                {
                    lbTimeNextBell.Text = labelTimeNextBell.Text = "с урока в " + NextBell.Time.ToString("hh':'mm");
                }

                if (urok != 0) // если урок не первый
                {
                    if (NextBell.NaPary) // если звонок на урок
                    {
                        PrevBell = new Bell(AllBells[urok - 1].TimeEnd, false, AllBells[urok - 1].MuzActive, false, urok - 1); // предыдущий звонок - конец предыдущего урока
                    }
                    else
                    {
                        PrevBell = new Bell(AllBells[urok].TimeStart, true, AllBells[urok].MuzActive, false, urok); // предыдущий звонок - начало этого урока
                    }
                }
            }
        }

        private void NewCheckPlayBell() // Проверка на подачу звонка
        {
            if (NextBell != null)
            {
                lbLeftTimeToNextBell.Text = labelLeftTimeToNextBell.Text = (NextBell.Time - TimeNow).ToString();

                // Если след звонок не звонил и Тек время >= звонка и тек время < звонка + 5 сек (ловим с лагами)
                if (!NextBell.Pozvonil && TimeNow >= NextBell.Time && TimeNow < NextBell.Time + new TimeSpan(0, 0, 5))
                {
                    NextBell.Pozvonil = true;
                    Log.Write(" ");
                    Thread thread; // Новый поток для подачи звонка
                    if (NextBell.NaPary)
                    {
                        // Звонок на урок
                        AllBells[NextBell.Number].PozvonilStart = true;
                        thread = new Thread(Bell_Start); // Подаем звонок
                    }
                    else
                    {
                        // Звонок с урока
                        AllBells[NextBell.Number].PozvonilEnd = true;
                        int b = NextBell.Number;
                        thread = new Thread(delegate () { Bell_End(b); }); // Подаем звонок
                    }
                    thread.IsBackground = true;
                    thread.Start(); // Подаём звонок
                    NewlistViewBells.Items.Refresh(); // Обновили что звонок позвонил
                    FoundNextBell(); // Ищем следующий звонок
                }
            }
        }

        
        private void ChechMuzDoYrokov() // Проверка и запуск музыки перед занятиями
        {
            try
            {
                for (byte i = 0; i < MusicDoPar.Count; i++)
                {
                    // если еще не звонил (И) 10:01 >= 10:00 (И) 10:01 < 10:04
                    if (!MusicDoPar[i].Pozvonil && TimeNow >= MusicDoPar[i].Time && TimeNow < MusicDoPar[i].Time + new TimeSpan(0, 0, 4))
                    {
                        MusicDoPar[i].Pozvonil = true;
                        if (ActiveOtherPlayer && System.IO.File.Exists(FileOtherPlayer))
                        {
                            Log.Write("[Музыка_ДоЗанятий] Включение стороннего плеера на воспроизведение музыки");
                            MusicPlayer = Process.Start(FileOtherPlayer);
                            MuzPlayerOn = true;
                        }
                        else
                        {
                            AddMusic(true, true); // Добавить музыку в плеер и начать играть
                        }
                    }
                }
            }
            catch (Exception e)
            {
                ShowErrorMB("Ошибка при проверке на начало воспроизведения музыки перед занятиями", e.Message, e.ToString());
            }
        }
        


        private void PlayDopSignals(object KakieNado) // Проигрываем доп сигналы в новом потоке - [ПОРЯДОК 22 МАРТА]
        {
            try
            {
                try
                {
                    SysVolumeConfigurator VolumeConfig = new SysVolumeConfigurator();
                    Log.Write($"[Доп_Громкость] Текущая: [{VolumeConfig.Volume * 100}], Mute: [{VolumeConfig.Muted}], Устанавливается: [{NeedSetVolumePC}]");
                    VolumeConfig.UnmuteAndSetVolume(NeedSetVolumePC);
                }
                catch (Exception r)
                {
                    Log.Write(r.Message);
                    Log.Write(r.ToString());
                }
                #region Загрузка в плейлист, добавление в плеер
                var playlistDopSignals = axWmpDops.playlistCollection.newPlaylist("DopPlayList");
                totalDlitDops = 0;
                int number = 0;
                foreach (var value in (List<string>)KakieNado)
                {
                    number++;
                    var source = CodecFactory.Instance.GetCodec(value); // Считали информацию о песне
                    totalDlitDops += Math.Round(source.GetLength().TotalSeconds); // Добавили длительность в сек в общую длительность округлив

                    Log.Write($"[Доп сигналы] Мелодии на сейчас: ({source.GetLength()})" + value);
                    var mediaItem = axWmpDops.newMedia(value);
                    playlistDopSignals.appendItem(mediaItem);
                    Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal, (ThreadStart)delegate
                    {
                        ListViewPlayerDop.Items.Add(new PlayerItem() { Number = number, Melody = Path.GetFileNameWithoutExtension(value), FullMelody = value });
                    });
                }
                axWmpDops.currentPlaylist = playlistDopSignals;
                #endregion

                reservVolumeForDop = axWmpMusic.settings.volume; // Резервно сохраняем громкость
                Thread.Sleep(1500); // Ждем 2 секунды
                if (NowZvonitZvonok)
                {
                    axWmpMusic.settings.volume = 0;
                    while (NowZvonitZvonok) // Пока звенит звонок
                    {
                        Thread.Sleep(330); // Ждем 0,3 секунды
                    }
                }
                else // звонок не звенит
                {
                    if (axWmpMusic.playState == WMPLib.WMPPlayState.wmppsPlaying)
                    {
                        while (axWmpMusic.settings.volume > 0) // Пока громкость больше 0 - уменьшаем
                        {
                            axWmpMusic.settings.volume -= 1; // Плавно затушить громкость
                            Thread.Sleep(70);
                        }
                    }
                    else
                    {
                        axWmpMusic.settings.volume = 0;
                    }
                }

                // Проиграть доп.сигнал и плавно вернуть громкость плееру музыки
                axWmpDops.Ctlcontrols.play(); // Проигрываем доп.сигналы
                Log.Write("Ожидаем окончания доп сигналов через: " + TimeSpan.FromSeconds(totalDlitDops));
                Thread.Sleep((int)totalDlitDops * 1000); // Ждем пока закончат проигрывать допы
                Thread.Sleep(2000);
                axWmpDops.currentPlaylist = axWmpDops.playlistCollection.newPlaylist("LessonBellNullDops"); // впендюрили пустой плейлист

                Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal, (ThreadStart)delegate
                {
                    ListViewPlayerDop.Items.Clear();
                });
                
                while (axWmpMusic.settings.volume != reservVolumeForDop) // Пока громкость не равна резервной
                {
                    axWmpMusic.settings.volume += 1; // Прибавляем 1
                    Thread.Sleep(70);
                }
            }
            catch (Exception r)
            {
                Log.Write("Ошибка Проигрывания доп.сигналов! " + r.Message);
                Log.Write(r.ToString());
                while (axWmpMusic.settings.volume != reservVolumeForDop) // Пока громкость не равна резервной
                {
                    axWmpMusic.settings.volume += 1; // Прибавляем 1
                    Thread.Sleep(40);
                }
            }
        }

        private void CheckNaDopSignal() // Проверяем на подачу доп сигнала - [ПОРЯДОК 22 МАРТА]
        {
            List<string> KakieNado = new List<string>();
            try
            {
                for (byte i = 0; i < AllDops.Count; i++)
                {
                    // если звонок еще не звонил (И) 10:01 >= 10:00 (И) 10:01 < 10:04
                    if (!AllDops[i].Pozvonil && TimeNow >= AllDops[i].Time && TimeNow < AllDops[i].Time + new TimeSpan(0, 0, 4))
                    {
                        AllDops[i].Pozvonil = true;

                        if (System.IO.File.Exists(AllDops[i].Signal)) // если файл есть
                        {
                            KakieNado.Add(AllDops[i].Signal); // добавляем
                        }
                        else
                        {
                            Log.Write("Файла доп.сигнала нет! " + AllDops[i].Signal);
                        }
                    }
                }
                if (KakieNado.Count > 0) // Если список НЕ ПУСТ
                {
                    Thread thread = new Thread(PlayDopSignals);
                    thread.IsBackground = true;
                    thread.Start(KakieNado);
                }
            }
            catch (Exception e)
            {
                Log.Write($"[Доп сигналы] Ошибка при подаче доп.сигналов! [{e.Message}]");
                Log.Write(e.ToString());
            }
        }

        #endregion
        // -----------------------------------
        

        











        
        // -----------------------------------
        #region Подача звонков, добавление удаление музыки из плеера

        private void PlayBell(string kak, string FileBell) // Подать звонок - \\\\\ точно порядок апрель cscore
        {
            try
            {
                NowZvonitZvonok = true;
                try
                {
                    SysVolumeConfigurator VolumeConfig = new SysVolumeConfigurator();
                    Log.Write($"[Звонок_Громкость] Текущая: [{VolumeConfig.Volume * 100}], Mute: [{VolumeConfig.Muted}], Устанавливается: [{NeedSetVolumePC}]");
                    VolumeConfig.UnmuteAndSetVolume(NeedSetVolumePC);
                }
                catch (Exception r)
                {
                    Log.Write(r.Message);
                    Log.Write(r.ToString());
                }
                Log.Write("[Звонок] Подан звонок " + kak);

                Stop();

                if (WasapiOut.IsSupportedOnCurrentPlatform)
                    _soundOut = new WasapiOut();
                else
                    _soundOut = new DirectSoundOut();

                var source = CodecFactory.Instance.GetCodec(FileBell)
                    .ChangeSampleRate(44100)
                    .ToSampleSource()
                    .AppendSource(Equalizer.Create10BandEqualizer, out _equalizer)
                    .ToWaveSource();

                _soundOut.Initialize(source);

                for (int i = 0; i < EqFilters.Count; i++)
                {
                    EqualizerFilter filter = _equalizer.SampleFilters[i];
                    filter.AverageGainDB = EqFilters[i];
                }

                _soundOut.Play();


                //Log.Write("[Звонок] Длительность звонка: " + source.GetLength().ToString("hh':'mm"));
                while (source.GetPosition() < source.GetLength() - TimeSpan.FromMilliseconds(MusicStartBeforeBellEndEnded))
                {
                    Thread.Sleep(200);
                }

                NowZvonitZvonok = false; // Звонок закончил звучать
                Log.Write("[Звонок] Звонок закончил звучать");
            }
            catch (Exception e)
            {
                ShowErrorMB("Ошибка при подаче звонка!", e.Message, e.ToString());
            }
        }

        private void Bell_Start() // Выключить плееры и дать звонок на пару - [ПОРЯДОК 19 МАРТА]
        {
            CheckFolderBells();

            reservVolume = axWmpMusic.settings.volume; // Резервно сохраняем громкость

            if (axWmpMusic.playState == WMPLib.WMPPlayState.wmppsPlaying)
            {
                while (axWmpMusic.settings.volume > 0) // Пока громкость больше 0 - уменьшаем
                {
                    axWmpMusic.settings.volume -= 1; // Плавно затушить громкость
                    Thread.Sleep(80);
                }
            }
            Log.Write("[Звонок_Музыка] Выключение встроенного плеера");
            axWmpDops.Ctlcontrols.stop(); // Остановили
            axWmpMusic.Ctlcontrols.stop();
            Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal, (ThreadStart)delegate
            {
                ListViewPlayerMusic.Items.Clear();
            });
            axWmpMusic.currentPlaylist = axWmpMusic.playlistCollection.newPlaylist("NullLessonBellplst"); // впендюрили пустой плейлист
            axWmpMusic.settings.volume = reservVolume; // вернули громкость

            if (MuzPlayerOn) // Если включен сторонний плеер - выключить его
            {
                MusicPlayer.CloseMainWindow();
                Thread.Sleep(300);
                MusicPlayer.Close();
                Thread.Sleep(300);
            }

            PlayBell("на урок, программно", MelodyStartBell); // Звонок на урок
            Log.Write(" ");
        }

        private void Bell_End(int i) // Включить плеер после звонка - [ПОРЯДОК 19 МАРТА]
        {
            CheckFolderBells();
            axWmpDops.Ctlcontrols.stop(); // Остановили
            axWmpMusic.Ctlcontrols.stop(); // Остановили

            if (ActiveMuzNaPeremenax && AllBells[i].MuzActive)
            {
                Thread lm = new Thread(delegate () { AddMusic(false, false); });
                lm.IsBackground = true;
                lm.Start();
            }

            PlayBell("с урока, программно", MelodyEndBell); // Звонок с урока
            
            if (ActiveMuzNaPeremenax && AllBells[i].MuzActive)
            {
                if (i != AllBells.Count - 1) // если звонок не последний
                {
                    if (ActiveOtherPlayer && System.IO.File.Exists(FileOtherPlayer))
                    {
                        Log.Write("[Звонок_Музыка] Включение стороннего плеера на воспроизведение музыки");
                        MusicPlayer = Process.Start(FileOtherPlayer);
                        MuzPlayerOn = true;
                    }
                    else
                    {
                        Log.Write("[Звонок_Музыка] Включение встроенного плеера на воспроизведение музыки");
                        axWmpMusic.Ctlcontrols.play();
                    }
                }
                else
                {
                    if (MinPlayMusicAfterLastBell > new TimeSpan(0, 0, 0))
                    {
                        if (ActiveOtherPlayer && System.IO.File.Exists(FileOtherPlayer))
                        {
                            Log.Write("[Звонок_Музыка] Включение стороннего плеера на воспроизведение музыки");
                            MusicPlayer = Process.Start(FileOtherPlayer);
                            MuzPlayerOn = true;
                        }
                        else
                        {
                            Log.Write("[Звонок_Музыка] Включение встроенного плеера на воспроизведение музыки");
                            axWmpMusic.Ctlcontrols.play();
                        }

                        if (timerEndMusic.Enabled)
                            timerEndMusic.Stop();
                        Log.Write("[Звонок_Последний] Прозвенел последний звонок! Выключение музыки через " + MinPlayMusicAfterLastBell);

                        timerEndMusic.Interval = Convert.ToInt32(MinPlayMusicAfterLastBell.TotalMilliseconds);
                        timerEndMusic.Start();
                    }
                }
            }
            Log.Write(" ");
        }



        private void OnMusicNow() // Включить музыку сейчас
        {
            #region Запуск музыки до занятий
            if (ActiveMuzNaPeremenax && MusicDoPar.Count > 0 && TimeNow >= MusicDoPar[0].Time && AllBells.Count > 0 && TimeNow < AllBells[0].TimeStart)
            {
                if (ActiveOtherPlayer && System.IO.File.Exists(FileOtherPlayer)) // если играем через аимп
                {
                    Log.Write("[Музыка_ДоЗанятий_Сейчас] Включение стороннего плеера на воспроизведение музыки");
                    MusicPlayer = Process.Start(FileOtherPlayer);
                    MuzPlayerOn = true;
                }
                else
                {
                    AddMusic(true, true); // Добавить музыку в плеер и начать играть
                }
                return;
            }
            #endregion

            if (ActiveMuzNaPeremenax && PrevBell != null && PrevBell.MuzActive && !PrevBell.NaPary && MusicDoPar.Count > 0 && TimeNow > MusicDoPar[0].Time &&
                axWmpMusic.playState != WMPLib.WMPPlayState.wmppsPlaying)
            {
                if (ActiveOtherPlayer && System.IO.File.Exists(FileOtherPlayer)) // если играем через аимп
                {
                    Log.Write("[Музыка_НаПеремене_Сейчас] Включение стороннего плеера на воспроизведение музыки");
                    MuzPlayerOn = true;
                    MusicPlayer = Process.Start(FileOtherPlayer);
                }
                else
                {
                    AddMusic(true, true); // Добавить музыку в плеер и начать играть
                }
            }

            if (axWmpMusic.playState == WMPLib.WMPPlayState.wmppsPlaying) // если плеер играет
            {
                if (!ActiveMuzNaPeremenax || AllBells.Count == 0 || PrevBell == null || !PrevBell.MuzActive || PrevBell.NaPary)
                {
                    StopAndClearPlayer(false);
                }
            }
        }

        private void Stop()
        {
            if (_soundOut != null)
            {
                _soundOut.Stop();
               // _soundOut.Dispose();
               // _equalizer.Dispose();
               // _soundOut = null;
            }
        }

        private void AddMusic(bool NeedPlay, bool NeedVolume)
        {
            if (Directory.Exists(FolderMusic)) // Если папка есть
            {
                if (NeedVolume)
                {
                    try
                    {
                        SysVolumeConfigurator VolumeConfig = new SysVolumeConfigurator();
                        Log.Write($"[Громкость_AddMusic] Громкость компьютера: [{VolumeConfig.Volume * 100}], Mute: [{VolumeConfig.Muted}], Устанавливается: [{NeedSetVolumePC}]");
                        VolumeConfig.UnmuteAndSetVolume(NeedSetVolumePC);
                    }
                    catch (Exception r)
                    {
                        Log.Write(r.Message);
                        Log.Write(r.ToString());
                    }
                }
                WMPLib.IWMPPlaylist playlist = axWmpMusic.playlistCollection.newPlaylist("lbPlstPeremena"); // создаем плейлист
                int number = 0;
                foreach (string file in Directory.GetFiles(FolderMusic, "*.mp3", SearchOption.AllDirectories)) // перебираем все mp3 файлы в заданной папке
                {
                    number++;
                    playlist.appendItem(axWmpMusic.newMedia(file)); // добавляем каждый mp3 файл в список

                    Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal, (ThreadStart)delegate
                    {
                        ListViewPlayerMusic.Items.Add(new PlayerItem() { Number = number, Melody = Path.GetFileNameWithoutExtension(file), FullMelody = file });
                    });
                }
                axWmpMusic.currentPlaylist = playlist; // добавляем плейлист в плеер
                if (NeedPlay) // если нужно начать проигрывать
                {
                    Log.Write("[Музыка_AddMusic] Включение встроенного плеера на воспроизведение музыки");
                    axWmpMusic.Ctlcontrols.play(); // начинаем проигрывание
                }
            }
            else
            {
                ShowMBinNewThread(MessageBoxIcon.Exclamation, "Не выбрана папка с музыкой!\n\nВоспроизведение музыки невозможно!");
                Log.Write("Не выбрана папка с музыкой! проигрывание невозможно");
            }
        }

        private void StopAndClearPlayer(bool NeedVolume)
        {
            Log.Write("[Музыка_СтопОчистить] Выключение встроенного плеера");
            Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal, (ThreadStart)delegate
            {
                ListViewPlayerMusic.Items.Clear();
            });
            Thread newth = new Thread(delegate ()
            {
                if (NeedVolume && axWmpMusic.playState == WMPLib.WMPPlayState.wmppsPlaying)
                {
                    reservVolume = axWmpMusic.settings.volume; // Резервно сохраняем громкость

                    while (axWmpMusic.settings.volume > 0) // Пока громкость больше 0 - уменьшаем
                    {
                        axWmpMusic.settings.volume -= 1; // Плавно затушить громкость
                        Thread.Sleep(80);
                    }
                }

                axWmpMusic.Ctlcontrols.stop();
                axWmpMusic.currentPlaylist = axWmpMusic.playlistCollection.newPlaylist("NullLessonBellplst"); // впендюрили пустой плейлист
                axWmpMusic.settings.volume = reservVolume; // вернули громкость

                // очистили папку от плейлистов
                DirectoryInfo dir = new DirectoryInfo(Environment.GetFolderPath(Environment.SpecialFolder.MyMusic));
                foreach (var item in dir.GetFiles("*.wpl", SearchOption.AllDirectories))
                {
                    item.Delete();
                }
            });
            newth.IsBackground = true;
            newth.Start();
        }

        private void ListViewPlayerMusic_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e) // Переключить песню музыки вручную
        {
            var dataContext = ((FrameworkElement)e.OriginalSource).DataContext;

            if (dataContext is PlayerItem)
            {
                Log.Write("[Музыка] Принудительно включена песня: " + (dataContext as PlayerItem).Melody);
                
                    // Declare a variable to hold the position of the media item 
                    // in the current playlist. An arbitrary value is supplied here.
                int index = (dataContext as PlayerItem).Number - 1;

                // Get the media item at the fourth position in the current playlist.
                WMPLib.IWMPMedia media = axWmpMusic.currentPlaylist.get_Item(index);

                // Play the media item.
                axWmpMusic.Ctlcontrols.playItem(media);
            }
        }

        private void btnOpenMusicFolder_Click(object sender, RoutedEventArgs e)
        {
            if (Directory.Exists(FolderMusic)) // Если папка с музыкой существует
            {
                Process.Start(FolderMusic);
            }
        }

        #endregion
        // -----------------------------------
    }
}