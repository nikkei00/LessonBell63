using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LessonBell
{
    class Music
    {
        private string _fullName;
        private TimeSpan _duration;
        private int _priority;

        public Music(string FullName, TimeSpan Duration, int Priority)
        {
            _fullName = FullName;
            _duration = Duration;
            _priority = Priority;
        }

        public string FullName { get { return _fullName; } }
        public TimeSpan Duration { get { return _duration; } }
        public int Priority { get { return _priority; } set { _priority = value; } }
    }
}
