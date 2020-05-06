using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LessonBell
{
    class MainSettings
    {
        Logger Log = new Logger("LogDejstvuj.txt");
        IniFile INI = new IniFile(AppDomain.CurrentDomain.BaseDirectory + "settings\\config.ini");

        public int VolumeWmpMusic;
        public int VolumeWmpDop;

        public bool MuzYmni;
        public bool MuzRandom;

        public bool PlayMuzNaPeremenax;
        public bool PlayDops;
        public bool ActiveOtherPlayer;
        public string OtherPlayer;

        public bool AutoRun;
        public bool RunMinimized;

        public string FolderMusic;
        public string MelodyStart;
        public string MelodyEnd;
        public int PoslednixPesen;
        public int ModePlayMusic;
        
        public bool YsilNoControl;
        public bool YsilAuto;
        public bool YsilHands;
        public bool YsilTime;
        public TimeSpan YsilTimeOn;
        public TimeSpan YsilTimeOff;
        public string ComPort;

        public List<Kanukyli> Kanikuls;
        public List<Holiday> Holidays;

        public MainSettings() // Настройки по умолчанию
        {
            VolumeWmpMusic = 50;
            VolumeWmpDop = 70;

            MuzYmni = true;
            MuzRandom = false;

            PlayMuzNaPeremenax = false;
            PlayDops = false;
            ActiveOtherPlayer = false;
            OtherPlayer = "Не выбран..";

            AutoRun = false;
            RunMinimized = false;

            FolderMusic = "Не выбрана..";
            MelodyStart = AppDomain.CurrentDomain.BaseDirectory + @"SelectedMusic\Dzz_Niz_10sek.mp3";
            MelodyEnd = AppDomain.CurrentDomain.BaseDirectory + @"SelectedMusic\Dzz_Niz_7sek.mp3";
            PoslednixPesen = 9;
            ModePlayMusic = 0;

            YsilNoControl = false;
            YsilAuto = true;
            YsilHands = false;
            YsilTime = false;
            YsilTimeOn = new TimeSpan(7, 59, 0);
            YsilTimeOff = new TimeSpan(17, 10, 0);
            ComPort = "COM";

            Kanikuls = new List<Kanukyli>();
            Holidays = new List<Holiday>();
        }

        public void Save() // Сохранить - запись в config.ini [MAIN]
        {
            INI.DeleteSection("Main");

            INI.WriteINI("Main", "VolumeMusic", VolumeWmpMusic.ToString());
            INI.WriteINI("Main", "VolumeDop", VolumeWmpDop.ToString());

            INI.WriteINI("Main", "MuzYmni", MuzYmni.ToString());
            INI.WriteINI("Main", "MuzRandom", MuzRandom.ToString());
            INI.WriteINI("Main", "PlayMuzNaPeremenax", PlayMuzNaPeremenax.ToString());
            INI.WriteINI("Main", "PlayDops", PlayDops.ToString());

            INI.WriteINI("Main", "AutoRun", AutoRun.ToString());
            INI.WriteINI("Main", "RunMinimized", RunMinimized.ToString());
            INI.WriteINI("Main", "MelodyStart", MelodyStart);
            INI.WriteINI("Main", "MelodyEnd", MelodyEnd);
            INI.WriteINI("Main", "FolderMusic", FolderMusic);

            INI.WriteINI("Main", "ActiveOtherPlayer", ActiveOtherPlayer.ToString());
            INI.WriteINI("Main", "OtherPlayer", OtherPlayer);
            INI.WriteINI("Main", "PoslednixPesen", PoslednixPesen.ToString());
            INI.WriteINI("Main", "ModePlayMusic", ModePlayMusic.ToString());

            INI.WriteINI("Main", "YsilNoControl", YsilNoControl.ToString());
            INI.WriteINI("Main", "YsilAuto", YsilAuto.ToString());
            INI.WriteINI("Main", "YsilHands", YsilHands.ToString());
            INI.WriteINI("Main", "YsilTime", YsilTime.ToString());
            INI.WriteINI("Main", "YsilTimeOn", YsilTimeOn.ToString());
            INI.WriteINI("Main", "YsilTimeOff", YsilTimeOff.ToString());
            INI.WriteINI("Main", "ComPort", ComPort);

            for (int i = 0; i < Kanikuls.Count; i++)
            {
                string dateS = Kanikuls[i].DateStart.Day.ToString("00") + "." + Kanikuls[i].DateStart.Month.ToString("00") + "." + DateTime.Now.Year.ToString("00");
                string dateE = Kanikuls[i].DateEnd.Day.ToString("00") + "." + Kanikuls[i].DateEnd.Month.ToString("00") + "." + DateTime.Now.Year.ToString("00");
                INI.WriteINI("Main", "KanukulStart" + i, dateS);
                INI.WriteINI("Main", "KanukulEnd" + i, dateE);
            }

            for (int i = 0; i < Holidays.Count; i++)
            {
                string dateH = Holidays[i].Date.Day.ToString("00") + "." + Holidays[i].Date.Month.ToString("00") + "." + DateTime.Now.Year.ToString("00");
                INI.WriteINI("Main", "Holiday" + i, dateH);
            }
        }

        /// <summary>
        /// Чтение значения
        /// </summary>
        /// <typeparam name="T">тип читаемого значений</typeparam>
        /// <param name="MapValue">Функция преобразования</param>
        /// <param name="defaultValue">Значение по умолчанию</param>
        /// <returns>Считаное значение или значение по умолчанию, если не удалось прочитать</returns>
        T ReadValue<T>(string Section, string Key, Func<string, T> MapValue, T defaultValue)
        {
            try
            {
                // Если ключ есть
                if (INI.KeyExistsINI(Key, Section))
                {
                    var value = INI.ReadINI(Section, Key);
                    return MapValue(value);
                }
                else
                {
                    throw new Exception();
                }
            }
            catch
            {
                Log.Write("[MainSettings.Load] Ошибка при считывании [" + Key + "] применена стандартная настройка: " + defaultValue.ToString());
                return defaultValue;
            }
        }

        public void Load() // Загрузить - считать из config.ini [MAIN]
        {
            var defaultSettings = new MainSettings();

            VolumeWmpMusic = ReadValue("Main", "VolumeMusic", s => Convert.ToInt32(s), defaultSettings.VolumeWmpMusic);
            VolumeWmpDop = ReadValue("Main", "VolumeDop", s => Convert.ToInt32(s), defaultSettings.VolumeWmpDop);

            MuzYmni = ReadValue("Main", "MuzYmni", s => Convert.ToBoolean(s), defaultSettings.MuzYmni);
            MuzRandom = ReadValue("Main", "MuzRandom", s => Convert.ToBoolean(s), defaultSettings.MuzRandom);
            PlayMuzNaPeremenax = ReadValue("Main", "PlayMuzNaPeremenax", s => Convert.ToBoolean(s), defaultSettings.PlayMuzNaPeremenax);
            PlayDops = ReadValue("Main", "PlayDops", s => Convert.ToBoolean(s), defaultSettings.PlayDops);

            AutoRun = ReadValue("Main", "AutoRun", s => Convert.ToBoolean(s), defaultSettings.AutoRun);
            RunMinimized = ReadValue("Main", "RunMinimized", s => Convert.ToBoolean(s), defaultSettings.RunMinimized);

            MelodyStart = ReadValue("Main", "MelodyStart", s => s, defaultSettings.MelodyStart);
            MelodyEnd = ReadValue("Main", "MelodyEnd", s => s, defaultSettings.MelodyEnd);
            FolderMusic = ReadValue("Main", "FolderMusic", s => s, defaultSettings.FolderMusic);

            ActiveOtherPlayer = ReadValue("Main", "ActiveOtherPlayer", s => Convert.ToBoolean(s), defaultSettings.ActiveOtherPlayer);
            OtherPlayer = ReadValue("Main", "OtherPlayer", s => s, defaultSettings.OtherPlayer);
            PoslednixPesen = ReadValue("Main", "PoslednixPesen", s => Convert.ToInt32(s), defaultSettings.PoslednixPesen);
            ModePlayMusic = ReadValue("Main", "ModePlayMusic", s => Convert.ToInt32(s), defaultSettings.ModePlayMusic);

            YsilNoControl = ReadValue("Main", "YsilNoControl", s => Convert.ToBoolean(s), defaultSettings.YsilNoControl);
            YsilAuto = ReadValue("Main", "YsilAuto", s => Convert.ToBoolean(s), defaultSettings.YsilAuto);
            YsilHands = ReadValue("Main", "YsilHands", s => Convert.ToBoolean(s), defaultSettings.YsilHands);
            YsilTime = ReadValue("Main", "YsilTime", s => Convert.ToBoolean(s), defaultSettings.YsilTime);
            YsilTimeOn = ReadValue("Main", "YsilTimeOn", s => TimeSpan.Parse(s), defaultSettings.YsilTimeOn);
            YsilTimeOff = ReadValue("Main", "YsilTimeOff", s => TimeSpan.Parse(s), defaultSettings.YsilTimeOff);

            ComPort = ReadValue("Main", "ComPort", s => s, defaultSettings.ComPort);

            DateTime emptyDate = new DateTime();

            for (int i = 0; INI.KeyExistsINI("KanukulStart" + i, "Main"); i++)
            {
                var newKanuk = new Kanukyli(ReadValue("Main", "KanukulStart" + i, s => Convert.ToDateTime(s), emptyDate),
                                            ReadValue("Main", "KanukulEnd" + i, s => Convert.ToDateTime(s), emptyDate));

                // если обе даты не стандартные т.е. считались норм
                if (newKanuk.DateStart != emptyDate && newKanuk.DateEnd != emptyDate)
                {
                    Kanikuls.Add(newKanuk);
                }
            }

            for (int i = 0; INI.KeyExistsINI("Holiday" + i, "Main"); i++)
            {
                var newHolid = new Holiday(ReadValue("Main", "Holiday" + i, s => Convert.ToDateTime(s), emptyDate));

                // если дата не стандартная т.е. считалась норм
                if (newHolid.Date != emptyDate)
                {
                    Holidays.Add(newHolid);
                }
            }
        }
    }
}
