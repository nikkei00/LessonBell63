using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LessonBell
{
    public class DopS
    {
        private bool _pozvonil;
        private TimeSpan _time;
        private string _signal;
        private string _rasp;

        public DopS(bool Pozvonil, TimeSpan Time, string Signal, string Rasp)
        {
            _pozvonil = Pozvonil;
            _time = Time;
            _signal = Signal;
            _rasp = Rasp;
        }

        public bool Pozvonil { get { return _pozvonil; } set { _pozvonil = value; } }
        public TimeSpan Time { get { return _time; } set { _time = value; } }
        public string Signal { get { return _signal; } set { _signal = value; } }
        public string Rasp { get { return _rasp; } }
    }
}
