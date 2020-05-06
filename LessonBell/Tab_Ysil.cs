using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using IWshRuntimeLibrary;
using System.Windows.Forms;
using System.Reflection;
using CSCore;
using CSCore.Streams.Effects;
using CSCore.SoundOut;
using CSCore.Codecs;
using System.Windows.Interop;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Json;
using System.Runtime.Serialization;

namespace LessonBell
{
    // Вкладка Усилитель
    public partial class MainWindow
    {
        bool StateYsil = false; // Состояние усилителя
        bool YsilNoControl = false;
        bool YsilAuto = false;
        bool YsilTime = false;
        bool YsilHands = false;
        bool comReady = false;

        TimeSpan YsilAutoTimeOn = TimeSpan.Zero; // Время автоматического включения усилителя
        TimeSpan YsilAutoTimeOff = TimeSpan.Zero; // Время автоматического выключения
        TimeSpan YsilFixTimeOn = new TimeSpan(0, 2, 0); // Фиксированное время включения усилителя
        TimeSpan YsilFixTimeOff = new TimeSpan(0, 2, 0); // Фиксированное время выключения
        string ReservedTimeOnYsil = "";
        string ReservedTimeOffYsil = "";
        string[] comPorts;
        string[] rezComPorts = null;
        SerialPort workComPort; // СОМ порт для управления усилителем
        string SelectedCOM = "";

        private void ControlYsil() // -- [ПОРЯДОК 19 МАРТА]
        {
            if (workComPort != null && comReady) // Если порт не пустой
            {
                if (YsilAuto && YsilAutoTimeOn != TimeSpan.Zero && YsilAutoTimeOff != TimeSpan.Zero) // Если вкл\выкл автоматически
                {
                    if (StateYsil == false && TimeNow > YsilAutoTimeOn && TimeNow < YsilAutoTimeOff) // Попадаем в рабочее время
                    {
                        OnYsil("Автоматический расчёт времени"); // Сразу включить
                    }

                    if (StateYsil == true && (TimeNow < YsilAutoTimeOn || TimeNow > YsilAutoTimeOff)) // Попадаем в нерабочее время
                    {
                        OffYsil("Автоматический расчёт времени"); // Сразу выключить
                    }
                }

                if (YsilTime) // Если вкл\выкл по заданному времени
                {
                    if (StateYsil == false && TimeNow > YsilFixTimeOn && TimeNow < YsilFixTimeOff) // Попадаем в рабочее время
                    {
                        OnYsil("По заданному времени"); // Сразу включить
                    }

                    if (StateYsil == true && (TimeNow < YsilFixTimeOn || TimeNow > YsilFixTimeOff)) // Попадаем в нерабочее время
                    {
                        OffYsil("По заданному времени"); // Сразу выключить
                    }
                }
            }
        }

        private bool EweDop() // Будут ли еще доп.сигналы -- [ПОРЯДОК 19 МАРТА]
        {
            for (byte i = 0; i < AllDops.Count; i++)
            {
                if (TimeNow < AllDops[i].Time) // Если сейчас < элементу доп
                {
                    return true; // еще будут доп.сигналы
                }
            }
            return false;
        }

