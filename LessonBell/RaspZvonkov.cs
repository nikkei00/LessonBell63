using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization.Json;
using System.Runtime.Serialization;

namespace LessonBell
{
    [DataContract]
    public class RaspZvonkov
    {
        [DataMember]
        public string NameRasp { get; set; } // Название расписания
        [DataMember]
        public int Number { get; set; } // номер в списке
        [DataMember]
        private bool NowLoad = false; // в данный момент загрузка настроек
        [DataMember]
        public bool _active; // активность расписания
        [DataMember]
        public byte _priority; // приоритет расписания

        public delegate void AoPChangedEventHandler(object sender, string msg);
        public event AoPChangedEventHandler AoPEdited; // событие при изменении активности/приортитета

        // Активность расписания, при изменении - вызывает событие
        
        public bool Active
        {
            get { return _active; }
            set
            {
                if (_active == value)
                    return;

                _active = value;
                if (!NowLoad)
                {
                    AoPEdited(this, "Изменена АКТИВНОСТЬ у");
                }
            }
        }
        
        public byte Priority
        {
            get { return _priority; }
            set
            {
                if (_priority == value)
                    return;

                _priority = value;
                if (!NowLoad)
                {
                    AoPEdited(this, "Изменен ПРИОРИТЕТ у");
                }
            }
        } // Активность расписания, при изменении - вызывает событие

        [DataMember]
        public bool ZvonDate, ZvonDniNedeli, PN, VT, SR, CT, PT, SB, VS; // Когда звонить, дни недели
        [DataMember]
        public DateTime Date; // Дата

        [DataMember]
        public List<RaspLesson> Uroks = new List<RaspLesson>(); // Список уроков
        [DataMember]
        public List<DopSignal> Dops = new List<DopSignal>(); // Список доп.сигналов


