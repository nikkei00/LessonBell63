using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace LessonBell
{
    /// <summary>
    /// Логика взаимодействия для WindowRasp.xaml
    /// </summary>
    public partial class WindowRasp: Window
    {
        public WindowRasp()
        {
            InitializeComponent();
            bool active = true;
            for (int i = 1; i < 10; i++)
            {
                active = !active;
                listViewUroks.Items.Add(new RaspsZvonkovItem { UrokActive = active, NumberUrok = i, TimeS = "07:00", TimeDo = "09:00" });
            }
        }

        public class RaspsZvonkovItem
        {
            public int NumberUrok { get; set; }
            public string TimeS { get; set; }
            public string TimeDo { get; set; }
            public bool UrokActive { get; set; }
        }
    }
}
