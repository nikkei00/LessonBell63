using System.Windows;
using System.Windows.Media;

namespace LessonBell
{
    /// <summary>
    /// Логика взаимодействия для WindowExitQuestion.xaml
    /// </summary>
    public partial class WindowExitQuestion : Window
    {
        System.Windows.Forms.Timer timer1 = new System.Windows.Forms.Timer();
        bool red = true;
        Brush brushRed;
        Brush brushWhite;
        Brush brushYellow;


        public WindowExitQuestion()
        {
            InitializeComponent();

            brushRed = new SolidColorBrush(Color.FromRgb(255, 0, 0));
            brushWhite = new SolidColorBrush(Color.FromRgb(255, 255, 255));
            brushYellow = new SolidColorBrush(Color.FromRgb(255, 255, 0));

            timer1.Interval = 800;
            timer1.Tick += Timer1_Tick;
            timer1.Enabled = true;
        }

        private void Timer1_Tick(object sender, System.EventArgs e)
        {
            if (red) // если ща красный на желтом
            {
                red = false;
                Alarm.Background = brushRed;
                Alarm.Foreground = brushWhite;
            }
            else
            {
                red = true;
                Alarm.Background = brushYellow;
                Alarm.Foreground = brushRed;
            }
        }

        // Выйти
        private void btnExit_Click(object sender, RoutedEventArgs e)
        {
            timer1.Stop();
            DialogResult = true;
        }

        // Оставить
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            timer1.Stop();
            DialogResult = false;
        }
    }
}
