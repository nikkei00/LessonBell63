using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LessonBell
{
    class MuzDoParComparer : IComparer<MuzDoPar>
    {
        public int Compare(MuzDoPar x, MuzDoPar y)
        {
            return TimeSpan.Compare(x.Time, y.Time);
        }
    }
}
