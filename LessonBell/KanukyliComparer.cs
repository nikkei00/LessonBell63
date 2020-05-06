using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LessonBell
{
    class KanukyliComparer : IComparer<Kanukyli>
    {
        public int Compare(Kanukyli x, Kanukyli y)
        {
            return DateTime.Compare(x.DateStart, y.DateStart);
        }
    }
}