        private void CalcTimeYsilOnOff()
        {
            if (AllBells.Count > 0)
            {
                // найти самый ранний звонок, самый ранний доп.сигнал, самую ранную музыку до уроков  и включить перед любым
                TimeSpan PosledniyZvonok = AllBells[AllBells.Count - 1].TimeEnd;
                YsilAutoTimeOn = AllBells[0].TimeStart; // Самый ранний звонок
                YsilAutoTimeOff = PosledniyZvonok; // Самый поздний звонок

                if (MusicDoPar.Count > 0 && YsilAutoTimeOn > MusicDoPar[0].Time) // Если муз.до уроков есть и ее время раньше
                {
                    YsilAutoTimeOn = MusicDoPar[0].Time; // Самая ранняя музыка до уроков
                }

                if (AllDops.Count > 0) // Если доп.сигналы есть
                {
                    for (int i = 0; i < AllDops.Count; i++) // перебираем
                    {
                        TimeSpan TimeEndPlayDop = TimeSpan.FromSeconds(0);
                        try
                        {
                            var source = CodecFactory.Instance.GetCodec(AllDops[i].Signal); // Считали информацию о допе
                            TimeEndPlayDop = AllDops[i].Time + source.GetLength(); // Время окончания звучания допа
                        }
                        catch (Exception r)
                        {
                            ShowErrorMB("При расчете времени автоматического включения и выключения усилителя произошла ошибка при считывании длительности звучания доп.сигнала!", r.Message, r.ToString());
                        }
                        if (YsilAutoTimeOn > AllDops[i].Time) // Если время включения позже доп.сигнала
                        {
                            YsilAutoTimeOn = AllDops[i].Time; // Самый ранний доп.сигнал
                        }

                        if (YsilAutoTimeOff < TimeEndPlayDop) // Если время выключения раньше времени окончания звучания
                        {
                            YsilAutoTimeOff = new TimeSpan(TimeEndPlayDop.Hours, TimeEndPlayDop.Minutes, TimeEndPlayDop.Seconds); // Делоем его
                        }
                    }
                }
                if (YsilAutoTimeOff == PosledniyZvonok) // если время выключения осталось = последнему звонку
                {
                    YsilAutoTimeOff += new TimeSpan(DurationEndBell.Hours, DurationEndBell.Minutes, DurationEndBell.Seconds + 1); // Добавили время звучания звонка с пары

                    if (ActiveMuzNaPeremenax && AllBells[AllBells.Count - 1].MuzActive) // Если музыка будет
                    {
                        YsilAutoTimeOff += MinPlayMusicAfterLastBell; // Добавляем время звучания музыки после уроков

                        if (!ActiveOtherPlayer) // Если играем по внутреннему плееру
                        {
                            double volume = axWmpMusic.settings.volume;
                            double TimeWait = Math.Ceiling((volume * 0.09)); // 2,160 = 3

                            YsilAutoTimeOff += TimeSpan.FromSeconds(TimeWait); // Добавили время затухания музыки
                        }
                    }
                }
                if (YsilAutoTimeOn != TimeSpan.Zero && YsilAutoTimeOff != TimeSpan.Zero)
                {
                    YsilAutoTimeOn -= new TimeSpan(0, 0, 30);
                    Log.Write($"[Усилитель] Автоматический режим. Включение: [{YsilAutoTimeOn}], Выключение: [{YsilAutoTimeOff}]."); // .ToString("hh':'mm")
                    lbYsilAutoTimeOn.Text = YsilAutoTimeOn.ToString();
                    lbYsilAutoTimeOff.Text = YsilAutoTimeOff.ToString();
                }
                else
                {
                    Log.Write($"[Усилитель] HEАвтоматический режим. Включение: [{YsilAutoTimeOn}], Выключение: [{YsilAutoTimeOff}].");
                    lbYsilAutoTimeOn.Text = "—?—";
                    lbYsilAutoTimeOff.Text = "—?—";
                }

                UpdateLabelsSvodkaTimeYsil();
            }
            else
            {
                YsilAutoTimeOn = TimeSpan.Zero;
                YsilAutoTimeOff = TimeSpan.Zero;
                lbYsilAutoTimeOn.Text = "—Звонков нет—";
                lbYsilAutoTimeOff.Text = "—Звонков нет—";
            }
        }

        private void UpdateLabelsSvodkaTimeYsil()
        {
            if (workComPort == null) // Если СОМ порт пуст
            {
                labelTimeOnYsil.Text = "——";
                labelTimeOffYsil.Text = "——";

                labelModeOnYsil.Text = " (не выбран порт управления)";
                labelModeOffYsil.Text = " (не выбран порт управления)";
            }
            else
            {
                if (YsilNoControl)
                {
                    labelTimeOnYsil.Text = "——";
                    labelTimeOffYsil.Text = "——";

                    labelModeOnYsil.Text = " (функция не используется)";
                    labelModeOffYsil.Text = " (функция не используется)";
                    return;
                }
                if (YsilAuto)
                {
                    if (YsilAutoTimeOn != TimeSpan.Zero && YsilAutoTimeOff != TimeSpan.Zero)
                    {
                        labelTimeOnYsil.Text = YsilAutoTimeOn.ToString();
                        labelTimeOffYsil.Text = YsilAutoTimeOff.ToString();
                    }
                    else
                    {
                        labelTimeOnYsil.Text = "—Z—";
                        labelTimeOffYsil.Text = "—Z—";
                    }

                    labelModeOnYsil.Text = " (автоматический расчёт)";
                    labelModeOffYsil.Text = " (автоматический расчёт)";
                    return;
                }

                if (YsilTime)
                {
                    labelTimeOnYsil.Text = YsilFixTimeOn.ToString();
                    labelTimeOffYsil.Text = YsilFixTimeOff.ToString();

                    labelModeOnYsil.Text = " (фиксированное время)";
                    labelModeOffYsil.Text = " (фиксированное время)";
                    return;
                }

                if (YsilHands)
                {
                    labelTimeOnYsil.Text = "——";
                    labelTimeOffYsil.Text = "——";

                    labelModeOnYsil.Text = " (вручную)";
                    labelModeOffYsil.Text = " (вручную)";
                    return;
                }
            }
        }

