using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LessonBell
{
    class BellComparer : IComparer<Bell>
    {
        public int Compare(Bell x, Bell y)
        {
            return TimeSpan.Compare(x.Time, y.Time);
        }
    }
}
