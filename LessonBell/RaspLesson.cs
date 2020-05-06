using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace LessonBell
{
    [DataContract]
    public class RaspLesson
    {
        [DataMember]
        public int Number { get; set; } // Номер по списку
        [DataMember]
        public bool MuzActive { get; set; } // Активность музык

        [DataMember]
        public TimeSpan TimeStart { get; set; } // Время начала

        [DataMember]
        public TimeSpan TimeEnd { get; set; } // Время конца

        [DataMember]
        public string NameRasp { get; set; } // Название расписания

        public bool PozvonilStart { get; set; } // Звонил ли

        public bool PozvonilEnd { get; set; } // Звонил ли

        // Создание урока - задаются данные
        public RaspLesson(bool aMuzActive, TimeSpan aTimeStart, TimeSpan aTimeEnd, string aNameRasp)
        {
            MuzActive = aMuzActive;
            TimeStart = aTimeStart;
            TimeEnd = aTimeEnd;
            NameRasp = aNameRasp;
            PozvonilStart = PozvonilEnd = false;
            Number = 0;
        }
    }
}
