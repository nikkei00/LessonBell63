using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LessonBell
{
    class KanikuliComparer: IComparer<Kanikuli>
    {
        public int Compare(Kanikuli x, Kanikuli y)
        {
            return DateTime.Compare(x.DateStart, y.DateStart);
        }
    }
}
