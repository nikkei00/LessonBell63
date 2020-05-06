using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LessonBell
{
    public class MuzDoPar
    {
        public TimeSpan Time { get; set; }
        public bool Pozvonil { get; set; }

        public MuzDoPar(TimeSpan aTime, bool aPozvonil)
        {
            Time = aTime;
            Pozvonil = aPozvonil;
        }
    }
}
