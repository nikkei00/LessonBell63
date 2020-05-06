using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace LessonBell
{
    /// <summary>
    /// Логика взаимодействия для WindowLoading.xaml
    /// </summary>
    public partial class WindowLoading : Window
    {
        System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
        MainWindow mw;
        public WindowLoading()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            timer.Interval = 1000;
            timer.Tick += Timer_Tick;
            timer.Start();

        }

        private void Mw_onSettingsLoaded()
        {
            //mw.Show();
            Close(); // прячем эту форму
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            timer.Stop();
            timer.Dispose();
            //mw = new MainWindow(Mw_onSettingsLoaded);
            //mw.Visibility = Visibility.Collapsed;
            
            //mw.Show();
            //mw.Visibility = Visibility.Collapsed;
        }
    }
}
