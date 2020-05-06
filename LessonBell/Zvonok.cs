using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LessonBell
{
    class Zvonok
    {
        private TimeSpan _time;
        private string _kakoi;

        public Zvonok(TimeSpan Time, string Kakoi)
        {
            _time = Time;
            _kakoi = Kakoi;
        }

        public TimeSpan Time { get { return _time; } }
        public string Kakoi { get { return _kakoi; } }
    }
}