        public RaspZvonkov() // Расписание по умолчанию
        {
            NameRasp = "По умолчанию (без настроек)";
            Active = false;
            Priority = 0;

            ZvonDate = false;
            Date = DateTime.Now;
            ZvonDniNedeli = true;
            PN = true;
            VT = true;
            SR = true;
            CT = true;
            PT = true;
            SB = false;
            VS = false;

            Uroks.Clear();

            // 0 урок - музыка до пар
            Uroks.Add(new RaspLesson(true, new TimeSpan(7, 50, 0), new TimeSpan(7, 41, 0), NameRasp));

            // 1-7 пары по ТСПК
            Uroks.Add(new RaspLesson(true, new TimeSpan(8, 30, 0), new TimeSpan(10, 0, 0), NameRasp));
            Uroks.Add(new RaspLesson(true, new TimeSpan(10, 20, 0), new TimeSpan(11, 50, 0), NameRasp));
            Uroks.Add(new RaspLesson(true, new TimeSpan(12, 25, 0), new TimeSpan(13, 55, 0), NameRasp));
            Uroks.Add(new RaspLesson(true, new TimeSpan(14, 5, 0), new TimeSpan(15, 35, 0), NameRasp));

            Dops.Clear();
            Dops.Add(new DopSignal(false, new TimeSpan(0, 0, 0), "null", NameRasp)); // Заняли 0 
            //Dops.Add(new DopSignal(false, new TimeSpan(12, 0, 0), "net"));
            //Dops.Add(new DopSignal(false, new TimeSpan(15, 0, 0), "netyyyy"));
        }
        /*
        /// <summary>
        /// Чтение значения, если значение не считано - применить по умолчанию
        /// </summary>
        /// <typeparam name="T">тип читаемого значений</typeparam>
        /// <param name="MapValue">Функция преобразования</param>
        /// <param name="defaultValue">Значение по умолчанию</param>
        /// <returns>Считаное значение или значение по умолчанию, если не удалось прочитать</returns>
        T ReadValue<T>(string Section, string Key, Func<string, T> MapValue, T defaultValue, IniFile INI)
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
                //Log.Write("[RaspZvonkov.Load] Ошибка при считывании [" + Key + "] применена стандартная настройка: " + defaultValue.ToString());
                return defaultValue;
            }
        }

        public void LoadRasp(IniFile INI, string kakoe) // считываем расписание из файлат
        {
            NowLoad = true;
            try
            {
                RaspZvonkov defaultRasp = new RaspZvonkov();

                NameRasp = ReadValue(kakoe, "NameRasp", s => s, defaultRasp.NameRasp, INI);
                Active = ReadValue(kakoe, "Active", s => Convert.ToBoolean(s), defaultRasp.Active, INI);
                Priority = ReadValue(kakoe, "Priority", s => Convert.ToByte(s), defaultRasp.Priority, INI);

                ZvonDate = ReadValue(kakoe, "ZvonDate", s => Convert.ToBoolean(s), defaultRasp.ZvonDate, INI);
                Date = ReadValue(kakoe, "Date", s => Convert.ToDateTime(s), DateTime.Now, INI);

                ZvonDniNedeli = ReadValue(kakoe, "ZvonDniNedeli", s => Convert.ToBoolean(s), defaultRasp.ZvonDniNedeli, INI);
                PN = ReadValue(kakoe, "PN", s => Convert.ToBoolean(s), defaultRasp.PN, INI);
                VT = ReadValue(kakoe, "VT", s => Convert.ToBoolean(s), defaultRasp.VT, INI);
                SR = ReadValue(kakoe, "SR", s => Convert.ToBoolean(s), defaultRasp.SR, INI);
                CT = ReadValue(kakoe, "CT", s => Convert.ToBoolean(s), defaultRasp.CT, INI);
                PT = ReadValue(kakoe, "PT", s => Convert.ToBoolean(s), defaultRasp.PT, INI);
                SB = ReadValue(kakoe, "SB", s => Convert.ToBoolean(s), defaultRasp.SB, INI);
                VS = ReadValue(kakoe, "VS", s => Convert.ToBoolean(s), defaultRasp.VS, INI);

                Uroks.Clear();
                TimeSpan emptyTime = new TimeSpan();

                // 0 урок - музыка до занятий
                Lesson newUrokMuzD = new Lesson(
                         ReadValue(kakoe, "ActiveMuzDoPar", s => Convert.ToBoolean(s), true, INI),
                         ReadValue(kakoe, "TimeMuzDoPar", s => TimeSpan.Parse(s), emptyTime, INI),
                         new TimeSpan(),
                         NameRasp
                         );

                // если считалось нормально
                if (newUrokMuzD.TimeStart != emptyTime)
                {
                    Uroks.Add(newUrokMuzD);
                }

                for (int i = 1; INI.KeyExistsINI(i + "urokMuzActive", kakoe); i++) // пока есть урок
                {
                    Lesson newUrok = new Lesson(
                        ReadValue(kakoe, i + "urokMuzActive", s => Convert.ToBoolean(s), true, INI),
                        ReadValue(kakoe, i + "urokTimeStart", s => TimeSpan.Parse(s), emptyTime, INI),
                        ReadValue(kakoe, i + "urokTimeEnd", s => TimeSpan.Parse(s), emptyTime, INI),
                        NameRasp
                        );

                    // если считалось нормально
                    if (newUrok.TimeStart != emptyTime && newUrok.TimeEnd != emptyTime)
                    {
                        Uroks.Add(newUrok);
                    }
                    else
                    {
                        //Log.Write("Урок " + i + ": считан некорректно! TimeStart: " + newUrok.TimeStart + ", TimeEnd: " + newUrok.TimeEnd);
                    }
                }

                Dops.Clear();
                Dops.Add(new DopSignal(false, new TimeSpan(0, 0, 0), "null", NameRasp)); // Заняли 0 элемент

                for (int i = 1; INI.KeyExistsINI(i + "dopActive", kakoe); i++)
                {
                    DopSignal newDop = new DopSignal(
                        ReadValue(kakoe, i + "dopActive", s => Convert.ToBoolean(s), true, INI),
                        ReadValue(kakoe, i + "dopTime", s => TimeSpan.Parse(s), emptyTime, INI),
                        ReadValue(kakoe, i + "dopSignal", s => s, string.Empty, INI),
                        NameRasp
                        );

                    if (newDop.Time != emptyTime && newDop.Signal != string.Empty)
                    {
                        Dops.Add(newDop);
                    }
                }
                NowLoad = false;
            }
            catch (Exception r)
            {
                NowLoad = false;
            }
        }

        public void SaveRasp(IniFile INI, string kakoe) // Сохранение настроек расписания в файл
        {
            INI.DeleteSection(kakoe);
            INI.WriteINI(kakoe, "NameRasp", NameRasp);
            INI.WriteINI(kakoe, "Active", Active.ToString());
            INI.WriteINI(kakoe, "Priority", Priority.ToString());

            INI.WriteINI(kakoe, "ZvonDate", ZvonDate.ToString());
            INI.WriteINI(kakoe, "Date", Date.ToShortDateString());

            INI.WriteINI(kakoe, "ZvonDniNedeli", ZvonDniNedeli.ToString());
            INI.WriteINI(kakoe, "PN", PN.ToString());
            INI.WriteINI(kakoe, "VT", VT.ToString());
            INI.WriteINI(kakoe, "SR", SR.ToString());
            INI.WriteINI(kakoe, "CT", CT.ToString());
            INI.WriteINI(kakoe, "PT", PT.ToString());
            INI.WriteINI(kakoe, "SB", SB.ToString());
            INI.WriteINI(kakoe, "VS", VS.ToString());

            INI.WriteINI(kakoe, "ActiveMuzDoPar", Uroks[0].MuzActive.ToString());
            INI.WriteINI(kakoe, "TimeMuzDoPar", Uroks[0].TimeStart.ToString());
            
            for (int i = 1; i < Uroks.Count; i++)
            {
                INI.WriteINI(kakoe, i + "urokMuzActive", Uroks[i].MuzActive.ToString());
                INI.WriteINI(kakoe, i + "urokTimeStart", Uroks[i].TimeStart.ToString("hh':'mm"));
                INI.WriteINI(kakoe, i + "urokTimeEnd", Uroks[i].TimeEnd.ToString("hh':'mm"));
            }

            for (int i = 1; i < Dops.Count; i++)
            {
                INI.WriteINI(kakoe, i + "dopActive", Dops[i].Active.ToString());
                INI.WriteINI(kakoe, i + "dopTime", Dops[i].Time.ToString("hh':'mm"));
                INI.WriteINI(kakoe, i + "dopSignal", Dops[i].Signal);
            }
        }
        */
    }
}
