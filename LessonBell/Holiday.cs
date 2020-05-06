using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace LessonBell
{
    [DataContract]
    class Holiday
    {
        [DataMember]
        public DateTime Date { get; set; }
        [DataMember]
        public int Number { get; set; }

        public Holiday(DateTime dDate, int nNumber = 0)
        {
            Date = dDate;
            Number = nNumber;
        }
    }
}
