using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LessonBell
{
    class Bell
    {
        private TimeSpan _time;
        private bool _napary;
        private bool _muzActive;
        private bool _last;
        private int _number;
        private bool _pozvonil;

        public Bell(TimeSpan Time, bool NaPary, bool MuzActive, bool Last, int Number, bool Pozvonil = false)
        {
            _time = Time;
            _napary = NaPary;
            _muzActive = MuzActive;
            _last = Last;
            _number = Number;
            _pozvonil = false;
        }

        public TimeSpan Time { get { return _time; } set { _time = value; } }
        public bool NaPary { get { return _napary; } set { _napary = value; } }
        public bool MuzActive { get { return _muzActive; } set { _muzActive = value; } }
        public bool Last { get { return _last; } set { _last = value; } }
        public int Number { get { return _number; } set { _number = value; } }
        public bool Pozvonil { get { return _pozvonil; } set { _pozvonil = value; } }
    }
}
