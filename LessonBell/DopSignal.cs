using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization.Json;
using System.Runtime.Serialization;

namespace LessonBell
{
    [DataContract]
    public class DopSignal
    {
        [DataMember]
        public bool Active { get; set; } // Активность

        [DataMember]
        public TimeSpan Time { get; set; } // Время
        [DataMember]
        public string Signal { get; set; } // Файл
        [DataMember]
        public string MiniSignal { get; set; } // назв.файла
        [DataMember]
        public string NameRasp { get; set; } // Назв.расписания
        [DataMember]
        public bool Pozvonil { get; set; } // Звонил ли

        // Создание доп.сигнала - задаются настройки
        public DopSignal(bool aActive, TimeSpan aTime,
            string aSignal, string aNameRasp, bool aPozvonil = false)
        {
            Active = aActive;
            Time = aTime;
            Signal = aSignal;
            if (Signal == "Не выбран..")
            {
                MiniSignal = Signal;
            }
            else
            {
                MiniSignal = Path.GetFileName(aSignal);
            }
            NameRasp = aNameRasp;
            Pozvonil = aPozvonil;
        }
    }
}
