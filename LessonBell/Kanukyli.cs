using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace LessonBell
{
    [DataContract]
    class Kanukyli
    {
        [DataMember]
        public int Number { get; set; }
        [DataMember]
        public DateTime DateStart { get; set; }
        [DataMember]
        public DateTime DateEnd { get; set; }

        public Kanukyli(DateTime aDateStart, DateTime aDateEnd, int nNumber = 0)
        {
            DateStart = aDateStart;
            DateEnd = aDateEnd;
            Number = nNumber;
        }
    }
}
