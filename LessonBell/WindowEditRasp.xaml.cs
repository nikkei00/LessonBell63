using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Threading;
using System.Windows;
using System.Windows.Forms;

namespace LessonBell
{
    /// <summary>
    /// Логика взаимодействия для WindowEditRasp.xaml
    /// </summary>
    public partial class WindowEditRasp : Window
    {
        Logger Logg;
        LessonsViewModel vmLessons;
        DopsViewModel vmDops;
        MuzDoPar mdp;
        RaspZvonkov RaspZ;
        bool GoodSave = false;

        public WindowEditRasp(MainWindow f, RaspZvonkov Rasp, string LogFile)
        {
            InitializeComponent();
            Logg = new Logger(LogFile);
            newNewLoad(Rasp);
            
        }

        private void newNewSave(string fileName, bool ToFile) // Загрузка настроек
        {
            try
            {
                byte err = 0;
                string error = "";

                if (mdp.Error != string.Empty) // Ошибка в музыке до занятий
                {
                    err++;
                    error += "\nНевозможно считать время МУЗЫКИ ДО ЗАНЯТИЙ!";
                }

                for (int i = 0; i < vmLessons.AllLessons.Count; i++) // Ищем ошибки в ЗВОНКАХ
                {
                    if (vmLessons.AllLessons[i].Error != string.Empty)
                    {
                        error += $"\nУрок {i + 1}: " + vmLessons.AllLessons[i].Error;
                        err++;
                    }

                    
                    if (err == 0) // если ошибок выше нет - все данные введены правильно, таймспан считает
                    {
                        // Если время начала урока >= времени окончания - некорректный ввод
                        if (TimeSpan.Parse(vmLessons.AllLessons[i].TimeS) >= TimeSpan.Parse(vmLessons.AllLessons[i].TimeDo))
                        {
                            error += $"\nУрок {i + 1}: Время начала урока не может быть больше или равно времени конца урока!";
                            err++;
                        }

                        if (i != vmLessons.AllLessons.Count - 1 && TimeSpan.Parse(vmLessons.AllLessons[i].TimeDo) > TimeSpan.Parse(vmLessons.AllLessons[i + 1].TimeS))
                        {
                            error += $"\nУрок {i} и {i + 1}: Время начала следующего урока не может быть меньше времени конца предыдущего урока!";
                            err++;
                        }
                    }
                }

                if (err == 0)
                {
                    if ((bool)cbxMuzBeforeLessons.IsChecked) // Если включена музка перед занятиями
                    {
                        if (TimeSpan.Parse(tbxMuzBeforeLessonsTime.Text) >= TimeSpan.Parse(vmLessons.AllLessons[0].TimeS))
                        {
                            error += $"\nВремя начала воспроизведения музыки перед занятиями не может быть больше или равно времени начала первого урока!";
                            err++;
                        }
                    }
                }

                for (int i = 0; i < vmDops.AllDops.Count; i++) // Ищем ошибки в ДОП.СИГНАЛАХ
                {
                    if (vmDops.AllDops[i].Error != string.Empty)
                    {
                        error += $"\nДоп.сигнал {i + 1}: " + vmDops.AllDops[i].Error;
                        err++;
                    }
                }
                
                if (err == 0) // Если ошибок нет
                {
                    RaspZ.NameRasp = tbxNameRasp.Text;
                    RaspZ.ZvonDniNedeli = (bool)rbDniNeledi.IsChecked;
                    RaspZ.ZvonDate = (bool)rbDate.IsChecked;
                    RaspZ.PN = (bool)cbxPN.IsChecked;
                    RaspZ.VT = (bool)cbxVT.IsChecked;
                    RaspZ.SR = (bool)cbxSR.IsChecked;
                    RaspZ.CT = (bool)cbxCT.IsChecked;
                    RaspZ.PT = (bool)cbxPT.IsChecked;
                    RaspZ.SB = (bool)cbxSB.IsChecked;
                    RaspZ.VS = (bool)cbxVS.IsChecked;
                    RaspZ.Date = (DateTime)dpDate.SelectedDate;

                    RaspZ.Uroks.Clear();
                    RaspZ.Uroks.Add(new RaspLesson((bool)cbxMuzBeforeLessons.IsChecked, TimeSpan.Parse(tbxMuzBeforeLessonsTime.Text), new TimeSpan(0, 1, 0), RaspZ.NameRasp));
                    
                    for (int i = 0; i < vmLessons.AllLessons.Count; i++) // сохраняем уроки
                    {
                        RaspZ.Uroks.Add(new RaspLesson(vmLessons.AllLessons[i].MuzActive, TimeSpan.Parse(vmLessons.AllLessons[i].TimeS), TimeSpan.Parse(vmLessons.AllLessons[i].TimeDo), RaspZ.NameRasp));
                    }

                    RaspZ.Dops.Clear();
                    RaspZ.Dops.Add(new DopSignal(true, new TimeSpan(1, 1, 0), "0 element", RaspZ.NameRasp));
                    for (int i = 0; i < vmDops.AllDops.Count; i++)
                    {
                        RaspZ.Dops.Add(new DopSignal(true, TimeSpan.Parse(vmDops.AllDops[i].Time), vmDops.AllDops[i].Signal, RaspZ.NameRasp));
                    }

                    if (ToFile) // сохраняем в отдельный файл
                    {
                        DataContractJsonSerializer jsonSerializerRasps = new DataContractJsonSerializer(typeof(RaspZvonkov));
                        using (FileStream fs = new FileStream(fileName, FileMode.Create))
                        {
                            jsonSerializerRasps.WriteObject(fs, RaspZ);
                        }
                    }
                    else
                    {
                        // Сохраняем расписание в обхект в главной форме
                        MainWindow mainForm = Owner as MainWindow;
                        mainForm.RaspVr = RaspZ;
                    }
                    GoodSave = true;
                }
                else
                {
                    GoodSave = false;
                    System.Windows.Forms.MessageBox.Show("Имеются ошибки в настройках расписания!\nСохранение невозможно!\n" + error, "LessonBell", MessageBoxButtons.OK,
                        MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
                }
            }
            catch (Exception e)
            {
                GoodSave = false;
                Logg.Write(e.Message);
                Logg.Write(e.ToString());
                System.Windows.Forms.MessageBox.Show($"(catch) Ошибка при сохранении настроек расписания звонков!\n\n{e.Message}\n\n\n{e.ToString()}",
                    "LessonBell", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
            }
        }

        public void newNewLoad(RaspZvonkov Rz) // Сохранение настроек
        {
            RaspZ = Rz;
            
            Rz = null;
            try
            {
                vmLessons = new LessonsViewModel(RaspZ);
                listViewUroks.DataContext = vmLessons;

                vmDops = new DopsViewModel(RaspZ);
                listViewDops.DataContext = vmDops;

                mdp = new MuzDoPar() { Active = RaspZ.Uroks[0].MuzActive, Time = RaspZ.Uroks[0].TimeStart.ToString("hh':'mm") };
                cbxMuzBeforeLessons.DataContext = tbxMuzBeforeLessonsTime.DataContext = mdp;

                tbxNameRasp.Text = RaspZ.NameRasp;

                rbDate.IsChecked = RaspZ.ZvonDate;
                dpDate.SelectedDate = RaspZ.Date;

                rbDniNeledi.IsChecked = RaspZ.ZvonDniNedeli;
                cbxPN.IsChecked = RaspZ.PN;
                cbxVT.IsChecked = RaspZ.VT;
                cbxSR.IsChecked = RaspZ.SR;
                cbxCT.IsChecked = RaspZ.CT;
                cbxPT.IsChecked = RaspZ.PT;
                cbxSB.IsChecked = RaspZ.SB;
                cbxVS.IsChecked = RaspZ.VS;
            }
            catch (Exception e)
            {
                Logg.Write(e.Message);
                Logg.Write(e.ToString());
                System.Windows.Forms.MessageBox.Show($"Ошибка при загрузке настроек расписания звонков!\n\n{e.Message}\n\n\n{e.ToString()}", "TSPK LessonBell", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
            }
        }
        

        private void rbDniNeledi_Checked(object sender, RoutedEventArgs e) // Включено по дням недели --    [ ПОРЯДОК 06 АПРЕЛЯ MVVM ]
        {
            cbxPN.IsEnabled = cbxVT.IsEnabled = cbxSR.IsEnabled = cbxCT.IsEnabled = cbxPT.IsEnabled = cbxSB.IsEnabled = cbxVS.IsEnabled = (bool)rbDniNeledi.IsChecked;
        }

        private void rbDate_Checked(object sender, RoutedEventArgs e) // Включено по дате --                [ ПОРЯДОК 06 АПРЕЛЯ MVVM ]
        {
            dpDate.IsEnabled = (bool)rbDate.IsChecked;
        }

        private void cbxMuzBeforeLessons_Checked(object sender, RoutedEventArgs e) // Включена музыка до занятий -- [ПОРЯДОК 22 МАРТА]
        {
            RaspZ.Uroks[0].MuzActive = tbxMuzBeforeLessonsTime.IsEnabled = (bool)cbxMuzBeforeLessons.IsChecked;
        }

        private void btnAddNewLesson_Click(object sender, RoutedEventArgs e) // Добавить урок --              [ПОРЯДОК 04 АПРЕЛЯ MVVM]
        {
            vmLessons.AllLessons.Add(new Lesson() { Number = vmLessons.AllLessons.Count + 1, TimeS = "08:00", TimeDo = "09:30", MuzActive = true });
        }

        private void btnDelSelectedLesson_Click(object sender, RoutedEventArgs e) // Удалить выбранный урок -- [ПОРЯДОК 04 АПРЕЛЯ MVVM]
        {
            if (listViewUroks.SelectedItems.Count > 0) // Если чота выделено
            {
                while (listViewUroks.SelectedItems.Count > 0) // Пока есть выделенные элементы
                {
                    vmLessons.AllLessons.Remove((Lesson)listViewUroks.SelectedItems[0]); // Удаляем все выделенные
                }
                for (int i = 0; i < vmLessons.AllLessons.Count; i++) // установили правильные номера урокам
                {
                    vmLessons.AllLessons[i].Number = i + 1; // Задаем номер урока
                }
            }
        }
        
        

        #region доп сигналы

        private void btnDopEditSignal_Click(object sender, RoutedEventArgs e) // изменить сигнал допа -- [ ПОРЯДОК 06 АПРЕЛЯ MVVM ]
        {
            int NumberDop = (int)(sender as System.Windows.Controls.Button).Tag - 1; // в каком допе меняем

            OpenFileDialog openFileDialog1 = new OpenFileDialog();

            openFileDialog1.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            openFileDialog1.Title = "Выберите мелодию ДОП.СИГНАЛА";
            openFileDialog1.Filter = "All files (*.*)|*.*|Аудио файлы (*.mp3, *.wav)|*.mp3;*.wav";
            openFileDialog1.InitialDirectory = Directory.GetCurrentDirectory() + @"\SelectedDops";
            openFileDialog1.FilterIndex = 2;
            openFileDialog1.Multiselect = false;

            if (!Directory.Exists(AppDomain.CurrentDomain.BaseDirectory + "SelectedDops"))
            {
                Directory.CreateDirectory(AppDomain.CurrentDomain.BaseDirectory + "SelectedDops");
            }

            if (openFileDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK) // Если файл выбран
            {
                string ReservMelody = vmDops.AllDops[NumberDop].Signal;
                //string ReservMelody = RaspZ.Dops[NumberDop].Signal;
                try
                {
                    if (openFileDialog1.FileName != System.IO.Path.GetFullPath(vmDops.AllDops[NumberDop].Signal)) // если полный путь до нового файла равен пути старого файла
                    {
                        string Melody = AppDomain.CurrentDomain.BaseDirectory + @"SelectedDops\" + openFileDialog1.SafeFileName;
                        if (!File.Exists(Melody)) // если этого файла еще нет в папке
                        {
                            File.Copy(openFileDialog1.FileName, Melody, true); // скопировать с заменой
                        }
                        Thread.Sleep(50);

                        if (File.Exists(Melody)) // если файл скопировался
                        {
                            new Thread(() => System.Windows.Forms.MessageBox.Show("Выбранная Вами мелодия дополнительного сигнала была скопирована в директорию программы.\n\nВыбрано: " + openFileDialog1.SafeFileName,
                                "LessonBell — Сигнал выбран", MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1, System.Windows.Forms.MessageBoxOptions.ServiceNotification)).Start();
                            
                            vmDops.AllDops[NumberDop].Signal = Melody;
                            vmDops.AllDops[NumberDop].SignalShow = Path.GetFileName(Melody);
                            Logg.Write($"Изменена мелодия доп.сигнала [{NumberDop}] на [{System.IO.Path.GetFileName(vmDops.AllDops[NumberDop].Signal)}]");
                        }
                        else // Файл не скопировался
                        {
                            new Thread(() => System.Windows.Forms.MessageBox.Show("Файл звонка не скопировался в директорию программы!\nВозможно у программы нет доступа или файл занят другой программой.\nИзменения отменены.",
                                "LessonBell", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, System.Windows.Forms.MessageBoxOptions.ServiceNotification)).Start();
                        }
                    }
                    else
                    {
                        new Thread(() => System.Windows.Forms.MessageBox.Show("Вы выбрали уже выбранную мелодию!\nИзменения отменены.",
                            "LessonBell", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1,
                            System.Windows.Forms.MessageBoxOptions.ServiceNotification)).Start();
                    }
                }
                catch (Exception w)
                {
                    RaspZ.Dops[NumberDop].Signal = ReservMelody;
                    Logg.Write($"Ошибка при копировании мелодии звонка [{w.Message}]");
                    Logg.Write(w.ToString());
                    new Thread(() => System.Windows.Forms.MessageBox.Show($"Ошибка при копировании мелодии звонка\n\n {w.Message}\n\n\n{w.ToString()}",
                        "LessonBell - Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, System.Windows.Forms.MessageBoxOptions.ServiceNotification)).Start();
                }
            }
        }

        private void btnAddDop_Click(object sender, RoutedEventArgs e) // Добавить доп.сигнал --         [ ПОРЯДОК 06 АПРЕЛЯ MVVM ]
        {
            vmDops.AllDops.Add(new Dop() { Number = vmDops.AllDops.Count + 1, Time = "11:00", Signal = "Не выбран..", SignalShow = "Не выбран.." });
        }

        private void btnDelSelectedDop_Click(object sender, RoutedEventArgs e) // Удалить доп.сигнал -- [ ПОРЯДОК 06 АПРЕЛЯ MVVM ]
        {
            while (listViewDops.SelectedItems.Count > 0) // Пока есть выделенные элементы
            {
                vmDops.AllDops.Remove((Dop)listViewDops.SelectedItems[0]); // Удаляем все выделенные
            }
        }
        #endregion

        private void btnSave_Click(object sender, RoutedEventArgs e) // Сохранить --                [ ПОРЯДОК 06 АПРЕЛЯ MVVM ]
        {
            newNewSave(null, false); // Сохранили в главной форме
            
            if (GoodSave)
            {
                DialogResult = true;
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e) // Отменить - закрыть окно -- [ ПОРЯДОК 06 АПРЕЛЯ MVVM ]
        {
            DialogResult = false;
        }
        


        private void btnSaveToFile_Click(object sender, RoutedEventArgs e) // Сохранить настройки в файл -- [ПОРЯДОК 31 МАРТА 2018]
        {
            try
            {
                SaveFileDialog sFD = new SaveFileDialog();
                sFD.Title = "Сохранение настроек расписания [" + tbxNameRasp.Text + "] в файл";
                sFD.Filter = "Все файлы (*.*)|*.*|Расписание звонков (*.lbRasp)|*.lbRasp";
                sFD.FilterIndex = 2;
                sFD.FileName = tbxNameRasp.Text + ".lbRasp";
                
                if (sFD.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    newNewSave(sFD.FileName, true);   
                    if (!File.Exists(sFD.FileName)) // если файла нет
                    {
                        System.Windows.Forms.MessageBox.Show($"Ошибка при сохранении настроек расписания звонков в файл!\nФайл не был сохранен.\nВозможно у программы нет доступа",
                            "LessonBell", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
                    }
                }
            }
            catch (Exception k)
            {
                Logg.Write(k.Message);
                Logg.Write(k.ToString());
                System.Windows.Forms.MessageBox.Show($"Ошибка при сохранении настроек расписания звонков!\n\n{k.Message}\n\n\n{k.ToString()}",
                    "LessonBell", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
            }
        }

        private void btnLoadOnFile_Click(object sender, RoutedEventArgs e) // Загрузить настройки из файла -- [ПОРЯДОК 31 МАРТА 2018]
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();

            openFileDialog1.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            openFileDialog1.Title = "Выберите файл с расписанием звонков";
            openFileDialog1.Filter = "Все файлы (*.*)|*.*|Расписание звонков (*.lbRasp)|*.lbRasp";
            openFileDialog1.FilterIndex = 2;
            openFileDialog1.Multiselect = false;

            // Загрузить из файла
            if (openFileDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                RaspZvonkov RaspForLoad = new RaspZvonkov();

                DataContractJsonSerializer jsonSerializerRasps = new DataContractJsonSerializer(typeof(RaspZvonkov));

                try // пытаемся считать
                {
                    using (FileStream fs = new FileStream(openFileDialog1.FileName, FileMode.Open))
                    {
                        RaspForLoad = (RaspZvonkov)jsonSerializerRasps.ReadObject(fs);
                    }
                    newNewLoad(RaspForLoad);
                }
                catch (Exception f)
                {
                    System.Windows.Forms.MessageBox.Show("Ошибка при считывании расписания звонков\n\n" + f.Message + "\n\n------------------------------\n\n" + f.ToString());
                }
            }
        }

        #region mvvm
        public class Dop : BindableBase, IDataErrorInfo // -- [ПОРЯДОК 04 АПРЕЛЯ MVVM]
        {
            private int _number;
            private string _time;
            private string _signal;
            private string _signalShow;
            private string _error = string.Empty;
            
            public int Number
            {
                get { return _number; }
                set { SetProperty(ref _number, value); }
            }
            public string Time
            {
                get { return _time; }
                set
                {
                    SetProperty(ref _time, value);
                }
            }
            public string Signal
            {
                get { return _signal; }
                set
                {
                    SetProperty(ref _signal, value);
                }
            }

            public string SignalShow
            {
                get { return _signalShow; }
                set
                {
                    SetProperty(ref _signalShow, value);
                }
            }

            public string Error
            {
                get { return _error; }
            }

            public string this[string columnName]
            {
                get
                {
                    string error = String.Empty;
                    _error = string.Empty;

                    TimeSpan rezult;

                    switch (columnName)
                    {
                        case "Time":
                            if (!TimeSpan.TryParse(_time, out rezult))
                            {
                                _error = error = "Невозможно считать время звучания дополнительного сигнала";
                            }
                            break;
                    }
                    return error;
                }
            }
        }

        class DopsViewModel
        {
            ObservableCollection<Dop> lst_Dops = new ObservableCollection<Dop>();

            public DopsViewModel(RaspZvonkov RaspZv)
            {
                for (int i = 1; i < RaspZv.Dops.Count; i++)
                {
                    lst_Dops.Add(new Dop() { Number = i, Time = RaspZv.Dops[i].Time.ToString("hh':'mm"), Signal = RaspZv.Dops[i].Signal, SignalShow = System.IO.Path.GetFileName(RaspZv.Dops[i].Signal) });
                }
            }

            public ObservableCollection<Dop> AllDops
            {
                get { return lst_Dops; }
            }
        }


        public class MuzDoPar : BindableBase, IDataErrorInfo
        {
            private bool _active;
            private string _time;
            private string _error = string.Empty;

            public bool Active
            {
                get { return _active; }
                set { SetProperty(ref _active, value); }
            }
            public string Time
            {
                get { return _time; }
                set { SetProperty(ref _time, value); }
            }
            public string Error
            {
                get { return _error; }
            }

            public string this[string columnName]
            {
                get
                {
                    string error = String.Empty;
                    _error = string.Empty;

                    TimeSpan rezult;

                    switch (columnName)
                    {
                        case "Time":
                            if (!TimeSpan.TryParse(_time, out rezult))
                            {
                                _error = error = "Невозможно считать время МУЗЫКИ ПЕРЕД ЗАНЯТИЯМИ";
                            }
                            break;
                    }
                    return error;
                }
            }
        }


        public class Lesson : BindableBase, IDataErrorInfo // -- [ПОРЯДОК 04 АПРЕЛЯ MVVM]
        {
            private int _number;
            private string _timeS;
            private string _timeDo;
            private bool _muzActive;
            private string _error = string.Empty;

            public int Number
            {
                get { return _number; }
                set { SetProperty(ref _number, value); }
            }
            public string TimeS
            {
                get { return _timeS; }
                set
                {
                    SetProperty(ref _timeS, value);
                }
            }
            public string TimeDo
            {
                get { return _timeDo; }
                set
                {
                    SetProperty(ref _timeDo, value);
                }
            }
            public bool MuzActive
            {
                get { return _muzActive; }
                set { SetProperty(ref _muzActive, value); }
            }
            public string Error
            {
                get { return _error; }
            }

            public string this[string columnName]
            {
                get
                {
                    string error = String.Empty;
                    _error = string.Empty;

                    TimeSpan rezult;

                    switch (columnName)
                    {
                        case "TimeS":
                            if (!TimeSpan.TryParse(_timeS, out rezult))
                            {
                                _error = error = "Невозможно считать время НАЧАЛА урока";
                            }
                            break;

                        case "TimeDo":
                            if (!TimeSpan.TryParse(_timeDo, out rezult))
                            {
                                _error = error = "Невозможно считать время КОНЦА урока";
                            }
                            break;
                    }

                    return error;
                }
            }
        }
        
        class LessonsViewModel
        {
            ObservableCollection<Lesson> lst_Lessons = new ObservableCollection<Lesson>();

            public LessonsViewModel(RaspZvonkov RaspZv)
            {
                for (int i = 1; i < RaspZv.Uroks.Count; i++)
                {
                    lst_Lessons.Add(new Lesson() { Number = i, TimeS = RaspZv.Uroks[i].TimeStart.ToString("hh':'mm"), TimeDo = RaspZv.Uroks[i].TimeEnd.ToString("hh':'mm"), MuzActive = RaspZv.Uroks[i].MuzActive });
                }
            }

            public ObservableCollection<Lesson> AllLessons
            {
                get { return lst_Lessons; }
            }
        }
        #endregion

        private void tbxNameRasp_PreviewMouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            tbxNameRasp.SelectAll();
        }

        private void tbxMuzBeforeLessonsTime_PreviewMouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            tbxMuzBeforeLessonsTime.SelectAll();
        }

        private void ClickMskTbTime(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            (sender as Xceed.Wpf.Toolkit.MaskedTextBox).SelectAll();
        }
    }
}
