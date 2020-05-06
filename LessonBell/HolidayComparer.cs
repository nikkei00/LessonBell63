using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LessonBell
{
    class HolidayComparer: IComparer<Holiday>
    {
        public int Compare(Holiday x, Holiday y)
        {
            return DateTime.Compare(x.Date, y.Date);
        }
    }
}
