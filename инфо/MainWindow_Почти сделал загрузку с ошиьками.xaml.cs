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
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        IniFile SettingsINI = new IniFile(Directory.GetCurrentDirectory() + @"\Settings\mainSettings.ini");
        string FolderSettings = Directory.GetCurrentDirectory() + @"\Settings";
        string FolderSelectedBells = Directory.GetCurrentDirectory() + @"\SelectedBells";
        string FolderSelectedDops = Directory.GetCurrentDirectory() + @"\SelectedDops";
        DataContractJsonSerializer jsonSerializerRasps = new DataContractJsonSerializer(typeof(ObservableCollection<RaspZvonkov>));
        
        // ------------------
        #region Объявление переменных
        System.Windows.Threading.DispatcherTimer timer = new System.Windows.Threading.DispatcherTimer();
        System.Windows.Forms.Timer timerLongPressNumeric = new System.Windows.Forms.Timer();
        private byte longT = 0;
        private bool UpOrDown = false;
        NotifyIcon notifyIcon = new NotifyIcon();
        ContextMenuStrip cms = new ContextMenuStrip();

        private const double MaxDB = 40;

        private Equalizer _equalizer;
        private ISoundOut _soundOut;

        //IniFile INI = new IniFile(AppDomain.CurrentDomain.BaseDirectory + "settings\\config.ini"); // Все настройки тут
        //IniFile AllRaspsINI = new IniFile(AppDomain.CurrentDomain.BaseDirectory + "settings\\Rasps.ini"); // Все настройки тут
        Logger Log; // Ведение лога
        Logger PlstLog;
        Process MusicPlayer;
        bool MuzPlayerOn = false;

        #region Для усилителя
        // ---------------------------------------------------------------------------------------------
        bool StateYsil = false; // Состояние усилителя
        bool YsilNoControl = false;
        bool YsilAuto = false;
        bool YsilTime = false;
        bool YsilHands = false;

        TimeSpan YsilAutoTimeOn = new TimeSpan(0, 2, 0); // Время автоматического включения усилителя
        TimeSpan YsilAutoTimeOff = new TimeSpan(0, 2, 0); // Время автоматического выключения
        TimeSpan YsilFixTimeOn = new TimeSpan(0, 2, 0); // Фиксированное время включения усилителя
        TimeSpan YsilFixTimeOff = new TimeSpan(0, 2, 0); // Фиксированное время выключения
        string ReservedTimeOnYsil = "";
        string ReservedTimeOffYsil = "";
        SerialPort workComPort; // СОМ порт для управления усилителем
        string[] comPorts;
        string SelectedCOM = "";
        // ---------------------------------------------------------------------------------------------
        #endregion


        #region AllRasps, AllBells, AllDops, MuzDoPar, Kanuluks, Holydays, 1 RaspVr
        // ---------------------------------------------------------------------------------------------
        public ObservableCollection<RaspZvonkov> AllRasps = new ObservableCollection<RaspZvonkov>();
        public ObservableCollection<Urok> AllBells = new ObservableCollection<Urok>();
        public ObservableCollection<DopSignal> AllDops = new ObservableCollection<DopSignal>();
        public List<MuzDoPar> MusicDoPar = new List<MuzDoPar>(); // Сегодняшняя музыка до уроков
        List<Kanukyli> Kanukuls = new List<Kanukyli>(); // Каникулы
        List<Holiday> Holydays = new List<Holiday>(); // Выходные
        public RaspZvonkov RaspVr = new RaspZvonkov(); // создали расписание с значениями по умолчанию
        // ---------------------------------------------------------------------------------------------
        #endregion

        string FolderMusic = ""; // Папка с музыкой (полный путь)
        string FileOtherPlayer = ""; // Сторонний плеер (полный путь)
        TimeSpan MinPlayMusicAfterLastBell = new TimeSpan(0, 6, 30); // Сколько минут играть музыку

        bool ActiveMuzNaPeremenax = false;
        bool ActiveDops = false;
        bool ActiveOtherPlayer = false;

        int reservVolume = 40; // Резервная громкость (для плавного уменьшения и возвращения)
        int reservVolumeForDop = 70;

        string MelodyStartBell = ""; // Мелодия звонка на урок
        string MelodyEndBell = ""; // Мелодия звонка с урока

        double totalDlitDops = 0;

        TimeSpan DurationEndBell = new TimeSpan(0, 0, 0);
        TimeSpan TimeOffMusic = new TimeSpan(0, 0, 0);

        protected Process[] procs;
        bool NowLoadSettings = true;
        bool NowZvonitZvonok = false;

        TimeSpan TimeNow = TimeSpan.Parse(DateTime.Now.ToLongTimeString());

        string Date = "";
        string DateNow = "";
        #endregion
        // ------------------
        System.Timers.Timer timerEndMusic = new System.Timers.Timer();

        private bool PrevBellMuzActive;
        private bool PrevBellNaPary;

        public MainWindow()
        {
            InitializeComponent(); // Инициализация всех объектов на форме
            if (Properties.Settings.Default.RunMinimized) // если запуск в свернутом виде
            {
                WindowState = System.Windows.WindowState.Minimized;
                ShowInTaskbar = false;
            }

            NewLoadSettings();
            InitializeTimersAndSettings();
            SetNotifyIcon(); // Установка значка в трее
        }

        private void TimerEndMusic_Tick(object sender, EventArgs e)
        {
            timerEndMusic.Enabled = false;
            StopAndClearPlayer(true);

            if (MuzPlayerOn)
            {
                MusicPlayer.CloseMainWindow();
            }
        }

        private void timerTick(object sender, EventArgs e)
        {
            labelCurrentTime.Text = DateTime.Now.ToString("HH:mm:ss");
            lbCurrentDateTime.Text = "Дата: " + DateTime.Now.ToShortDateString() + " | " + "Время: " + DateTime.Now.ToString("HH:mm:ss");
            TimeNow = TimeSpan.Parse(DateTime.Now.ToLongTimeString());

            #region Дата изменилась - Пересобрать GetTimeBells
            DateNow = DateTime.Now.ToShortDateString();
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
                    CheckPlayBell();
                    //InfoKogdaSledZvonok(); // Выводим информацию о времени следующего звонка в раздел время на форме
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
                if (workComPort != null && StateYsil) // Если усилитель включен
                {
                    OffYsil("Сегодня нет звонков");
                }
            }
        } // таймер на время - каждую сек

        private void OnMusicNow() // Включить музыку сейчас
        {
            #region Запуск музыки до занятий
            if (MusicDoPar.Count > 0 && TimeNow >= MusicDoPar[0].Time && TimeNow < AllBells[0].TimeStart)
            {
                if (ActiveOtherPlayer) // если играем через аимп
                {
                    MusicPlayer = Process.Start(FileOtherPlayer);
                    MuzPlayerOn = true;
                }
                else
                {
                    AddMusic(true); // Добавить музыку в плеер и начать играть
                }
                return;
            }
            #endregion

            if (ActiveMuzNaPeremenax && PrevBellMuzActive && PrevBellNaPary == false &&
                axWmpMusic.playState != WMPLib.WMPPlayState.wmppsPlaying)
            {
                AddMusic(true); // Добавить музыку в плеер и начать играть
            }

            if (axWmpMusic.playState == WMPLib.WMPPlayState.wmppsPlaying) // если плеер играет
            {
                if (!ActiveMuzNaPeremenax || !PrevBellMuzActive || PrevBellNaPary)
                {
                    StopAndClearPlayer(true);
                }
            }
        }

        private void InitializeTimersAndSettings()
        {
            timerEndMusic.Elapsed += TimerEndMusic_Tick; // Событие окончания времени
            NewlistViewRaspsZvonkov.ItemsSource = AllRasps; // Список всех расписаний
            NewlistViewBells.ItemsSource = AllBells; // Список звонков на сегодня
            NewlistViewDops.ItemsSource = AllDops; // Список доп.сигналов на сегодня

            axWmpDops.settings.autoStart = false; // Автостарт плеера выключен
            axWmpMusic.settings.autoStart = false; // Автостарт плеера выключен
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
        }

        private void NewSaveSettings() // [NEW NEW NEW]  Сохранение настроек
        {
            Log.Write("Завершение работы программы. Сохранение настроек");
            Log.Write("=============================================================");
            SettingsINI.WriteINI("Main", "VolumeMusic", axWmpMusic.settings.volume.ToString());
            SettingsINI.WriteINI("Main", "VolumeDops", axWmpDops.settings.volume.ToString());
            Stop();
            NewSaveAllRasps();
        }

        private void NewLoadSettings() // [NEW NEW NEW]  Загрузка настроек
        {
            CheckFilesInAppData(); // Проверяем наличие файлов/папок для настроек
            Log.Write("=============================================================");
            //Log.Write($"Запуск программы № []. Загрузка настроек");
            NewLoadAllRasps(); // Загрузить настройки расписаний звонков

            axWmpMusic.settings.volume = Convert.ToInt32(SettingsINI.ReadINI("Main", "VolumeMusic"));
            axWmpDops.settings.volume = Convert.ToInt32(SettingsINI.ReadINI("Main", "VolumeDops"));

            string ListErrors = string.Empty;
            string newValue = string.Empty;

            if (SettingsINI.TryReadINI("Main", "VolumeMusic", ref newValue)) // Пытаемся считать
                axWmpMusic.settings.volume = Convert.ToInt32(newValue); // Удачно считали
            else
            {
                ListErrors += "Ошибка при считывании громкости звука плеера музыки - установлено значение по умолчанию [35]";
                axWmpMusic.settings.volume = 35; // Не считалось - значение по умолчанию
            }
            if (SettingsINI.TryReadINI("Main", "VolumeDops", ref newValue)) // Пытаемся считать
                axWmpDops.settings.volume = Convert.ToInt32(newValue); // Удачно считали
            else
            {
                ListErrors += "Ошибка при считывании громкости звука плеера доп.сигналов - установлено значение по умолчанию [85]";
                axWmpDops.settings.volume = 85; // Не считалось - значение по умолчанию
            }

            //axWmpMusic.settings.volume = Convert.ToInt32(SettingsINI.ReadINI("Main", "VolumeMusic"));
            //axWmpDops.settings.volume = Convert.ToInt32(SettingsINI.ReadINI("Main", "VolumeDops"));

            if (SettingsINI.TryReadINI("Main", "PlayMusic", ref newValue)) // Пытаемся считать
                cbxPlayMusic.IsChecked = Convert.ToBoolean(newValue); // Удачно считали
            else
            {
                ListErrors += "Ошибка при считывании [PlayMusic] - установлено значение по умолчанию [false]";
                cbxPlayMusic.IsChecked = false; // Не считалось - значение по умолчанию
            }
            if (SettingsINI.TryReadINI("Main", "PlayDops", ref newValue)) // Пытаемся считать
                cbxPlayDops.IsChecked = Convert.ToBoolean(newValue); // Удачно считали
            else
            {
                ListErrors += "Ошибка при считывании [PlayDops] - установлено значение по умолчанию [false]";
                cbxPlayDops.IsChecked = false; // Не считалось - значение по умолчанию
            }
            //cbxPlayMusic.IsChecked = Convert.ToBoolean(SettingsINI.ReadINI("Main", "PlayMusic"));
            //cbxPlayDops.IsChecked = Convert.ToBoolean(SettingsINI.ReadINI("Main", "PlayDops"));

            if (SettingsINI.TryReadINI("Main", "AutoRun", ref newValue)) // Пытаемся считать
                cbxAutoStart.IsChecked = Convert.ToBoolean(newValue); // Удачно считали
            else
            {
                ListErrors += "Ошибка при считывании [AutoRun] - установлено значение по умолчанию [false]";
                cbxAutoStart.IsChecked = false; // Не считалось - значение по умолчанию
            }

            if (SettingsINI.TryReadINI("Main", "RunMinimized", ref newValue)) // Пытаемся считать
                cbxRunMinimized.IsChecked = Convert.ToBoolean(newValue); // Удачно считали
            else
            {
                ListErrors += "Ошибка при считывании [RunMinimized] - установлено значение по умолчанию [false]";
                cbxRunMinimized.IsChecked = false; // Не считалось - значение по умолчанию
            }

            //cbxAutoStart.IsChecked = Convert.ToBoolean(SettingsINI.ReadINI("Main", "AutoRun"));
            //cbxRunMinimized.IsChecked = Convert.ToBoolean(SettingsINI.ReadINI("Main", "RunMinimized"));

            if (SettingsINI.TryReadINI("Main", "MelodyStartBell", ref newValue)) // Пытаемся считать
            {
                MelodyStartBell = Convert.ToString(newValue); // Удачно считали
                tbxMelodyStart.Text = Path.GetFileName(MelodyStartBell);
            }
            else
            {
                ListErrors += "Ошибка при считывании [MelodyStartBell] - установлено значение по умолчанию";
                UnpackMelodyStartBell(); // Не считалось - значение по умолчанию
            }

            if (SettingsINI.TryReadINI("Main", "MelodyEndBell", ref newValue)) // Пытаемся считать
            {
                MelodyEndBell = Convert.ToString(newValue); // Удачно считали
                tbxMelodyEnd.Text = Path.GetFileName(MelodyEndBell);
            }
            else
            {
                ListErrors += "Ошибка при считывании [MelodyEndBell] - установлено значение по умолчанию";
                UnpackMelodyEndBell(); // Не считалось - значение по умолчанию
            }

            //MelodyStartBell = Properties.Settings.Default.MelodyStart;
            //MelodyEndBell = Properties.Settings.Default.MelodyEnd;

            if (SettingsINI.TryReadINI("Main", "FolderMusic", ref newValue)) // Пытаемся считать
            {
                FolderMusic = Convert.ToString(newValue); // Удачно считали
                tbxMusicFolder.Text = Path.GetFileName(FolderMusic);
            }
            else
            {
                ListErrors += "Ошибка при считывании [FolderMusic] - установлено значение по умолчанию [null]";
                FolderMusic = "null";
                tbxMusicFolder.Text = "Не выбрана";
            }

            //FolderMusic = Properties.Settings.Default.FolderMusic;
            //tbxMusicFolder.Text = Path.GetFileName(FolderMusic);



            comboModePlayMusic.SelectedIndex = Properties.Settings.Default.ModePlayMusic;
            MinPlayMusicAfterLastBell = new TimeSpan(0, Properties.Settings.Default.PoslednixPesen, 0);
            tbxMinMusAfterBells.Text = Properties.Settings.Default.PoslednixPesen.ToString();

            cbxOtherPlayer.IsChecked = Properties.Settings.Default.ActiveOtherPlayer;
            FileOtherPlayer = Properties.Settings.Default.OtherPlayer;
            tbxOtherPlayer.Text = Path.GetFileName(Properties.Settings.Default.OtherPlayer);

            rbYsilNoControl.IsChecked = YsilNoControl = Properties.Settings.Default.YsilNoControl;
            rbYsilAuto.IsChecked = YsilAuto = Properties.Settings.Default.YsilAuto;
            rbYsilHands.IsChecked = YsilHands = Properties.Settings.Default.YsilHands;
            rbYsilTime.IsChecked = YsilTime = Properties.Settings.Default.YsilTime;
            YsilFixTimeOn = Properties.Settings.Default.YsilTimeOn;
            YsilFixTimeOff = Properties.Settings.Default.YsilTimeOff;
            tbxYsilTimeOn.Text = YsilFixTimeOn.ToString("hh':'mm");
            tbxYsilTimeOff.Text = YsilFixTimeOff.ToString("hh':'mm");
            SelectedCOM = Properties.Settings.Default.ComPort;
            LoadComPorts();
            CheckFolderSelectedMusic();
            var source = CodecFactory.Instance.GetCodec(MelodyEndBell); // Считали информацию
            DurationEndBell = source.GetLength();
            ShowKanikuls();
            ShowHolidays();
            CheckMelodies();
            GetTimeBells("LoadSettings");
            NowLoadSettings = false;
            Log.Write("END LOAD SETTINGS");
        }

        private void NewLoadAllRasps() // [NEW NEW NEW]  Загрузить все расписания звонков (десериализовать)
        {
            if (!System.IO.File.Exists(FolderSettings + @"\Rasps.json")) // если файла нет
            {
                System.IO.File.Create(FolderSettings + @"\Rasps.json"); // создать файл
            }
            else // Файл есть - попытаться считать
            {
                if (System.IO.File.ReadAllLines(FolderSettings + @"\Rasps.json").Length == 0)
                {
                    // файл пуст
                }
                else
                {
                    try // пытаемся считать
                    {
                        using (FileStream fs = new FileStream(FolderSettings + @"\Rasps.json", FileMode.Open))
                        {
                            AllRasps = (ObservableCollection<RaspZvonkov>)jsonSerializerRasps.ReadObject(fs);

                            for (int i = 0; i < AllRasps.Count; i++) // Перебираем все расписания
                            {
                                AllRasps[i].Number = i + 1; // Выставляем нумерацию
                                AllRasps[i].AoPEdited += MainWindow_AoPEdited; // Привязываем событие изменение активности или приоритета
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        System.Windows.Forms.MessageBox.Show("Ошибка при считывании расписаний звонков\n\n" + e.Message + "\n\n------------------------------\n\n" + e.ToString());
                    }
                }
            }
        }

        private void NewSaveAllRasps() // [NEW NEW NEW]  Сохранить все расписания звонков (сериализовать)
        {
            using (FileStream fs = new FileStream(FolderSettings + @"\Rasps.json", FileMode.Create))
            {
                jsonSerializerRasps.WriteObject(fs, AllRasps);
            }
        }

        private void UnpackMelodyStartBell() // [NEW NEW NEW]  Выгрузить и применить стандартную мелодию звонка НА УРОК
        {
            System.IO.File.WriteAllBytes(FolderSelectedBells + @"\Dzz_Niz_10sek.mp3", Properties.Resources.Dzz_Niz_10sek); // Выгружаем мелодии
            MelodyStartBell = FolderSelectedBells + @"\Dzz_Niz_10sek.mp3"; // Применяем ссылки на них
            tbxMelodyStart.Text = Path.GetFileName(MelodyStartBell); // Выводим выбранное
            SettingsINI.WriteINI("Main", "MelodyStartBell", MelodyStartBell.ToString());
        }
        private void UnpackMelodyEndBell() // [NEW NEW NEW]  Выгрузить и применить стандартную мелодию звонка С УРОКА
        {
            System.IO.File.WriteAllBytes(FolderSelectedBells + @"\Dzz_Niz_7sek.mp3", Properties.Resources.Dzz_Niz_7sek); // Выгружаем мелодии
            MelodyEndBell = FolderSelectedBells + @"\Dzz_Niz_7sek.mp3"; // Применяем ссылки на них
            tbxMelodyEnd.Text = Path.GetFileName(MelodyEndBell); // Выводим выбранное
            SettingsINI.WriteINI("Main", "MelodyEndBell", MelodyStartBell.ToString());
        }

        private void CheckFolderSelectedMusic() // [NEW NEW NEW]  Проверяем есть ли AppData, AppData\LessonBell, мелодии звонков, выгружаем если нет
        {
            if (!Directory.Exists(FolderSettings)) // если папки нет
            {
                Directory.CreateDirectory(FolderSettings); // создали папку
            }

            if (!Directory.Exists(FolderSelectedBells)) // если папки нет
            {
                Directory.CreateDirectory(FolderSelectedBells); // создали папку

                ShowMBinNewThread(MessageBoxIcon.Exclamation,
                    "Не найдена папка с выбранными мелодиями звонков!\n\nРаспакованы и установлены стандартные мелодии звонков на начало и окончание занятия.");
                UnpackMelodyStartBell(); // Выгрузить и применить стандартную мелодию звонка НА УРОК
                UnpackMelodyEndBell(); // Выгрузить и применить стандартную мелодию звонка С УРОКА
                return; // останавливаемся т.к. только что применили стандартные мелодии звонков
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

        private void ShowMBinNewThread(MessageBoxIcon icon, string text) // Показ MessageBox в новом потоке
        {
            new Thread(() => System.Windows.Forms.MessageBox.Show(text, "LessonBell - Подача звонков и музыка на переменах", MessageBoxButtons.OK, icon,
                MessageBoxDefaultButton.Button1, System.Windows.Forms.MessageBoxOptions.ServiceNotification)).Start(); // Сообщение с ошибкой
        }

        private void CheckFilesInAppData() // [NEW NEW NEW]  Проверка на наличие папок, файлов в appData
        {
            CheckFolderSelectedMusic();

            if (!System.IO.File.Exists(FolderSettings + @"\Log.txt")) // если файла нет ЛОГ
            {
                System.IO.File.Create(FolderSettings + @"\Log.txt"); // создать файл
            }
            Log = new Logger(FolderSettings + @"\Log.txt"); // работаем с этим файлом

            if (!System.IO.File.Exists(FolderSettings + @"\mainSettings.ini")) // если файла нет ГЛАВН НАСТРОЙКИ
            {
                System.IO.File.Create(FolderSettings + @"\mainSettings.ini"); // создать файл
            }

            if (!System.IO.File.Exists(FolderSettings + @"\Holidays.json")) // если файла нет ВЫХОДНЫЕ
            {
                System.IO.File.Create(FolderSettings + @"\Holidays.json"); // создать файл
            }

            


            if (!System.IO.File.Exists(FolderSettings + @"\Played_music.txt")) // если файла нет ИСТОРИЯ МУЗЫКИ
            {
                System.IO.File.Create(FolderSettings + @"\Played_music.txt"); // создать файл
            }
            PlstLog = new Logger(FolderSettings + @"\Played_music.txt"); // работаем с этим файлом
        }


        // -----------------------------------
        #region Все по звонкам

        public void GetTimeBells(string kem)//  - [ПОРЯДОК 30 МАРТА 2018]
        {
            Log.Write($"[{kem}] Составляем рабочее раписание звонков [GetTimeBells]");

            labelTimeNextBell.Text = "——";
            labelLeftTimeToNextBell.Text = "——";
            labelTimeMusicBeforeBells.Text = "——";
            labelLeftTimeToMusicBeforeBells.Text = "——";
            MusicDoPar.Clear();
            AllBells.Clear();
            AllDops.Clear();

            if (!NowHoliday()) // если сейчас НЕ выходной - добавляем время звонков
            {
                bool PoSpec = false;
                
                for (byte i = 0; i < AllRasps.Count; i++) // Ищем ПРИОРИТЕТНЫЕ РАСПИСАНИЯ
                {
                    if (AllRasps[i].Active && CheckZvonit(i) && AllRasps[i].Priority == 1)
                    {
                        PoSpec = true;
                        Log.Write("Работаем по спец.расписанию: " + AllRasps[i].NameRasp);
                        AddTimeBells(i); // добавляем время ТОЛЬКО спец.расписаний
                    }
                }

                if (!PoSpec) // добавляем ОБЫЧНЫЕ
                {
                    for (byte i = 0; i < AllRasps.Count; i++)
                    {
                        if (AllRasps[i].Active && CheckZvonit(i) && AllRasps[i].Priority == 0) // если активно и сегодня звонится
                        {
                            Log.Write("Работаем по обычному расписанию: " + AllRasps[i].NameRasp);
                            AddTimeBells(i);
                        }
                    }
                }

                MusicDoPar.Sort(new MuzDoParComparer());
                if (AllBells.Count > 0)
                {
                    SortObservableCollection(AllBells); // сортируем список
                }
                NewlistViewBells.Items.Refresh();
                NewlistViewDops.Items.Refresh();

                timerTick(null, null);
                if (ActiveMuzNaPeremenax)
                    OnMusicNow();
            }
            else
            {
                if (StateYsil && workComPort != null) // если усилитель включен и порт есь
                {
                    OffYsil("Сегодня выходной"); // выключаем усилитель
                }
                labelLeftTimeToNextBell.Text = "Сегодня ВЫХОДНОЙ!";
                Log.Write("Сегодня ВЫХОДНОЙ!");
            }
        }

        public static ObservableCollection<Urok> SortObservableCollection(ObservableCollection<Urok> orderThoseGroups)
        {
            ObservableCollection<Urok> temp;
            temp = new ObservableCollection<Urok>(orderThoseGroups.OrderBy(p => p.TimeStart));
            orderThoseGroups.Clear();
            foreach (Urok j in temp) orderThoseGroups.Add(j);

            for (int i = 0; i < orderThoseGroups.Count; i++)
            {
                orderThoseGroups[i].Number = i + 1; // сделали нумерацию
            }

            return orderThoseGroups;
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
                AllBells.Add(AllRasps[k].Uroks[i]);
            }

            for (byte i = 1; i < AllRasps[k].Dops.Count; i++) // повтор
            {
                AllDops.Add(AllRasps[k].Dops[i]);
            }

            if (AllRasps[k].Uroks[0].MuzActive)
            {
                MusicDoPar.Add(new MuzDoPar(AllRasps[k].Uroks[0].TimeStart, false));
            }
        }

        private void AddMusic(bool NeedPlay)
        {
            if (Directory.Exists(FolderMusic) && Directory.GetFiles(FolderMusic).Length >= 2) // Если папка есть и музыки ббольше 2
            {
                WMPLib.IWMPPlaylist playlist = axWmpMusic.playlistCollection.newPlaylist("lbPlstPeremena"); // создаем плейлист
                //lbPlaylistMusic.Items.Clear();
                foreach (string file in Directory.GetFiles(FolderMusic, "*.mp3")) // перебираем все mp3 файлы в заданной папке
                {
                    //lbPlaylistMusic.Items.Add(Path.GetFileName(file));
                    playlist.appendItem(axWmpMusic.newMedia(file)); // добавляем каждый mp3 файл в список
                }
                axWmpMusic.currentPlaylist = playlist; // добавляем плейлист в плеер
                if (NeedPlay) // если нужно начать проигрывать
                    axWmpMusic.Ctlcontrols.play(); // начинаем проигрывание
            }
            else
            {
                lbErrors.Items.Add($"[{DateTime.Now}] Папки с музыкой не существует или песен .mp3 меньше двух. Воспроизведение невозможно!");
                Log.Write("Не выбрана папка с музыкой / файлов в папке меньше двух - проигрывание невозможно");
            }
        }

        private void CheckPlayBell() // Проверка на подачу звонка - [ПОРЯДОК 19 МАРТА]
        {
            for (int i = 0; i < AllBells.Count; i++)
            {
                // Если звонок не звонил и нужно дать сейчас
                if (!AllBells[i].PozvonilStart && TimeNow >= AllBells[i].TimeStart && TimeNow < AllBells[i].TimeStart + new TimeSpan(0, 0, 4))
                {
                    AllBells[i].PozvonilStart = true; // Позвонил
                    NewlistViewBells.Items.Refresh();
                    UpVolumeWindows();
                    DownVolumeWindows();
                    Thread thread = new Thread(Bell_Start); // Подаем звонок
                    thread.IsBackground = true;
                    thread.Start();
                }

                if (!AllBells[i].PozvonilEnd && TimeNow == AllBells[i].TimeEnd && TimeNow < AllBells[i].TimeEnd + new TimeSpan(0, 0, 4))
                {
                    AllBells[i].PozvonilEnd = true; // Позвонил
                    NewlistViewBells.Items.Refresh();
                    UpVolumeWindows();
                    DownVolumeWindows();
                    int b = i;
                    Thread thread = new Thread(delegate () { Bell_End(b); }); // Подаем звонок
                    thread.IsBackground = true;
                    thread.Start(); // передаем номер урока
                }
                TimeSpan TimeNextBell = new TimeSpan(0, 0, 0);
                TimeSpan MinInterval = new TimeSpan(23, 59, 59);
                int urok = -1;
                bool napary = false;

                if (!AllBells[i].PozvonilStart && AllBells[i].TimeStart > TimeNow && AllBells[i].TimeStart - TimeNow < MinInterval)
                {
                    TimeNextBell = AllBells[i].TimeStart;
                    MinInterval = AllBells[i].TimeStart - TimeNow; // минимальный интервал
                    urok = i; // номер урока
                    napary = true;
                }

                if (!AllBells[i].PozvonilEnd && AllBells[i].TimeEnd > TimeNow && AllBells[i].TimeEnd - TimeNow < MinInterval)
                {
                    TimeNextBell = AllBells[i].TimeEnd;

                    MinInterval = AllBells[i].TimeEnd - TimeNow; // минимальный интервал
                    urok = i; // номер урока
                    napary = false;
                }
                // если урок так и остался
                if (urok == -1)
                {
                    // звонков нет
                    labelTimeNextBell.Text = "——";
                    labelLeftTimeToNextBell.Text = "уроки ЗАКОНЧИЛИСЬ!";
                }
                else
                {
                    if (napary)
                    {
                        labelTimeNextBell.Text = "на урок в " + TimeNextBell.ToString("hh':'mm");
                        if (urok > 0)
                        {
                            PrevBellMuzActive = AllBells[urok - 1].MuzActive; // музыка предыдущего звонка
                            PrevBellNaPary = false; // с пары
                        }
                    }
                    else
                    {
                        labelTimeNextBell.Text = "с урока в " + TimeNextBell.ToString("hh':'mm");

                        PrevBellMuzActive = AllBells[urok].MuzActive; // музыка предыдущего звонка
                        PrevBellNaPary = true; // с пары
                    }

                    labelLeftTimeToNextBell.Text = (TimeNextBell - TimeNow).ToString();
                    lbCurrentDateTime.Text += " | Ближайший звонок " + labelTimeNextBell.Text + " через " + (TimeNextBell - TimeNow).ToString();
                    
                }
            }
            if (MusicDoPar.Count > 0)
            {
                foreach (var Muz in MusicDoPar)
                {
                    if (TimeNow <= Muz.Time)
                    {
                        labelTimeMusicBeforeBells.Text = Muz.Time.ToString("hh':'mm");
                        labelLeftTimeToMusicBeforeBells.Text = (Muz.Time - TimeNow).ToString();
                        break;
                    }

                    if (TimeNow > Muz.Time) // сейчас играет
                    {
                        labelTimeMusicBeforeBells.Text = Muz.Time.ToString("hh':'mm");
                        labelLeftTimeToMusicBeforeBells.Text = "——";
                        break;
                    }

                    labelTimeMusicBeforeBells.Text = "——";
                    labelLeftTimeToMusicBeforeBells.Text = "——";
                }
            }
            else
            {
                labelTimeMusicBeforeBells.Text = "——";
                labelLeftTimeToMusicBeforeBells.Text = "——";
            }
        }
        
      /*  private void FoundNextBell() // FOUND FOUND FOUND AA Ищем ближайший звонок
        {
            NextBell = PrevBell = null;
            TimeSpan MinInterval = new TimeSpan(23, 59, 59);
            int urok = -1;

            // ищем следющий звонок
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
                    bool Last;
                    if (i == AllBells.Count - 1) // если звонок последний
                        Last = true;
                    else
                        Last = false;
                    NextBell = new Bell(AllBells[i].TimeEnd, false, AllBells[i].MuzActive, Last, i);
                    MinInterval = AllBells[i].TimeEnd - TimeNow; // минимальный интервал
                    urok = i; // номер урока
                }
            }
            
            // если урок так и остался
            if (urok == -1)
            {
                // звонков нет
                labelTimeNextBell.Text = "——";
                labelLeftTimeToNextBell.Text = "уроки ЗАКОНЧИЛИСЬ!";
            }
            else
            {
                if (NextBell.NaPary) // если звонок на урок
                    labelTimeNextBell.Text = "на урок в " + NextBell.Time.ToString("hh':'mm");
                else
                {
                    labelTimeNextBell.Text = "с урока в " + NextBell.Time.ToString("hh':'mm");

                    if (ActiveMuzNaPeremenax && NextBell.MuzActive)
                    {
                        if (timerMusic.Enabled)
                            timerMusic.Stop();
                        if (NextBell.Time - TimeNow > new TimeSpan(0, 0, 20)) // если больше 7 секунд до звонка
                        {
                            timerMusic.Interval = Convert.ToInt32(Math.Round((NextBell.Time - TimeNow).TotalMilliseconds - 15000));
                        }
                        else
                        {
                            timerMusic.Interval = 100;
                        }
                        timerMusic.Start();
                        Log.Write("Спать до добавления музыки: " + TimeSpan.FromMilliseconds(timerMusic.Interval));
                    }
                }

                if (urok != 0) // если урок не первый
                {
                    if (NextBell.NaPary) // если звонок на урок
                        PrevBell = new Bell(AllBells[urok - 1].TimeEnd, false, AllBells[urok - 1].MuzActive, false, urok - 1);// предыдущий звонок - конец предыдущего урока
                    else
                        PrevBell = new Bell(AllBells[urok].TimeStart, true, AllBells[urok].MuzActive, false, urok);// предыдущеий звонок - начало этого урока
                }    
                Log.Write("СПАТЬ ДО ЗВОНКА: " + (NextBell.Time - TimeNow) + " Музыка у след звонка: " + NextBell.MuzActive);

                if (timerBells.Enabled)
                    timerBells.Stop();

                timerBells.Interval = Convert.ToInt32(Math.Round((NextBell.Time - TimeNow).TotalMilliseconds - 500));
                timerBells.Start();
            }
        } */

        private void InfoKogdaSledZvonok() // Выводим информацию о времени следующего звонка - [ПОРЯДОК 30 МАРТА 2018]
        {
            //if (NextBell != null)
            //{
            //    labelLeftTimeToNextBell.Text = (NextBell.Time - TimeNow).ToString();
                
            //    if (NextBell.NaPary)
            //    {
            //        lbCurrentDateTime.Text += " | Ближайший звонок на урок в " + NextBell.Time.ToString("hh':'mm") +
            //        " через " + (NextBell.Time - TimeNow);
            //    }
            //    else
            //    {
            //        lbCurrentDateTime.Text += " | Ближайший звонок с урока в " + NextBell.Time.ToString("hh':'mm") +
            //        " через " + (NextBell.Time - TimeNow);
            //    }
            //}

            
        }


        private void ChechMuzDoYrokov() // Музыка до уроков - [ПОРЯДОК 22 МАРТА]
        {
            try
            {
                for (byte i = 0; i < MusicDoPar.Count; i++)
                {
                    // если еще не звонил (И) 10:01 >= 10:00 (И) 10:01 < 10:04
                    if (!MusicDoPar[i].Pozvonil && TimeNow >= MusicDoPar[i].Time && TimeNow < MusicDoPar[i].Time + new TimeSpan(0, 0, 4))
                    {
                        MusicDoPar[i].Pozvonil = true;
                        if (ActiveOtherPlayer)
                        {
                            MusicPlayer = Process.Start(FileOtherPlayer);
                            MuzPlayerOn = true;
                            Log.Write("[Муз до уроков] Включен сторонний плеер");
                        }
                        else
                        {
                            AddMusic(true); // Добавить музыку в плеер и начать играть
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Log.Write("[Муз до уроков] Ошибка ChechMuzDoYrokov " + e.Message);
                Log.Write(e.ToString());
                new Thread(() => System.Windows.Forms.MessageBox.Show("Ошибка ChechMuzDoYrokov\n\n" + e.Message,
                    "LessonBell", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, System.Windows.Forms.MessageBoxOptions.ServiceNotification)).Start();
            }
        }



        private void PlayDopSignals(object KakieNado) // Проигрываем доп сигналы в новом потоке - [ПОРЯДОК 22 МАРТА]
        {
            try
            {
                #region Загрузка в плейлист, добавление в плеер
                var playlistDopSignals = axWmpDops.playlistCollection.newPlaylist("DopPlayList");
                totalDlitDops = 0;
                foreach (var value in (List<string>)KakieNado)
                {
                    //var audiof = TagLib.File.Create(value);
                    //totalDlitDops += Math.Round(audiof.Properties.Duration.TotalSeconds);

                    var source = CodecFactory.Instance.GetCodec(value); // Считали информацию о песне
                    totalDlitDops += Math.Round(source.GetLength().TotalSeconds); // Добавили длительность в сек в общую длительность округлив

                    Log.Write($"[Доп сигналы] Мелодии на сейчас: ({source.GetLength()})" + value);
                    var mediaItem = axWmpDops.newMedia(value);
                    playlistDopSignals.appendItem(mediaItem);
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

                axWmpDops.currentPlaylist = axWmpDops.playlistCollection.newPlaylist("LessonBellNullDops"); // впендюрили пустой плейлист

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
                    Thread.Sleep(70);
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
        #region Сохранение, загрузка настроек, проверка наличия файлов
        

        /* private void CheckHaveBellStart() // Если нет файла звонка на УРОК - выгрузить стандартный и применить его
        {
            if (!Directory.Exists(AppDomain.CurrentDomain.BaseDirectory + "SelectedMusic")) // если нет папки SelectedMusic
            {
                Directory.CreateDirectory(AppDomain.CurrentDomain.BaseDirectory + "SelectedMusic"); // Создаем
            }
            if (!System.IO.File.Exists(MelodyStartBell)) // Если нет файла звонка на УРОК - выгрузить стандартный и применить
            {
                var defaultMainSettings = new MainSettings(); // Настройки по умолчанию

                new Thread(() => System.Windows.Forms.MessageBox.Show(
                    "Не найден звуковой файл звонка на урок!\nУстановлена мелодия по умолчанию.",
                            "LessonBell", MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1,
                            System.Windows.Forms.MessageBoxOptions.ServiceNotification)).Start(); // Сообщение с ошибкой

                MelodyStartBell = defaultMainSettings.MelodyStart; // Мелодия НА УРОК
                tbxMelodyStart.Text = Path.GetFileName(MelodyStartBell);

                if (!System.IO.File.Exists(MelodyStartBell)) // если файла нет - выгрузить
                {
                    System.IO.File.WriteAllBytes(MelodyStartBell, Properties.Resources.Dzz_Niz_10sek);
                }
            }
        }

        private void CheckHaveBellEnd() // Если нет файла звонка С УРОКА - выгрузить стандартный и применить его
        {
            if (!Directory.Exists(AppDomain.CurrentDomain.BaseDirectory + "SelectedMusic")) // если нет папки SelectedMusic
            {
                Directory.CreateDirectory(AppDomain.CurrentDomain.BaseDirectory + "SelectedMusic"); // Создаем
            }
            if (!System.IO.File.Exists(MelodyEndBell)) // Если нет файла звонка С УРОКА - выгрузить стандартный и применить его
            {
                var defaultMainSettings = new MainSettings();

                new Thread(() => System.Windows.Forms.MessageBox.Show("Не найден звуковой файл звонка с урока!\nУстановлена мелодия по умолчанию.",
                            "LessonBell", MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1,
                            System.Windows.Forms.MessageBoxOptions.ServiceNotification)).Start();

                MelodyEndBell = defaultMainSettings.MelodyEnd; // Мелодия С УРОКА
                tbxMelodyEnd.Text = System.IO.Path.GetFileName(MelodyEndBell);

                if (!System.IO.File.Exists(MelodyEndBell)) // если файла нет - выгрузить
                {
                    System.IO.File.WriteAllBytes(MelodyEndBell, Properties.Resources.Dzz_Niz_7sek);
                }
            }
        }
        */
        private void CheckMelodies() // Проверка на существование файлов звонка, папки с плейлистом, музыки в папке
        {
            try
            {
                string err = string.Empty;

                CheckFolderSelectedMusic();

                if (!Directory.Exists(FolderMusic))
                {
                    err += "Папки с плейлистом не существует\n";
                }

                if (err != string.Empty)
                {
                    new Thread(() => System.Windows.Forms.MessageBox.Show(err, "LessonBell",
                        MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1,
                        System.Windows.Forms.MessageBoxOptions.ServiceNotification)).Start();
                }
            }
            catch (Exception e)
            {
                System.Windows.Forms.MessageBox.Show("Ошибка CheckMelodies\n\n" + e.Message + "\n\n------------------------------\n\n" + e.ToString());
            }
        }

        private void MainForm_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //System.Windows.MessageBoxResult rez = System.Windows.MessageBox.Show("Вы действительно хотите выйти из программы?\n\nЗвонки подаваться не будут!", "LessonBell",
            //    MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No, System.Windows.MessageBoxOptions.ServiceNotification);
            MessageBoxResult rez = MessageBoxResult.Yes;
            if (rez == MessageBoxResult.Yes)
            {
                // да выйти
                timer.Stop();
                NewSaveSettings();
            }
            else
            {
                e.Cancel = true;
            }
            /*
            if (e.CloseReason == CloseReason.UserClosing)
            {
                if (halt)
                {
                    SaveSettings();
                    // выключается комп после окна с предупреждением
                }
                else
                {
                    if (MessageBox.Show("Вы действительно хотите выйти из программы?\n\nЗвонки подаваться не будут!", "LessonBell", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) == DialogResult.Yes)
                    {
                        Hide();
                        ShowIcon = false;
                        SaveSettings(); // выходим из программы
                    }
                    else
                    {
                        e.Cancel = true; // не выходим из программы
                    }
                }
            }

            if (e.CloseReason == CloseReason.WindowsShutDown || e.CloseReason == CloseReason.TaskManagerClosing)
            {
                e.Cancel = true; // отменяем закрытие программы
                FormStopShutdown Mem = new FormStopShutdown(this);
                Mem.Owner = this;
                Mem.ShowDialog(); // показываем форму на весь экран что оставим колледж без звонков

                if (Mem.DialogResult == DialogResult.OK) // если подверждено ВЫКЛЮЧЕНИЕ КОМПА
                {
                    reboot r = new reboot(); // Создаем объект класса reboot
                    r.halt(false, false); // мягкое выключение
                    halt = true; // чтобы не было окна "вы дйствительно хоитте выйти?"
                    Close(); // закрываемся
                }
            }*/
        }
        #endregion
        // -----------------------------------
        // -----------------------------------
        #region Подача звонков

        private void PlayBell(string kak, string FileBell) // Подать звонок - \\\\\ точно порядок апрель cscore
        {
            try
            {
                Log.Write("[ЗВОНОК] Подан звонок " + kak);
                NowZvonitZvonok = true;

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
                EqualizerFilter filt1 = _equalizer.SampleFilters[0];
                filt1.AverageGainDB = -30;

                EqualizerFilter filt2 = _equalizer.SampleFilters[1];
                filt2.AverageGainDB = -30;

                //EqualizerFilter filt3 = _equalizer.SampleFilters[9]; // 16 kHz
                //filt3.AverageGainDB = -30;

                _soundOut.Play();
                
                Log.Write("Длительность звонка: " + source.GetLength());
                while (source.GetPosition() < source.GetLength() - new TimeSpan(0, 0, 1))
                {
                    Thread.Sleep(200);
                }

                NowZvonitZvonok = false; // Звонок закончил звучать
                Log.Write("[ЗВОНОК] Звонок закончил звучать");
            }
            catch (Exception e)
            {
                Log.Write("Ошибка при подаче звонка\n\n" + e.Message + "\n\n" + e.ToString());
                System.Windows.Forms.MessageBox.Show("Ошибка при подаче звонка\n\n" + e.Message + "\n\n" + e.ToString(), "LessonBell",
                    MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, System.Windows.Forms.MessageBoxOptions.ServiceNotification);
            }
        }

        private void Bell_Start() // Выключить плееры и дать звонок на пару - [ПОРЯДОК 19 МАРТА]
        {
            CheckFolderSelectedMusic();
            reservVolume = axWmpMusic.settings.volume; // Резервно сохраняем громкость

            if (axWmpMusic.playState == WMPLib.WMPPlayState.wmppsPlaying)
            {
                while (axWmpMusic.settings.volume > 0) // Пока громкость больше 0 - уменьшаем
                {
                    axWmpMusic.settings.volume -= 1; // Плавно затушить громкость
                    Thread.Sleep(80);
                }
            }
            axWmpDops.Ctlcontrols.stop(); // Остановили
            axWmpMusic.Ctlcontrols.stop();
            axWmpMusic.settings.volume = reservVolume; // вернули громкость

            if (MuzPlayerOn) // Если включен сторонний плеер - выключить его
            {
                MusicPlayer.CloseMainWindow();
                MusicPlayer.Close();
                Thread.Sleep(500);
            }

            PlayBell("на урок, программно", MelodyStartBell); // Звонок на урок

            StopAndClearPlayer(false);
        }

        private void Bell_End(int i) // Включить плеер после звонка - [ПОРЯДОК 19 МАРТА]
        {
            CheckFolderSelectedMusic();
            axWmpDops.Ctlcontrols.stop(); // Остановили
            axWmpMusic.Ctlcontrols.stop(); // Остановили

            if (ActiveMuzNaPeremenax && AllBells[i].MuzActive)
            {
                Thread lm = new Thread(delegate () { AddMusic(false); });
                lm.IsBackground = true;
                lm.Start();
            }

            PlayBell("с урока, программно", MelodyEndBell); // Звонок с урока
            
            if (ActiveMuzNaPeremenax && AllBells[i].MuzActive)
            {
                if (i != AllBells.Count - 1) // если звонок не последний
                {
                    if (ActiveOtherPlayer)
                    {
                        MusicPlayer = Process.Start(FileOtherPlayer);
                        MuzPlayerOn = true;
                    }
                    else
                    {
                        axWmpMusic.Ctlcontrols.play();
                    }
                }
                else
                {
                    if (MinPlayMusicAfterLastBell > new TimeSpan(0, 0, 0))
                    {
                        if (ActiveOtherPlayer)
                        {
                            MusicPlayer = Process.Start(FileOtherPlayer);
                            MuzPlayerOn = true;
                        }
                        else
                        {
                            axWmpMusic.Ctlcontrols.play();
                        }

                        if (timerEndMusic.Enabled)
                            timerEndMusic.Stop();
                        Log.Write("Прозвенел последний звонок! Выключение музыки через " + MinPlayMusicAfterLastBell);

                        timerEndMusic.Interval = Convert.ToInt32(MinPlayMusicAfterLastBell.TotalMilliseconds);
                        timerEndMusic.Start();
                    }
                }
            }
        }

        private void Stop()
        {
            if (_soundOut != null)
            {
                _soundOut.Stop();
                _soundOut.Dispose();
                _equalizer.Dispose();
                _soundOut = null;
            }
        }

        private void StopAndClearPlayer(bool NeedVolume)
        {
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

        #endregion
        // -----------------------------------
        // -----------------------------------
        #region Вкладка Настройки
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
                        shortcut.Description = "Ярлык LessonBell";

                        //путь к самой программе
                        shortcut.TargetPath = System.Windows.Forms.Application.ExecutablePath;

                        //Создаем ярлык
                        shortcut.Save();
                        Thread.Sleep(20);
                        if (!System.IO.File.Exists(Environment.GetFolderPath(Environment.SpecialFolder.Startup) + @"\LessonBell.lnk"))
                        {
                            Log.Write("Ярлык в папке 'Автозагрузка' не был создан по какой то причине....");
                            Process.Start(Environment.CurrentDirectory);
                            Process.Start(Environment.GetFolderPath(Environment.SpecialFolder.Startup));
                            System.Windows.Forms.MessageBox.Show("Ярлык в папке 'Автозагрузка' не был создан!\n\nСоздайте ярлык для 'LessonBell' из папки приложения в папке Автозагрузка вручную!",
                                "LessonBell", MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1, System.Windows.Forms.MessageBoxOptions.ServiceNotification);
                        }
                        else
                        {

                            SettingsINI.WriteINI("Main", "AutoRun", cbxAutoStart.IsChecked.ToString());
                        }
                    }
                    catch (Exception l)
                    {
                        Log.Write($"Ошибка при создании ярлыка программы в папке 'Автозагрузка' [{l.Message}]");
                        Log.Write(l.ToString());
                        System.Windows.Forms.MessageBox.Show(l.Message);
                    }
                }
                else
                {
                    // выключен
                    try
                    {
                        Log.Write("Удаление ярлыка программы из папки 'Автозагрузка'");
                        System.IO.File.Delete(Environment.GetFolderPath(Environment.SpecialFolder.Startup) + @"\LessonBell.lnk");
                        Properties.Settings.Default.AutoRun = false;
                        Properties.Settings.Default.Save();
                    }
                    catch (Exception l)
                    {
                        Log.Write($"Ошибка при удалении ярлыка программы из папки 'Автозагрузка' [{l.Message}]");
                        Log.Write(l.ToString());
                        System.Windows.Forms.MessageBox.Show(l.Message);
                    }
                }
            }
        }

        private void cbxRunMinimized_Checked(object sender, RoutedEventArgs e) // Запускать свернутой -- [ПОРЯДОК 22 МАРТА]
        {
            if (!NowLoadSettings)
            {
                Properties.Settings.Default.RunMinimized = (bool)cbxRunMinimized.IsChecked;
                Properties.Settings.Default.Save();
            }
        }

       
        private void btnEditMusicFolder_Click(object sender, RoutedEventArgs e) // Изменить папку с музыкой -- [ПОРЯДОК 19 МАРТА]
        {
            FolderBrowserDialog folderBrowserDialog1 = new FolderBrowserDialog();
            folderBrowserDialog1.Description = "Выберите папку с музыкой для перемен";
            folderBrowserDialog1.ShowNewFolderButton = false;

            if (folderBrowserDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string ReservFolder = FolderMusic;
                try
                {
                    FolderMusic = folderBrowserDialog1.SelectedPath;
                    Log.Write($"Изменена папка с музыкой на [{FolderMusic}]");
                    //tbxMusicFolder.Text = System.IO.Path.GetFileName(FolderMusic);
                    tbxMusicFolder.Text = Path.GetFileName(FolderMusic);
                    Properties.Settings.Default.FolderMusic = FolderMusic;
                    Properties.Settings.Default.Save();
                }
                catch (Exception w)
                {
                    FolderMusic = ReservFolder;
                    tbxMusicFolder.Text = System.IO.Path.GetFileName(FolderMusic);
                    Properties.Settings.Default.FolderMusic = ReservFolder;
                    Properties.Settings.Default.Save();
                    Log.Write($"Ошибка при изменении папки с музыкой [{w.Message}]");
                    Log.Write(w.ToString());
                    new Thread(() => System.Windows.Forms.MessageBox.Show($"Ошибка при изменении папки с музыкой\n\n {w.Message}\n\n\n{w.ToString()}", "LessonBell - Ошибка",
                        MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, System.Windows.Forms.MessageBoxOptions.ServiceNotification)).Start();
                }
            }
        }

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
            MinPlayMusicAfterLastBell = new TimeSpan(0, int.Parse(tbxMinMusAfterBells.Text), 0);
            Properties.Settings.Default.PoslednixPesen = int.Parse(tbxMinMusAfterBells.Text);
            Properties.Settings.Default.Save();
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
            MinPlayMusicAfterLastBell = new TimeSpan(0, int.Parse(tbxMinMusAfterBells.Text), 0);
            Properties.Settings.Default.PoslednixPesen = int.Parse(tbxMinMusAfterBells.Text);
            Properties.Settings.Default.Save();
        }

        private void btnMinMusUp_Click(object sender, RoutedEventArgs e) // Добавить время
        {
            byte current = byte.Parse(tbxMinMusAfterBells.Text);
            if (current <= 58 && current >= 0)
            {
                current++;
                tbxMinMusAfterBells.Text = current.ToString();

                if (AllBells.Count > 0)
                    TimeOffMusic = AllBells[AllBells.Count - 1].TimeEnd + new TimeSpan(0, current, 0);
            }
        }

        private void btnMinMusDown_Click(object sender, RoutedEventArgs e) // Убрать время
        {
            byte current = byte.Parse(tbxMinMusAfterBells.Text);
            if (current <= 59 && current >= 1)
            {
                current--;
                tbxMinMusAfterBells.Text = current.ToString();
                if (AllBells.Count > 0)
                    TimeOffMusic = AllBells[AllBells.Count - 1].TimeEnd + new TimeSpan(0, current, 0);
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


        private void btnEditMelodyStart_Click(object sender, RoutedEventArgs e) // Изменить звонок НА урок -- [ПОРЯДОК 19 МАРТА]
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();

            openFileDialog1.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            openFileDialog1.Title = "Выберите мелодию звонка НА УРОК";
            openFileDialog1.Filter = "All files (*.*)|*.*|Мелодии MP3 (*.mp3)|*.mp3";
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
                            new Thread(() => System.Windows.Forms.MessageBox.Show("Выбранная Вами мелодия звонка была скопирована в директорию программы.\n\nВыбрано: " + openFileDialog1.SafeFileName,
                                "LessonBell — Сигнал выбран", MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1, System.Windows.Forms.MessageBoxOptions.ServiceNotification)).Start();

                            MelodyStartBell = Melody; // Мелодия НА УРОК
                            tbxMelodyStart.Text = Path.GetFileName(MelodyStartBell);
                            Log.Write($"Изменена мелодия звонка на урок на [{System.IO.Path.GetFileName(MelodyStartBell)}]");
                            SettingsINI.WriteINI("Main", "MelodyStartBell", Melody);
                        }
                        else // Файл не скопировался
                        {
                            new Thread(() => System.Windows.Forms.MessageBox.Show("Файл звонка не скопировался в директорию программы!\nВозможно у программы нет доступа или файл занят другой программой.\nИзменения отменены.",
                                "LessonBell", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, System.Windows.Forms.MessageBoxOptions.ServiceNotification)).Start();
                        }
                    }
                    else
                    {
                        new Thread(() => System.Windows.Forms.MessageBox.Show("Вы выбрали уже выбранную мелодию!\nИзменения отменены.",
                            "LessonBell", MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1, System.Windows.Forms.MessageBoxOptions.ServiceNotification)).Start();
                    }
                }
                catch (Exception w)
                {
                    MelodyStartBell = ReservMelody;
                    tbxMelodyStart.Text = System.IO.Path.GetFileName(MelodyStartBell);
                    Properties.Settings.Default.MelodyStart = ReservMelody;
                    Properties.Settings.Default.Save();
                    Log.Write($"Ошибка при копировании мелодии звонка [{w.Message}]");
                    Log.Write(w.ToString());
                    new Thread(() => System.Windows.Forms.MessageBox.Show($"Ошибка при копировании мелодии звонка\n\n {w.Message}\n\n\n{w.ToString()}",
                        "LessonBell - Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, System.Windows.Forms.MessageBoxOptions.ServiceNotification)).Start();
                }
            }

        }

        private void btnEditMelodyEnd_Click(object sender, RoutedEventArgs e) // Изменить звонок С урока -- [ПОРЯДОК 19 МАРТА]
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();

            openFileDialog1.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            openFileDialog1.Title = "Выберите мелодию звонка С УРОКА";
            openFileDialog1.Filter = "All files (*.*)|*.*|Мелодии MP3 (*.mp3)|*.mp3";
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
                            new Thread(() => System.Windows.Forms.MessageBox.Show("Выбранная Вами мелодия звонка была скопирована в директорию программы.\n\nВыбрано: " + openFileDialog1.SafeFileName,
                                "LessonBell — Сигнал выбран", MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1, System.Windows.Forms.MessageBoxOptions.ServiceNotification)).Start();

                            MelodyEndBell = Melody; // Мелодия НА УРОК
                            tbxMelodyEnd.Text = Path.GetFileName(MelodyEndBell);
                            Log.Write($"Изменена мелодия звонка с урока на [{System.IO.Path.GetFileName(MelodyEndBell)}]");
                            SettingsINI.WriteINI("Main", "MelodyEndBell", Melody);
                            var source = CodecFactory.Instance.GetCodec(MelodyEndBell); // Считали информацию
                            DurationEndBell = source.GetLength();
                        }
                        else // Файл не скопировался
                        {
                            new Thread(() => System.Windows.Forms.MessageBox.Show("Файл звонка не скопировался в директорию программы!\nВозможно у программы нет доступа или файл занят другой программой.\nИзменения отменены.",
                                "LessonBell", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, System.Windows.Forms.MessageBoxOptions.ServiceNotification)).Start();
                        }
                    }
                    else
                    {
                        new Thread(() => System.Windows.Forms.MessageBox.Show("Вы выбрали уже выбранную мелодию!\nИзменения отменены.",
                            "LessonBell", MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1, System.Windows.Forms.MessageBoxOptions.ServiceNotification)).Start();
                    }
                }
                catch (Exception w)
                {
                    MelodyEndBell = ReservMelody;
                    tbxMelodyEnd.Text = System.IO.Path.GetFileName(MelodyEndBell);
                    Properties.Settings.Default.MelodyStart = ReservMelody;
                    Properties.Settings.Default.Save();
                    Log.Write($"Ошибка при копировании мелодии звонка [{w.Message}]");
                    Log.Write(w.ToString());
                    new Thread(() => System.Windows.Forms.MessageBox.Show($"Ошибка при копировании мелодии звонка\n\n {w.Message}\n\n\n{w.ToString()}",
                        "LessonBell - Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, System.Windows.Forms.MessageBoxOptions.ServiceNotification)).Start();
                }
            }
        }


        private void btnBellStartNow_Click(object sender, RoutedEventArgs e) // Подать звонок НА урок -- [ПОРЯДОК 19 МАРТА]
        {
            Thread thread = new Thread(delegate () { PlayBell("на урок, вручную", MelodyStartBell); });
            thread.IsBackground = true;
            thread.Start();
        }

        private void btnBellEndNow_Click(object sender, RoutedEventArgs e) // Подать звонок С урока -- [ПОРЯДОК 19 МАРТА]
        {
            Thread thread = new Thread(delegate () { PlayBell("с урока, вручную", MelodyEndBell); });
            thread.IsBackground = true;
            thread.Start();
        }
        #endregion
        // -----------------------------------
        // -----------------------------------
        #region Вкладка Усилитель, вывод состояния плееров

        private void ControlYsil() // -- [ПОРЯДОК 19 МАРТА]
        {
            if (workComPort != null) // Если порт не пустой
            {
                if (YsilAuto) // Если вкл\выкл автоматически
                {
                    if (StateYsil == false && TimeNow > YsilAutoTimeOn && TimeNow < YsilAutoTimeOff) // Попадаем в рабочее время
                    {
                        OnYsil("Автоматический расчёт времени"); // Сразу включить
                    }

                    if (StateYsil == true && (TimeNow < YsilAutoTimeOn || TimeNow > YsilAutoTimeOff)) // Попадаем в нерабочее время
                    {
                        OffYsil("Автоматический расчёт времени"); // Сразу выключить
                    }
                }

                if (YsilTime) // Если вкл\выкл по заданному времени
                {
                    if (StateYsil == false && TimeNow > YsilFixTimeOn && TimeNow < YsilFixTimeOff) // Попадаем в рабочее время
                    {
                        OnYsil("По заданному времени"); // Сразу включить
                    }

                    if (StateYsil == true && (TimeNow < YsilFixTimeOn || TimeNow > YsilFixTimeOff)) // Попадаем в нерабочее время
                    {
                        OffYsil("По заданному времени"); // Сразу выключить
                    }
                }
            }
        }

        private bool EweDop() // Будут ли еще доп.сигналы -- [ПОРЯДОК 19 МАРТА]
        {
            for (byte i = 0; i < AllDops.Count; i++)
            {
                if (TimeNow < AllDops[i].Time) // Если сейчас < элементу доп
                {
                    return true; // еще будут доп.сигналы
                }
            }
            return false;
        }

        private void CalcTimeYsilOnOff()
        {
            if (AllBells.Count > 0)
            {
                // найти самый ранний звонок, самый ранний доп.сигнал, самую ранную музыку до уроков  и включить перед любым
                TimeSpan PosledniyZvonok = AllBells[AllBells.Count - 1].TimeEnd;
                YsilAutoTimeOn = AllBells[0].TimeStart; // Самый ранний звонок
                YsilAutoTimeOff = PosledniyZvonok; // Самый поздний звонок

                if (MusicDoPar.Count > 0 && YsilAutoTimeOn > MusicDoPar[0].Time) // Если муз.до уроков есть и ее время раньше
                {
                    YsilAutoTimeOn = MusicDoPar[0].Time; // Самая ранняя музыка до уроков
                }

                if (AllDops.Count > 0) // Если доп.сигналы есть
                {
                    for (int i = 0; i < AllDops.Count; i++) // перебираем
                    {
                        var source = CodecFactory.Instance.GetCodec(AllDops[i].Signal); // Считали информацию о допе
                        TimeSpan TimeEndPlayDop = AllDops[i].Time + source.GetLength(); // Время окончания звучания допа

                        if (YsilAutoTimeOn > AllDops[i].Time) // Если время включения позже доп.сигнала
                        {
                            YsilAutoTimeOn = AllDops[i].Time; // Самый ранний доп.сигнал
                        }

                        if (YsilAutoTimeOff < TimeEndPlayDop) // Если время выключения раньше времени окончания звучания
                        {
                            YsilAutoTimeOff = new TimeSpan(TimeEndPlayDop.Hours, TimeEndPlayDop.Minutes, TimeEndPlayDop.Seconds); // Делоем его
                        }
                    }
                }
                YsilAutoTimeOn -= new TimeSpan(0, 0, 20);
                if (YsilAutoTimeOff == PosledniyZvonok) // если время выключения осталось = последнему звонку
                {
                    YsilAutoTimeOff += new TimeSpan(DurationEndBell.Hours, DurationEndBell.Minutes, DurationEndBell.Seconds + 1); // Добавили время звучания звонка с пары

                    if (ActiveMuzNaPeremenax && AllBells[AllBells.Count - 1].MuzActive) // Если музыка будет
                    {
                        YsilAutoTimeOff += MinPlayMusicAfterLastBell; // Добавляем время звучания музыки после уроков

                        if (!ActiveOtherPlayer) // Если играем по внутреннему плееру
                        {
                            double volume = axWmpMusic.settings.volume;
                            double TimeWait = Math.Ceiling((volume * 0.09)); // 2,160 = 3

                            YsilAutoTimeOff += TimeSpan.FromSeconds(TimeWait); // Добавили время затухания музыки
                        }
                    }
                }

                Log.Write($"Расчет автоматической работы усилителя: Включение: [{YsilAutoTimeOn}], Выключение: [{YsilAutoTimeOff}]."); // .ToString("hh':'mm")
                
                UpdateLabelsSvodkaTimeYsil();
            }
            else
            {


                YsilAutoTimeOn = new TimeSpan(0, 1, 10);
                YsilAutoTimeOff = new TimeSpan(0, 1, 11);
            }
        }

        private void UpdateLabelsSvodkaTimeYsil()
        {
            if (workComPort == null) // Если СОМ порт пуст
            {
                lbYsilAutoTimeOn.Text = labelTimeOnYsil.Text = "——";
                lbYsilAutoTimeOff.Text = labelTimeOffYsil.Text = "——";

                labelModeOnYsil.Text = " (не выбран порт управления)";
                labelModeOffYsil.Text = " (не выбран порт управления)";
            }
            else
            {
                if (YsilAuto)
                {
                    lbYsilAutoTimeOn.Text = labelTimeOnYsil.Text = YsilAutoTimeOn.ToString();
                    lbYsilAutoTimeOff.Text = labelTimeOffYsil.Text = YsilAutoTimeOff.ToString();

                    labelModeOnYsil.Text = " (автоматический расчёт)";
                    labelModeOffYsil.Text = " (автоматический расчёт)";
                }

                if (YsilTime)
                {
                    labelTimeOnYsil.Text = YsilFixTimeOn.ToString();
                    labelTimeOffYsil.Text = YsilFixTimeOff.ToString();

                    labelModeOnYsil.Text = " (фиксированное время)";
                    labelModeOffYsil.Text = " (фиксированное время)";
                }

                if (YsilHands)
                {
                    labelTimeOnYsil.Text = "——";
                    labelTimeOffYsil.Text = "——";

                    labelModeOnYsil.Text = " (вручную)";
                    labelModeOffYsil.Text = " (вручную)";

                }
                if (YsilNoControl)
                {
                    lbYsilAutoTimeOn.Text = labelTimeOnYsil.Text = "——";
                    lbYsilAutoTimeOff.Text = labelTimeOffYsil.Text = "——";

                    labelModeOnYsil.Text = " (функция не используется)";
                    labelModeOffYsil.Text = " (функция не используется)";
                }
            }
        }

        private void OnYsil(string kak)
        {
            if (workComPort != null)
            {
                Log.Write($"[Усилитель] ON - Усилитель включен [{kak}]");
                StateYsil = true;
                try
                {
                    workComPort.RtsEnable = true;
                    workComPort.Open();
                    lbYsilState.Foreground = Brushes.Green;
                    lbYsilState.Text = "ВКЛЮЧЕН";
                }
                catch (Exception e)
                {
                    Log.Write(e.Message);
                    Log.Write(e.ToString());
                    new Thread(() => System.Windows.Forms.MessageBox.Show("Ошибка при усилителе: " + e.Message + "\n\n\n" + e.ToString())).Start();
                }
            }
        }

        private void OffYsil(string kak)
        {
            if (workComPort != null)
            {
                Log.Write($"[Усилитель] OFF - Усилитель выключен [{kak}]");
                StateYsil = false;
                try
                {
                    workComPort.RtsEnable = false;
                    workComPort.Close();
                    lbYsilState.Foreground = Brushes.Firebrick;
                    lbYsilState.Text = "ВЫКЛЮЧЕН";
                }
                catch (Exception e)
                {
                    Log.Write(e.Message);
                    Log.Write(e.ToString());
                    new Thread(() => System.Windows.Forms.MessageBox.Show("Ошибка при усилителе: " + e.Message + "\n\n\n" + e.ToString())).Start();
                }
            }
        }

        private void LoadComPorts() // Загрузить порты компа
        {
            comPorts = SerialPort.GetPortNames();

            comboYsilCom.Items.Clear();
            comboYsilCom.Items.Add("Не выбран");
            comboYsilCom.SelectedIndex = 0;
            
            foreach (var Com in comPorts)
                comboYsilCom.Items.Add(Com);

            if (comboYsilCom.Items.Count > 1)
            {
                for (int i = 1; i < comboYsilCom.Items.Count; i++) // перебираем элементы
                {
                    if (comboYsilCom.Items[i].ToString() == SelectedCOM) // если в списке есть ранее выбранный СОМ порт
                    {
                        comboYsilCom.SelectedIndex = i;
                        workComPort = new SerialPort(SelectedCOM, 9600, Parity.None, 8, StopBits.One);
                        Log.Write("СОМ - Подключен ранее выбранный СОМ порт: " + SelectedCOM);
                        break;
                    }
                }
            }
            else
            {
                StateYsil = false;
                workComPort = null;
                lbYsilState.Foreground = Brushes.Firebrick;
                lbYsilState.Text = "ВЫКЛЮЧЕН";
            }
        }
        
        private void comboYsilCom_SelectionChanged(object sender, SelectionChangedEventArgs e) // Изменен СОМ порт
        {
            if (comboYsilCom.SelectedIndex >= 1) // 0 элемент - не выбран
            {
                string aa = SelectedCOM;
                SelectedCOM = comboYsilCom.Items[comboYsilCom.SelectedIndex].ToString();
                workComPort = new SerialPort(SelectedCOM, 9600, Parity.None, 8, StopBits.One);
                
                Properties.Settings.Default.ComPort = SelectedCOM;
                Properties.Settings.Default.Save();
                Log.Write("СОМ - Выбран новый СОМ порт: " + workComPort.PortName + ", старый: " + aa);
            }
            else
            {
                if (StateYsil == true)
                {
                    OffYsil("Не выбран СОМ порт!");
                }
                workComPort = null; // выбрано "Не выбран"
            }
            if (!NowLoadSettings)
            {
                UpdateLabelsSvodkaTimeYsil();
            }
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            HwndSource source = PresentationSource.FromVisual(this) as HwndSource;
            source.AddHook(WndProc);
        }

        IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            // Handle messages...
            if (wParam.ToInt32() == 0x8000)
            {
                NotifyIcon kek = new NotifyIcon();
                kek.ShowBalloonTip(8000, "LessonBell", "В компуктер было подключено USB устройство", ToolTipIcon.Info);
                LoadComPorts();
                //новое usb подключено
            }
            if (wParam.ToInt32() == 0x8004)
            {
                NotifyIcon kek = new NotifyIcon();
                kek.ShowBalloonTip(8000, "LessonBell", "Из компуктера вынемено USB устройство", ToolTipIcon.Info);
                LoadComPorts();
                // usb отключено
            }
            return IntPtr.Zero;
        }

        private void btnYsilHandsOn_Click(object sender, RoutedEventArgs e) // Вручную ВКЛЮЧИТЬ усилитель -- [ПОРЯДОК 23 МАРТА]
        {
            OnYsil("Кнопка вкл.вручную");
        }

        private void btnYsilHandsOff_Click(object sender, RoutedEventArgs e) // Вручную ВЫКЛЮЧИТЬ усилитель -- [ПОРЯДОК 23 МАРТА]
        {
            OffYsil("Кнопка выкл.вручную");
        }


        private void btnYsilTimeEdit_Click(object sender, RoutedEventArgs e) // Изменить ВРЕМЯ усилителя -- [ПОРЯДОК 23 МАРТА]
        {
            btnYsilTimeEdit.Visibility = Visibility.Hidden;
            btnYsilTimeSave.Visibility = btnYsilTimeCancel.Visibility = Visibility.Visible; // Спрятали изменить, показали сохранить\отменить

            ReservedTimeOnYsil = tbxYsilTimeOn.Text;
            ReservedTimeOffYsil = tbxYsilTimeOff.Text; // резервно сохранили старые значения
            
            tbxYsilTimeOn.IsReadOnly = tbxYsilTimeOff.IsReadOnly = false; // разрешили редактирование
        }

        private void btnYsilTimeSave_Click(object sender, RoutedEventArgs e) // Сохранить ВРЕМЯ усилителя -- [ПОРЯДОК 23 МАРТА]
        {
            btnYsilTimeEdit.Visibility = Visibility.Visible;
            btnYsilTimeSave.Visibility = btnYsilTimeCancel.Visibility = Visibility.Hidden; // Спрятали

            tbxYsilTimeOn.IsReadOnly = tbxYsilTimeOff.IsReadOnly = true; // запретили редактирование

            YsilFixTimeOn = TimeSpan.Parse(tbxYsilTimeOn.Text);
            YsilFixTimeOff = TimeSpan.Parse(tbxYsilTimeOff.Text);
            Properties.Settings.Default.YsilTimeOn = YsilFixTimeOn;
            Properties.Settings.Default.YsilTimeOff = YsilFixTimeOff;
            Properties.Settings.Default.Save();
            Log.Write($"Время усилителя изменено с [вкл: {ReservedTimeOnYsil}, выкл: {ReservedTimeOffYsil}] на [вкл: {YsilFixTimeOn}, выкл: {YsilFixTimeOff}]");
            CalcTimeYsilOnOff();
        }

        private void btnYsilTimeCancel_Click(object sender, RoutedEventArgs e) // Отменить ВРЕМЯ усилителя -- [ПОРЯДОК 23 МАРТА]
        {
            btnYsilTimeEdit.Visibility = Visibility.Visible;
            btnYsilTimeSave.Visibility = btnYsilTimeCancel.Visibility = Visibility.Hidden; // Спрятали

            tbxYsilTimeOn.Text = ReservedTimeOnYsil;
            tbxYsilTimeOff.Text = ReservedTimeOffYsil; // вернули старые значения
            
            tbxYsilTimeOn.IsReadOnly = tbxYsilTimeOff.IsReadOnly = true; // запретили редактирование
        }
        

        private void rbYsilNoControl_Checked(object sender, RoutedEventArgs e) // Не контролировать -- [ПОРЯДОК 23 МАРТА]
        {
            lbYsilNoControl.IsEnabled = YsilNoControl = (bool)rbYsilNoControl.IsChecked;
            if (!NowLoadSettings)
            {
                Properties.Settings.Default.YsilNoControl = YsilNoControl;
                Properties.Settings.Default.Save();
                if (YsilNoControl && workComPort != null && StateYsil) // Если усилитель включен
                {
                    CalcTimeYsilOnOff();
                    OffYsil("Выбран режим 'Не управлять'");
                }
            }
        }

        private void rbYsilAuto_Checked(object sender, RoutedEventArgs e) // Автоматически -- [ПОРЯДОК 23 МАРТА]
        {
            lbYsilAutoTime1.IsEnabled = lbYsilAutoTime2.IsEnabled = lbYsilAutoTime3.IsEnabled =
                    lbYsilAutoTime4.IsEnabled = lbYsilAutoTimeOn.IsEnabled = lbYsilAutoTimeOff.IsEnabled = YsilAuto = (bool)rbYsilAuto.IsChecked;
            if (!NowLoadSettings)
            {
                Properties.Settings.Default.YsilAuto = YsilAuto;
                Properties.Settings.Default.Save();

                if (YsilAuto)
                {
                    CalcTimeYsilOnOff();
                    ControlYsil();
                }
            }

        }

        private void rbYsilTime_Checked(object sender, RoutedEventArgs e) // По времени -- [ПОРЯДОК 23 МАРТА]
        {
            lbYsilTimeOn.IsEnabled = lbYsilTimeOff.IsEnabled = tbxYsilTimeOn.IsEnabled = tbxYsilTimeOff.IsEnabled = YsilTime = (bool)rbYsilTime.IsChecked;
            if (!NowLoadSettings)
            {
                Properties.Settings.Default.YsilTime = YsilTime;
                Properties.Settings.Default.Save();

                if (!YsilTime) // если галка снята
                {
                    // если режим редактирования вкл, выключить его отменой
                    if (btnYsilTimeCancel.Visibility == Visibility.Visible) // если кнопка отмена видна т.е. редактируем
                    {
                        btnYsilTimeCancel_Click(btnYsilTimeCancel, null);
                    }
                    btnYsilTimeEdit.Visibility = Visibility.Hidden;
                }
                else
                {
                    btnYsilTimeEdit.Visibility = Visibility.Visible;
                }

                if (YsilTime)
                {
                    CalcTimeYsilOnOff();
                    ControlYsil();
                }
            }

        }

        private void rbYsilHands_Checked(object sender, RoutedEventArgs e) // Вручную -- [ПОРЯДОК 23 МАРТА]
        {
            btnYsilHandsOn.IsEnabled = btnYsilHandsOff.IsEnabled = YsilHands = (bool)rbYsilHands.IsChecked;
            if (!NowLoadSettings)
            {
                Properties.Settings.Default.YsilHands= YsilHands;
                Properties.Settings.Default.Save();
                if (YsilHands)
                {
                    CalcTimeYsilOnOff();
                    ControlYsil();
                }
            }
        }

        #endregion
        // -----------------------------------
        // -----------------------------------
        #region Вкладка Расписания звонков
        private void AddRasp_Click(object sender, RoutedEventArgs e) // Добавить расписание
        {
            AllRasps.Add(new RaspZvonkov() { NameRasp = "Новое добавленное /без настроек/" });
            AllRasps[AllRasps.Count - 1].Number = AllRasps.Count;
            AllRasps[AllRasps.Count - 1].AoPEdited += MainWindow_AoPEdited;
        }

        private void EditSelectedRasp_Click(object sender, RoutedEventArgs e) // Изменить выбранное расписание
        {
            int Selected = NewlistViewRaspsZvonkov.SelectedIndex;
            Log.Write("EDIT Selected: " + Selected);
            if (Selected >= 0)
            {
                WindowEditRasp edit = new WindowEditRasp(this, AllRasps[Selected], Log.File);
                edit.Owner = this;

                edit.ShowDialog();

                if (edit.DialogResult == true)
                {
                    AllRasps[Selected] = RaspVr;
                    GetTimeBells("Изменено расписание " + RaspVr.NameRasp);
                    NewlistViewRaspsZvonkov.Items.Refresh();

                    // сохранить в файл все расписания
                    NewSaveAllRasps();
                    RaspVr = null;
                }
            }
        }

        private void DeleteSelectedRasp_Click(object sender, RoutedEventArgs e) // Удалить выбранное расписание
        {
            int Selected = NewlistViewRaspsZvonkov.SelectedIndex;
            if (Selected >= 0)
            {
                string NameDeletedRasp = AllRasps[Selected].NameRasp;
                AllRasps.RemoveAt(Selected);
                GetTimeBells("Удалено расписание: " + NameDeletedRasp);

                for (int i = Selected; i < AllRasps.Count; i++)
                {
                    AllRasps[i].Number = i + 1;
                }

                NewlistViewRaspsZvonkov.Items.Refresh();
                // сохранить в файл все расписания
                NewSaveAllRasps();
            }
        }

        private void MainWindow_AoPEdited(object sender, string msg) // Изменена Активность или приоритет в расписании
        {
            GetTimeBells(msg + " " + (sender as RaspZvonkov).NameRasp);
        }
        #endregion
        // -----------------------------------
        // -----------------------------------
        #region Вкладка плееры

        private void cbxMuzNaPeremenax_Checked(object sender, RoutedEventArgs e) // Активность музыки на переменах - [ПОРЯДОК 19 МАРТА]
        {
            ActiveMuzNaPeremenax = (bool)cbxPlayMusic.IsChecked;

            if (ActiveMuzNaPeremenax) // если включили
            {
                OnMusicNow(); // проверить нужна ли музыка сейчас и включить
            }
            else
            {
                StopAndClearPlayer(false); // убрать из плеера всё
                //axWmpMusic.currentPlaylist = axWmpMusic.playlistCollection.newPlaylist("NullLessonBellplst"); // впендюрили пустой плейлист
            }

            if (!NowLoadSettings)
            {
                Properties.Settings.Default.PlayMuzNaPeremenax = ActiveMuzNaPeremenax; // сохранить настройку
                Properties.Settings.Default.Save();
            }
        }
        
        private void cbxOtherPlayer_Checked(object sender, RoutedEventArgs e) // Активность стороннего плеера - [ПОРЯДОК 20 июня]
        {
            btnChangeOtherPlayer.IsEnabled = tbxOtherPlayer.IsEnabled = ActiveOtherPlayer = (bool)cbxOtherPlayer.IsChecked;
            
            lbModeGetMusic.IsEnabled = comboModePlayMusic.IsEnabled = !ActiveOtherPlayer;

            if (!NowLoadSettings && AllBells.Count > 0)
            {
                CalcTimeYsilOnOff();
            }

            if (!NowLoadSettings)
            {
                Properties.Settings.Default.ActiveOtherPlayer = ActiveOtherPlayer; // сохранить настройку
                Properties.Settings.Default.Save();
            }
        }

        private void cbxDops_Checked(object sender, RoutedEventArgs e) // Активность доп.сигналов
        {
            ActiveDops = (bool)cbxPlayDops.IsChecked;

            if (!NowLoadSettings)
            {
                Properties.Settings.Default.PlayDops = ActiveDops;
                Properties.Settings.Default.Save();
            }
        }
        
        private void btnChangeOtherPlayer_Click(object sender, RoutedEventArgs e) // Изменить сторонний плеер - [ПОРЯДОК 19 МАРТА]
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
                Properties.Settings.Default.OtherPlayer = FileOtherPlayer;
                Properties.Settings.Default.Save();
            }
        }

        private void comboModePlayMusic_SelectionChanged(object sender, SelectionChangedEventArgs e) // Режим проигрывания музыки
        {
            if (comboModePlayMusic.SelectedIndex == 0)
            {
                axWmpMusic.settings.setMode("shuffle", true); // Рандом ВКЛ
                Properties.Settings.Default.ModePlayMusic = 0;
            }
            else
            {
                axWmpMusic.settings.setMode("shuffle", false); // Рандом ВЫКЛ
                Properties.Settings.Default.ModePlayMusic = 1;
            }
            Properties.Settings.Default.Save();
        }

        private void AxWmpMusic_PlayStateChange(object sender, AxWMPLib._WMPOCXEvents_PlayStateChangeEvent e)
        {
            if (e.newState == 3) // если новый статус - проигрывание
            {
                // Записать название проигрываемой песни в файл
                PlstLog.Write("Playing: " + Path.GetFileName(axWmpMusic.currentMedia.sourceURL));

                //string howPlaying = Path.GetFileName(axWmpMusic.currentMedia.sourceURL);
                //for (int i = 0; i < lbPlaylistMusic.Items.Count; i++)
                //{
                //    if (lbPlaylistMusic.Items[i].ToString() == howPlaying)
                //    {
                //        lbPlaylistMusic.SelectedIndex = i;
                //    }
                //}
            }
        }

        #endregion
        // -----------------------------------
        // -----------------------------------
        #region Вкладка Выходные
        private void AddHoliday_Click(object sender, RoutedEventArgs e) // Добавить неучебный день
        {
            // string Date = dateTimePickerHolyday.Value.ToString("dd'.'MM"); // Дата
            if (dpDayHoliday.SelectedDate != null)
            {
                DateTime DayH = (DateTime)dpDayHoliday.SelectedDate;
                //   DateTime DayH = new DateTime(dateTimePickerHolyday.Value.Year, dateTimePickerHolyday.Value.Month, dateTimePickerHolyday.Value.Day);
                Holiday NewH = new Holiday(DayH);

                bool YjeEst = false;
                for (int i = 0; i < Holydays.Count; i++)
                {
                    if (Holydays[i].Date == NewH.Date)
                    {
                        YjeEst = true;
                        lbHolidays.SelectedIndex = i;
                        System.Windows.Forms.MessageBox.Show("Этот выходной день уже есть в списке!", "LessonBell",
                            MessageBoxButtons.OK, MessageBoxIcon.Asterisk, MessageBoxDefaultButton.Button1);
                        break;
                    }
                }
                if (!YjeEst)
                {
                    Holydays.Add(NewH);
                    ShowHolidays();
                    Log.Write($"[Holidays] Добавлен выходной день: [{NewH.Date}]");
                }
                GetTimeBells("Добавлен выходной день");
            }
            else
            {
                System.Windows.Forms.MessageBox.Show("Вы не выбрали дату!");
            }
        }

        private void RemoveSelectedHoliday_Click(object sender, RoutedEventArgs e) // Удалить неучебный день
        {
            if (lbHolidays.SelectedIndex >= 0)
            {
                Holydays.RemoveAt(lbHolidays.SelectedIndex);
                Log.Write($"[Holidays] Удален выходной день: [{lbHolidays.SelectedItem}]");
                ShowHolidays(); // перепоказали уже без него
                GetTimeBells("Удален выходной день");
            }
        }

        private void ShowHolidays()
        {
            lbHolidays.Items.Clear();
            if (Holydays.Count != 0)
            {
                Holydays.Sort(new HolidayComparer());
                for (int i = 0; i < Holydays.Count; i++)
                {
                    lbHolidays.Items.Add(Holydays[i].Date.ToString("d MMMM", CultureInfo.CreateSpecificCulture("ru-RU")));
                }
            }
        }

        private bool NowHoliday() // Проверка выходной/каникулы ли сегодня
        {
            DateTime DateNow = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day);
            for (int i = 0; i < Holydays.Count; i++) // Проверяем что текущий день попадает в список выходных дней
            {
                if (Holydays[i].Date == DateNow) // .ToString("dd'.'MM")
                {
                    return true; // Сегодня выходной
                }
            }

            for (int i = 0; i < Kanukuls.Count; i++) // Проверяем что текущий день попадает в какой либо промежуток каникул
            {
                if (DateNow >= Kanukuls[i].DateStart && DateNow <= Kanukuls[i].DateEnd)
                {
                    return true; // Сегодня выходной
                }
            }
            return false; // Не выходной
        }

        private void AddKanikylu_Click(object sender, RoutedEventArgs e) // Добавить каникулы
        {
            if (dpStartKanikylu.SelectedDate != null && dpEndKanikylu.SelectedDate != null)
            {
                DateTime DateStartK = (DateTime)dpStartKanikylu.SelectedDate;
                DateTime DateEndK = (DateTime)dpEndKanikylu.SelectedDate;

                if (DateEndK < DateStartK || DateStartK == DateEndK)
                {
                    // неправильно задано
                    System.Windows.Forms.MessageBox.Show("Дата конца каникул не может быть раньше или равна дате начала!", "LessonBell",
                        MessageBoxButtons.OK, MessageBoxIcon.Asterisk, MessageBoxDefaultButton.Button1);
                }
                else
                {
                    Kanukyli NewK = new Kanukyli(DateStartK, DateEndK); // Объект каникулы с этими датами

                    bool YjeEst = false;
                    for (int i = 0; i < Kanukuls.Count; i++)
                    {
                        if (Kanukuls[i].DateStart == DateStartK && Kanukuls[i].DateEnd == DateEndK)
                        {
                            YjeEst = true;
                            lbKanikylu.SelectedIndex = i;
                            System.Windows.Forms.MessageBox.Show("Эти каникулы уже есть в списке!", "LessonBell",
                                MessageBoxButtons.OK, MessageBoxIcon.Asterisk, MessageBoxDefaultButton.Button1);
                            break;
                        }

                        if (DateStartK >= Kanukuls[i].DateStart && DateStartK <= Kanukuls[i].DateEnd || DateEndK >= Kanukuls[i].DateStart && DateEndK <= Kanukuls[i].DateEnd)
                        {
                            // Пересечение
                            YjeEst = true;
                            lbKanikylu.SelectedIndex = i;
                            System.Windows.Forms.MessageBox.Show("Эти каникулы пересекаются с другими каникулами!", "LessonBell",
                                MessageBoxButtons.OK, MessageBoxIcon.Asterisk, MessageBoxDefaultButton.Button1);
                            break;
                        }
                    }
                    if (!YjeEst) // если таких каникул нет
                    {
                        Kanukuls.Add(NewK);
                        ShowKanikuls();
                        Log.Write($"[Каникулы] Добавлены каникулы: [{NewK.DateStart.ToString("dd'.'MM") + " — " + NewK.DateEnd.ToString("dd'.'MM")}]");
                    }
                }
                GetTimeBells("Добавлены каникулы");
            }
            else
            {
                System.Windows.Forms.MessageBox.Show("Вы не выбрали дату!");
            }
        }

        private void RemoveSelectedKanikylu_Click(object sender, RoutedEventArgs e) // Удалить каникулы
        {
            if (lbKanikylu.SelectedIndex >= 0)
            {
                Kanukuls.RemoveAt(lbKanikylu.SelectedIndex);
                Log.Write($"[Каникулы] Удалены каникулы: [{lbKanikylu.SelectedItem}]");
                ShowKanikuls();
                GetTimeBells("Удалены каникулы");
            }
        }

        private void ShowKanikuls() // Выводим каникулы в листБокс
        {
            lbKanikylu.Items.Clear();

            if (Kanukuls.Count != 0)
            {
                Kanukuls.Sort(new KanukyliComparer());

                for (int i = 0; i < Kanukuls.Count; i++)
                {
                    string normStart = Kanukuls[i].DateStart.ToString("d MMMM", CultureInfo.CreateSpecificCulture("ru-RU"));
                    string normEnd = Kanukuls[i].DateEnd.ToString("d MMMM", CultureInfo.CreateSpecificCulture("ru-RU"));

                    lbKanikylu.Items.Add(normStart + " — " + normEnd); // Заново заполнили
                }
            }
        }

        #endregion
        // -----------------------------------
        // -----------------------------------
        #region Трей
        protected override void OnStateChanged(EventArgs e) // Свернуть в трей
        {
            if (WindowState == System.Windows.WindowState.Minimized)
            {
                ShowInTaskbar = false;
            }
            else
            {
                this.ShowInTaskbar = true;
            }
            base.OnStateChanged(e);
        }

        private void SetNotifyIcon() // Установить меню и иконку
        {
            cms.Cursor = System.Windows.Forms.Cursors.Hand;
            cms.Items.Add("Открыть LessonBell");
            cms.Items[0].Image = Properties.Resources.bigBell;
            cms.Items[0].Click +=
                delegate (object sender, EventArgs e)
                {
                    WindowState = System.Windows.WindowState.Normal;
                };


            ToolStripSeparator stripSeparator1 = new ToolStripSeparator();
            stripSeparator1.Alignment = ToolStripItemAlignment.Right;//right alignment
            cms.Items.Add(stripSeparator1);

            cms.Items.Add("О программе");
            cms.Items[2].Click +=
                delegate (object sender, EventArgs e)
                {
                    // о программе
                };

            cms.Items.Add("Выход");
            cms.Items[3].Click +=
                delegate (object sender, EventArgs e)
                {
                    Close();
                };


            notifyIcon.Icon = Properties.Resources.bigICO;
            notifyIcon.Visible = true;
            notifyIcon.DoubleClick +=
                delegate (object sender, EventArgs e)
                {
                    WindowState = System.Windows.WindowState.Normal;
                };
            notifyIcon.ContextMenuStrip = cms;
            notifyIcon.MouseUp += notifyIcon1_MouseUp;
            notifyIcon.Text = "LessonBell\nАвтоматическая подача звонков и музыка на переменах";
        }

        private void notifyIcon1_MouseUp(object sender, System.Windows.Forms.MouseEventArgs e) // Показать меню по нажатию ПКМ
        {
            if (e.Button == MouseButtons.Left)
            {
                MethodInfo mi = typeof(NotifyIcon).GetMethod("ShowContextMenu", BindingFlags.Instance | BindingFlags.NonPublic);
                mi.Invoke(notifyIcon, null);
            }
        }

        #endregion
        // -----------------------------------
        // -----------------------------------
        #region Сделать неактивной кнопку "Закрыть"
        //[DllImport("user32.dll", EntryPoint = "GetSystemMenu")]
        //private static extern IntPtr GetSystemMenu(IntPtr hwnd, int revert);

        //[DllImport("user32.dll", EntryPoint = "GetMenuItemCount")]
        //private static extern int GetMenuItemCount(IntPtr hmenu);

        //[DllImport("user32.dll", EntryPoint = "RemoveMenu")]
        //private static extern int RemoveMenu(IntPtr hmenu, int npos, int wflags);

        //[DllImport("user32.dll", EntryPoint = "DrawMenuBar")]
        //private static extern int DrawMenuBar(IntPtr hwnd);

        //private const int MF_BYPOSITION = 0x0400;
        //private const int MF_DISABLED = 0x0002;

       

        private void MainWindow_SourceInitialized(object sender, EventArgs e)
        {
        //    WindowInteropHelper helper = new WindowInteropHelper(this);
        //    IntPtr windowHandle = helper.Handle; //Get the handle of this window
        //    IntPtr hmenu = GetSystemMenu(windowHandle, 0);
        //    int cnt = GetMenuItemCount(hmenu);
        //    //remove the button
        //    RemoveMenu(hmenu, cnt - 1, MF_DISABLED | MF_BYPOSITION);
        //    //remove the extra menu line
        //    RemoveMenu(hmenu, cnt - 2, MF_DISABLED | MF_BYPOSITION);
        //    DrawMenuBar(windowHandle); //Redraw the menu bar
        }
        #endregion
        // -----------------------------------
        // -----------------------------------
        #region управление громкостью звука
        private const int APPCOMMAND_VOLUME_MUTE = 0x80000;
        private const int APPCOMMAND_VOLUME_UP = 0xA0000;
        private const int APPCOMMAND_VOLUME_DOWN = 0x90000;
        private const int WM_APPCOMMAND = 0x319;

        [DllImport("user32.dll")]
        public static extern IntPtr SendMessageW(IntPtr hWnd, int Msg,
            IntPtr wParam, IntPtr lParam);



        private void MuteVolumeWindows()//Выключение-включение звука
        {
            IntPtr windowHandle = new WindowInteropHelper(this).Handle;
            SendMessageW(windowHandle, WM_APPCOMMAND, windowHandle,
                (IntPtr)APPCOMMAND_VOLUME_MUTE);
        }

        private void DownVolumeWindows()//Убавление громкости
        {
            IntPtr windowHandle = new WindowInteropHelper(this).Handle;
            SendMessageW(windowHandle, WM_APPCOMMAND, windowHandle,
                (IntPtr)APPCOMMAND_VOLUME_DOWN);
        }

        private void UpVolumeWindows()//Прибавление звука
        {
            IntPtr windowHandle = new WindowInteropHelper(this).Handle;
            SendMessageW(windowHandle, WM_APPCOMMAND, windowHandle,
                (IntPtr)APPCOMMAND_VOLUME_UP);
        }


        #endregion
        // -----------------------------------
        public static string FirstUpper(string str) // Сделать первый символ строки заглавным, остальные маленькими - [ПОРЯДОК 19 МАРТА]
        {
            return str.Substring(0, 1).ToUpper() + (str.Length > 1 ? str.Substring(1) : "");
        }

        #region ПОРЯДОК





        #endregion

        

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            
        }

        private void lbPlaylistMusic_MouseDoubleClick(object sender, MouseButtonEventArgs e) // Двойной клик по элементу
        {

        }
        // при выключении выключателя проигрыать доп.сигналы - плеер не остановится!
    }
}