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
    // Вкладка Выходные
    public partial class MainWindow
    {
        // ===================================================================================================================================================

        private ObservableCollection<Holiday> HolidaysOneDate = new ObservableCollection<Holiday>(); // Одиночные выходные дни
        private ObservableCollection<Kanukyli> HolidaysTwoDate = new ObservableCollection<Kanukyli>(); // Каникулярное время

        DataContractJsonSerializer jsonSerializerHolidaysOneDate = new DataContractJsonSerializer(typeof(ObservableCollection<Holiday>));
        DataContractJsonSerializer jsonSerializerHolidaysTwoDate = new DataContractJsonSerializer(typeof(ObservableCollection<Kanukyli>));

        // ===================================================================================================================================================

        private void AddHoliday_Click(object sender, RoutedEventArgs e) // Добавить неучебный день
        {
            if (dpDayHoliday.SelectedDate != null) // Если выбранная дата не пустая
            {
                Holiday NewHoliday = new Holiday((DateTime)dpDayHoliday.SelectedDate);

                bool YjeEst = false;
                for (int i = 0; i < HolidaysOneDate.Count; i++) // Проверяем есть ли новый выходной в списке
                {
                    if (HolidaysOneDate[i].Date == NewHoliday.Date)
                    {
                        YjeEst = true;
                        ListViewHolidaysOneDate.SelectedIndex = i;
                        ShowMBinNewThread(MessageBoxIcon.Asterisk, $"Выбранный Вами выходной день уже есть в списке [№ {i+1}]!");
                        break;
                    }
                }
                if (!YjeEst) // Если нового выходного в списке нет
                {
                    bool NowHolidayLi = NowHoliday();
                    HolidaysOneDate.Add(NewHoliday);
                    Log.Write($"[Выходные] Добавлен выходной день: [{NewHoliday.Date.ToShortDateString()}]");
                    if (NowHolidayLi != NowHoliday())
                    {
                        GetTimeBells("Добавлен выходной день");
                    }
                    SortHolidays(HolidaysOneDate);
                    SaveAllHolidays();
                }
            }
        }

        private void RemoveSelectedHoliday_Click(object sender, RoutedEventArgs e) // Удалить неучебный день
        {
            if (ListViewHolidaysOneDate.SelectedItems.Count > 0)
            {
                bool NowHolidayLi = NowHoliday();
                while (ListViewHolidaysOneDate.SelectedItems.Count > 0) // Пока есть выделенные элементы
                {
                    Log.Write($"[Выходные] Удалён выходной день: [{((Holiday)ListViewHolidaysOneDate.SelectedItems[0]).Date.ToShortDateString()}]");
                    HolidaysOneDate.Remove((Holiday)ListViewHolidaysOneDate.SelectedItems[0]); // Удаляем все выделенные
                }
                if (NowHolidayLi != NowHoliday())
                {
                    GetTimeBells("Удалены выходные дни");
                }

                for (int i = 0; i < HolidaysOneDate.Count; i++) // Восстанавливаем нумерацию
                {
                    HolidaysOneDate[i].Number = i + 1;
                }
                ListViewHolidaysOneDate.Items.Refresh();
                SaveAllHolidays();
            }
        }

        // ===================================================================================================================================================

        private void AddKanikylu_Click(object sender, RoutedEventArgs e) // Добавить каникулы
        {
            if (dpStartKanikylu.SelectedDate != null && dpEndKanikylu.SelectedDate != null)
            {
                DateTime DateStartK = (DateTime)dpStartKanikylu.SelectedDate;
                DateTime DateEndK = (DateTime)dpEndKanikylu.SelectedDate;

                if (DateEndK < DateStartK || DateStartK == DateEndK)
                {
                    ShowMBinNewThread(MessageBoxIcon.Exclamation, "Выбранные Вами каникулы не были добавлены в список!\n\nДата конца каникул не может быть раньше или равна дате начала!"); // неправильно задано
                }
                else
                {
                    Kanukyli NewK = new Kanukyli(DateStartK, DateEndK); // Объект каникулы с этими датами

                    bool YjeEst = false;
                    for (int i = 0; i < HolidaysTwoDate.Count; i++)
                    {
                        if ((HolidaysTwoDate[i].DateStart == DateStartK && HolidaysTwoDate[i].DateEnd == DateEndK) ||
                            (DateStartK >= HolidaysTwoDate[i].DateStart && DateStartK <= HolidaysTwoDate[i].DateEnd || DateEndK >= HolidaysTwoDate[i].DateStart && DateEndK <= HolidaysTwoDate[i].DateEnd))
                        {
                            YjeEst = true;
                            ListViewHolidaysTwoDate.SelectedIndex = i;
                            ShowMBinNewThread(MessageBoxIcon.Asterisk, $"Выбранные Вами каникулы уже есть в списке [№ {i+1}] или они пересекаются с этими каникулами!");
                            break;
                        }
                    }
                    if (!YjeEst) // если таких каникул нет
                    {
                        bool NowHolidayLi = NowHoliday();
                        HolidaysTwoDate.Add(NewK);
                        Log.Write($"[Каникулы] Добавлены каникулы: [{NewK.DateStart.ToShortDateString() + " — " + NewK.DateEnd.ToShortDateString()}]");
                        if (NowHolidayLi != NowHoliday())
                        {
                            GetTimeBells("Добавлен выходной день");
                        }
                        SortKanukyls(HolidaysTwoDate);
                        SaveAllHolidays();
                    }
                }
            }
        }

        private void RemoveSelectedKanikylu_Click(object sender, RoutedEventArgs e) // Удалить каникулы
        {
            if (ListViewHolidaysTwoDate.SelectedItems.Count > 0)
            {
                bool NowHolidayLi = NowHoliday();
                while (ListViewHolidaysTwoDate.SelectedItems.Count > 0) // Пока есть выделенные элементы
                {
                    Log.Write($"[Выходные] Удалены каникулы: [{((Kanukyli)ListViewHolidaysTwoDate.SelectedItems[0]).DateStart.ToShortDateString()} - {((Kanukyli)ListViewHolidaysTwoDate.SelectedItems[0]).DateEnd.ToShortDateString()}]");
                    HolidaysTwoDate.Remove((Kanukyli)ListViewHolidaysTwoDate.SelectedItems[0]); // Удаляем все выделенные
                }
                if (NowHolidayLi != NowHoliday())
                {
                    GetTimeBells("Удалены каникулы");
                }

                for (int i = 0; i < HolidaysTwoDate.Count; i++) // Восстанавливаем нумерацию
                {
                    HolidaysTwoDate[i].Number = i + 1;
                }
                ListViewHolidaysTwoDate.Items.Refresh();
                SaveAllHolidays();
            }
        }

        // ===================================================================================================================================================

        private bool NowHoliday() // Проверка выходной/каникулы ли сегодня
        {
            DateTime DateNow = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day);
            for (int i = 0; i < HolidaysOneDate.Count; i++) // Проверяем что текущий день попадает в список выходных дней
            {
                if (HolidaysOneDate[i].Date == DateNow) // .ToString("dd'.'MM")
                {
                    return true; // Сегодня выходной
                }
            }

            for (int i = 0; i < HolidaysTwoDate.Count; i++) // Проверяем что текущий день попадает в какой либо промежуток каникул
            {
                if (DateNow >= HolidaysTwoDate[i].DateStart && DateNow <= HolidaysTwoDate[i].DateEnd)
                {
                    return true; // Сегодня выходной
                }
            }
            return false; // Не выходной
        }

        // ===================================================================================================================================================

        private void SaveAllHolidays() // Сохранить выходные и каникулы (сереализовать)
        {
            using (FileStream fs = new FileStream(FolderSettings + @"\HolidaysOneDate.json", FileMode.Create))
            {
                jsonSerializerHolidaysOneDate.WriteObject(fs, HolidaysOneDate); // Сохраняем выходные
            }

            using (FileStream fs = new FileStream(FolderSettings + @"\HolidaysTwoDate.json", FileMode.Create))
            {
                jsonSerializerHolidaysTwoDate.WriteObject(fs, HolidaysTwoDate); // Сохраняем каникулы
            }
        }

        private void LoadHolidaysOneDate() // Загрузить выходные (десереализовать)
        {
            if (!System.IO.File.Exists(FolderSettings + @"\HolidaysOneDate.json")) // если файла нет
            {
                Log.Write("[Выходные] невозможно загрузить список выходных из файла - файла нет!");
                return; // Выходим из метода т.к. файла нет
            }
            else // Файл есть - попытаться считать
            {
                if (System.IO.File.ReadAllLines(FolderSettings + @"\HolidaysOneDate.json").Length == 0)
                {
                    Log.Write("[Выходные] невозможно загрузить список выходных из файла - файл пуст!");
                    return; // Выходим из метода т.к. файл пуст
                }
                else
                {
                    try // пытаемся считать
                    {
                        using (FileStream fs = new FileStream(FolderSettings + @"\HolidaysOneDate.json", FileMode.Open))
                        {
                            HolidaysOneDate = (ObservableCollection<Holiday>)jsonSerializerHolidaysOneDate.ReadObject(fs);
                        }
                    }
                    catch (Exception e)
                    {
                        ShowErrorMB("Ошибка при десереализации выходных дней!", e.Message, e.ToString());
                    }
                }
            }
        }

        private void LoadHolidaysTwoDate() // Загрузить каникулы (десереализовать)
        {
            if (!System.IO.File.Exists(FolderSettings + @"\HolidaysTwoDate.json")) // если файла нет
            {
                Log.Write("[Каникулы] невозможно загрузить список каникул - файла нет!");
                return; // Выходим из метода т.к. файла нет
            }
            else // Файл есть - попытаться считать
            {
                if (System.IO.File.ReadAllLines(FolderSettings + @"\HolidaysTwoDate.json").Length == 0)
                {
                    Log.Write("[Каникулы] невозможно загрузить список каникул - файл пуст!");
                    return; // Выходим из метода т.к. файл пуст
                }
                else
                {
                    try // пытаемся считать
                    {
                        using (FileStream fs = new FileStream(FolderSettings + @"\HolidaysTwoDate.json", FileMode.Open))
                        {
                            HolidaysTwoDate = (ObservableCollection<Kanukyli>)jsonSerializerHolidaysTwoDate.ReadObject(fs);
                        }
                    }
                    catch (Exception e)
                    {
                        ShowErrorMB("Ошибка при десереализации выходных дней!", e.Message, e.ToString());
                    }
                }
            }
        }

        // ===================================================================================================================================================

        private static ObservableCollection<Holiday> SortHolidays(ObservableCollection<Holiday> orderThoseGroups) // Сортируем список выходных по возрастанию
        {
            ObservableCollection<Holiday> temp;
            temp = new ObservableCollection<Holiday>(orderThoseGroups.OrderBy(p => p.Date));
            orderThoseGroups.Clear();
            foreach (Holiday j in temp) orderThoseGroups.Add(j);

            for (int i = 0; i < orderThoseGroups.Count; i++)
            {
                orderThoseGroups[i].Number = i + 1; // сделали нумерацию
            }

            return orderThoseGroups;
        }

        private static ObservableCollection<Kanukyli> SortKanukyls(ObservableCollection<Kanukyli> orderThoseGroups) // Сортируем список каникул по возрастанию
        {
            ObservableCollection<Kanukyli> temp;
            temp = new ObservableCollection<Kanukyli>(orderThoseGroups.OrderBy(p => p.DateStart));
            orderThoseGroups.Clear();
            foreach (Kanukyli j in temp) orderThoseGroups.Add(j);

            for (int i = 0; i < orderThoseGroups.Count; i++)
            {
                orderThoseGroups[i].Number = i + 1; // сделали нумерацию
            }

            return orderThoseGroups;
        }

        // ===================================================================================================================================================
    }
}
