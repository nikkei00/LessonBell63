using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LessonBell
{
    class Kanikuli
    {
        private DateTime _dateStart;
        private DateTime _dateEnd;

        public Kanikuli(DateTime DateStart, DateTime DateEnd)
        {
            _dateStart = DateStart;
            _dateEnd = DateEnd;
        }

        public DateTime DateStart { get { return _dateStart; } }
        public DateTime DateEnd { get { return _dateEnd; } }
    }
}