        private void OnYsil(string kak)
        {
            if (workComPort != null && comReady)
            {
                Log.Write($"[Усилитель] ON - Усилитель включен [{kak}]");
                StateYsil = true;
                try
                {
                    workComPort.WriteLine("1");
                    Log.Write("1 отправили");
                    workComPort.WriteLine("1");
                    //workComPort.RtsEnable = true;
                    lbYsilState.Foreground = Brushes.Green;
                    lbYsilState.Text = "ВКЛЮЧЕН";
                }
                catch (Exception e)
                {
                    ShowErrorMB("Ошибка при включении усилителя звука", e.Message, e.ToString());
                    lbYsilState.Foreground = Brushes.Firebrick;
                    lbYsilState.Text = "ВЫКЛЮЧЕН";
                }
            }
        }

        private void OffYsil(string kak)
        {
            if (workComPort != null)
            {
                Log.Write($"[Усилитель] OFF - Усилитель выключен [{kak}]");
                StateYsil = false;
                try
                {
                    workComPort.Write("0");
                    //workComPort.RtsEnable = false;
                    //workComPort.Close();
                    lbYsilState.Foreground = Brushes.Firebrick;
                    lbYsilState.Text = "ВЫКЛЮЧЕН";
                }
                catch (Exception e)
                {
                    ShowErrorMB("Ошибка при выключении усилителя звука", e.Message, e.ToString());
                    lbYsilState.Foreground = Brushes.Firebrick;
                    lbYsilState.Text = "ВЫКЛЮЧЕН";
                }
            }
        }

        private bool ArduinoDetected() // [2020] Проверяем, наше ли ардуино или другой сом порт
        {
            try
            {
                workComPort.DtrEnable = false;
                workComPort.ReadTimeout = 1500;
                workComPort.Open(); // Открываем порт
                System.Threading.Thread.Sleep(2000);
                
                workComPort.WriteLine("5"); // Отправляем туда число для проверки 
                //System.Threading.Thread.Sleep(100);
                //workComPort.WriteLine("5"); // Отправляем туда число для проверки 

                string returnMessage = workComPort.ReadLine(); // Считываем ответ от порта
                workComPort.Close(); // Закрываем порт

                if (returnMessage.Contains("9")) // Если ответ ардуино правильный
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception e)
            {
                this.Dispatcher.Invoke((ThreadStart)delegate {
                    ShowErrorMB("Ошибка при поиске АРДУИНО для управления электропитанием усилителя звука", e.Message, e.ToString());
                });
                return false;
            }
        }

        private void LoadComPorts(byte mode) // [2020] Загрузить порты компа
        {
            //comPorts = null; // Обнулили
            comPorts = SerialPort.GetPortNames(); // Получаем новые
            if (rezComPorts == null) // если пусто - еще не загоняли (первый запуск)
            {
                rezComPorts = new string[comPorts.Length + 2]; // Инициализируем
                comPorts.CopyTo(rezComPorts, 0); // Копируем в резерв текущий список
            }

            if (rezComPorts.Length == comPorts.Length) // Всё осталось как есть
            {
                //Log.Write("Действие с USB устройством, новые СОМ порты не появились. ");
            }
            else
            {
                if (mode == 1)
                {
                    if (rezComPorts.Length > comPorts.Length) // Если было больше чем стало - вынуто
                    {
                        notifyIcon.ShowBalloonTip(5000, "LessonBell", "Отключен модуль управления электропитанием.. Усилитель выключен!", ToolTipIcon.Info);
                    }
                    else
                    {
                        notifyIcon.ShowBalloonTip(5000, "LessonBell", "Подключен модуль управления электропитанием.. Подождите, идет настройка...", ToolTipIcon.Info);
                    }
                }
                Log.Write("Действие с USB устройством, обнаружены новые СОМ порты. " + rezComPorts.Length + " => " + comPorts.Length);
                rezComPorts = new string[comPorts.Length]; // Инициализируем
                comPorts.CopyTo(rezComPorts, 0); // Копируем в резерв текущий список

                if (mode == 1)
                    Thread.Sleep(7000); // Пока ардуино определится в компе

                bool ArduinoPortFound = false;
                foreach (string port in comPorts) // Перебираем
                {
                    workComPort = new SerialPort(port, 9600); // Создаём порт
                    workComPort.DtrEnable = false;
                    if (ArduinoDetected()) // Проверяем ардуино ли это
                    {
                        ArduinoPortFound = true;
                        
                        this.Dispatcher.Invoke((ThreadStart)delegate
                        {
                            Log.Write("Подключение к модулю управления электропитанием УСПЕШНО!");
                            lbYsilCom.Text = "Модуль управления электропитанием подключен";
                        });
                        // Порт найден, установлен
                        workComPort.Open();
                        Thread.Sleep(2000);

                        comReady = true;
                        break;
                    }
                    else
                    {
                        ArduinoPortFound = false;
                    }
                }

                if (!ArduinoPortFound) // Если порт не найден - выход из метода
                {
                    Log.Write("Подключение к модулю управления электропитанием НЕУДАЧНО. МОДУЛЬ НЕ ОБНАРУЖЕН!");
                    this.Dispatcher.Invoke((ThreadStart)delegate
                    {
                        lbYsilCom.Text = "Модуль управления электропитанием не обнаружен!";
                        lbYsilState.Foreground = Brushes.Firebrick;
                        lbYsilState.Text = "ВЫКЛЮЧЕН";
                    });

                    StateYsil = false;
                    workComPort = null;
                    return;
                }

            }
        }

