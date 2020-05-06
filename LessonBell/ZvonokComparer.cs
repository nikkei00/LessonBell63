using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LessonBell
{
    class ZvonokComparer : IComparer<Zvonok>
    {
        public int Compare(Zvonok x, Zvonok y)
        {
            return TimeSpan.Compare(x.Time, y.Time);
        }
    }
}
