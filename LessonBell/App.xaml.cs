using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace LessonBell
{
    /// <summary>
    /// Логика взаимодействия для App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App() : base()
        {
            this.Dispatcher.UnhandledException += OnDispatcherUnhandledException;
        }

        void OnDispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            string errorMessage = string.Format("An unhandled exception occurred:\n\n {0}\n\n{1}", e.Exception.Message, e.Exception.ToString());
            MessageBox.Show(errorMessage, "LessonBell - CTIRICAL ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true;
        }
    }
}