        /*private void comboYsilCom_SelectionChanged(object sender, SelectionChangedEventArgs e) // Изменен СОМ порт
        {
            if (comboYsilCom.SelectedIndex >= 1) // 0 элемент - не выбран
            {
                string aa = SelectedCOM;
                SelectedCOM = comboYsilCom.Items[comboYsilCom.SelectedIndex].ToString();
                workComPort = new SerialPort(SelectedCOM, 9600, Parity.None, 8, StopBits.One);

                SettingsINI.WriteINI("Main", "SelectedCOM", SelectedCOM);
                Log.Write("[Усилитель] Выбран новый СОМ порт: " + workComPort.PortName + ", старый: " + aa);
            }
            else
            {
                if (StateYsil == true)
                {
                    OffYsil("Не выбран СОМ порт!");
                }
                workComPort = null; // выбрано "Не выбран"
            }
            if (!NowLoadSettings)
            {
                UpdateLabelsSvodkaTimeYsil();
            }
        }*/

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            HwndSource source = PresentationSource.FromVisual(this) as HwndSource;
            source.AddHook(WndProc);
        }
        
         IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            // Handle messages...
            if (wParam.ToInt64() == 0x8000 || wParam.ToInt64() == 0x8004) // Включено || Выключено
            {
                //NotifyIcon kek = new NotifyIcon();
                

                new System.Threading.Thread(delegate (object obj) {
                    LoadComPorts(1);
                }).Start();
            }
            return IntPtr.Zero;
        }

        private void btnYsilHandsOn_Click(object sender, RoutedEventArgs e) // Вручную ВКЛЮЧИТЬ усилитель -- [ПОРЯДОК 23 МАРТА]
        {
            OnYsil("Кнопка вкл.вручную");
        }

        private void btnYsilHandsOff_Click(object sender, RoutedEventArgs e) // Вручную ВЫКЛЮЧИТЬ усилитель -- [ПОРЯДОК 23 МАРТА]
        {
            OffYsil("Кнопка выкл.вручную");
        }


        private void btnYsilTimeEdit_Click(object sender, RoutedEventArgs e) // Изменить ВРЕМЯ усилителя -- [ПОРЯДОК 23 МАРТА]
        {
            btnYsilTimeEdit.Visibility = Visibility.Hidden;
            btnYsilTimeSave.Visibility = btnYsilTimeCancel.Visibility = Visibility.Visible; // Спрятали изменить, показали сохранить\отменить

            ReservedTimeOnYsil = tbxYsilTimeOn.Text;
            ReservedTimeOffYsil = tbxYsilTimeOff.Text; // резервно сохранили старые значения

            tbxYsilTimeOn.IsReadOnly = tbxYsilTimeOff.IsReadOnly = false; // разрешили редактирование
        }

        private void btnYsilTimeSave_Click(object sender, RoutedEventArgs e) // Сохранить ВРЕМЯ усилителя -- [ПОРЯДОК 23 МАРТА]
        {
            btnYsilTimeEdit.Visibility = Visibility.Visible;
            btnYsilTimeSave.Visibility = btnYsilTimeCancel.Visibility = Visibility.Hidden; // Спрятали

            tbxYsilTimeOn.IsReadOnly = tbxYsilTimeOff.IsReadOnly = true; // запретили редактирование

            YsilFixTimeOn = TimeSpan.Parse(tbxYsilTimeOn.Text);
            YsilFixTimeOff = TimeSpan.Parse(tbxYsilTimeOff.Text);
            SettingsINI.WriteINI("Main", "YsilFixTimeOn", YsilFixTimeOn.ToString());
            SettingsINI.WriteINI("Main", "YsilFixTimeOff", YsilFixTimeOff.ToString());
            Log.Write($"Время усилителя изменено с [вкл: {ReservedTimeOnYsil}, выкл: {ReservedTimeOffYsil}] на [вкл: {YsilFixTimeOn}, выкл: {YsilFixTimeOff}]");
            CalcTimeYsilOnOff();
        }

        private void btnYsilTimeCancel_Click(object sender, RoutedEventArgs e) // Отменить ВРЕМЯ усилителя -- [ПОРЯДОК 23 МАРТА]
        {
            btnYsilTimeEdit.Visibility = Visibility.Visible;
            btnYsilTimeSave.Visibility = btnYsilTimeCancel.Visibility = Visibility.Hidden; // Спрятали

            tbxYsilTimeOn.Text = ReservedTimeOnYsil;
            tbxYsilTimeOff.Text = ReservedTimeOffYsil; // вернули старые значения

            tbxYsilTimeOn.IsReadOnly = tbxYsilTimeOff.IsReadOnly = true; // запретили редактирование
        }


        private void rbYsilNoControl_Checked(object sender, RoutedEventArgs e) // Не контролировать -- [ПОРЯДОК 23 МАРТА]
        {
            lbYsilNoControl.IsEnabled = YsilNoControl = (bool)rbYsilNoControl.IsChecked;
            if (!NowLoadSettings)
            {
                SettingsINI.WriteINI("Main", "YsilNoControl", YsilNoControl.ToString());
                if (YsilNoControl && workComPort != null && StateYsil) // Если усилитель включен
                {
                    CalcTimeYsilOnOff();
                    OffYsil("Выбран режим 'Не управлять'");
                }
            }
        }

        private void rbYsilAuto_Checked(object sender, RoutedEventArgs e) // Автоматически -- [ПОРЯДОК 23 МАРТА]
        {
            lbYsilAutoTime1.IsEnabled = lbYsilAutoTime2.IsEnabled = lbYsilAutoTime3.IsEnabled =
                    lbYsilAutoTime4.IsEnabled = lbYsilAutoTimeOn.IsEnabled = lbYsilAutoTimeOff.IsEnabled = YsilAuto = (bool)rbYsilAuto.IsChecked;
            if (!NowLoadSettings)
            {
                SettingsINI.WriteINI("Main", "YsilAuto", YsilAuto.ToString());

                if (YsilAuto)
                {
                    CalcTimeYsilOnOff();
                    ControlYsil();
                }
            }

        }

        private void rbYsilTime_Checked(object sender, RoutedEventArgs e) // По времени -- [ПОРЯДОК 23 МАРТА]
        {
            lbYsilTimeOn.IsEnabled = lbYsilTimeOff.IsEnabled = tbxYsilTimeOn.IsEnabled = tbxYsilTimeOff.IsEnabled = YsilTime = (bool)rbYsilTime.IsChecked;
            if (!NowLoadSettings)
            {
                SettingsINI.WriteINI("Main", "YsilTime", YsilTime.ToString());

                if (!YsilTime) // если галка снята
                {
                    // если режим редактирования вкл, выключить его отменой
                    if (btnYsilTimeCancel.Visibility == Visibility.Visible) // если кнопка отмена видна т.е. редактируем
                    {
                        btnYsilTimeCancel_Click(btnYsilTimeCancel, null);
                    }
                    btnYsilTimeEdit.Visibility = Visibility.Hidden;
                }
                else
                {
                    btnYsilTimeEdit.Visibility = Visibility.Visible;
                }

                if (YsilTime)
                {
                    CalcTimeYsilOnOff();
                    ControlYsil();
                }
            }

        }

        private void rbYsilHands_Checked(object sender, RoutedEventArgs e) // Вручную -- [ПОРЯДОК 23 МАРТА]
        {
            btnYsilHandsOn.IsEnabled = btnYsilHandsOff.IsEnabled = YsilHands = (bool)rbYsilHands.IsChecked;
            if (!NowLoadSettings)
            {
                SettingsINI.WriteINI("Main", "YsilHands", YsilHands.ToString());
                if (YsilHands)
                {
                    CalcTimeYsilOnOff();
                    ControlYsil();
                }
            }
        }
    }
}
