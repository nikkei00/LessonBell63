using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LessonBell
{
    public class Logger
    {
        private string _file;
        public delegate void LoggerWritedEventHandler(string DateTime, string msg);
        public event LoggerWritedEventHandler onWrited; // событие при изменении 

        public Logger(string file)
        {
            _file = file;
        }

        public string File { get { return _file; } }

        public void Write(string msg)
        {
            if (onWrited != null)
            {
                onWrited(DateTime.Now.ToString(), msg);
            }
            try
            {
                using (System.IO.StreamWriter file = new System.IO.StreamWriter(_file, true))
                {
                    file.WriteLine($"[{DateTime.Now}] " + msg);
                    file.Close();
                    file.Dispose();
                }
            }
            catch
            {

            }
        }
    }
}
