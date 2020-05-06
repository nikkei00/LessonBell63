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
    // Вкладка Расписания звонков
    public partial class MainWindow
    {
        // ===================================================================================================================================================

        private void AddRasp_Click(object sender, RoutedEventArgs e) // Добавить расписание
        {
            AllRasps.Add(new RaspZvonkov() { NameRasp = "Новое добавленное /без настроек/" });
            AllRasps[AllRasps.Count - 1].Number = AllRasps.Count;
            AllRasps[AllRasps.Count - 1].AoPEdited += MainWindow_AoPEdited;
        }

        private void EditSelectedRasp_Click(object sender, RoutedEventArgs e) // Изменить выбранное расписание
        {
            int Selected = NewlistViewRaspsZvonkov.SelectedIndex;
            if (Selected >= 0)
            {
                WindowEditRasp edit = new WindowEditRasp(this, AllRasps[Selected], Log.File);
                edit.Owner = this;

                edit.ShowDialog();

                if (edit.DialogResult == true)
                {
                    AllRasps[Selected] = RaspVr;
                    GetTimeBells("Изменено расписание " + RaspVr.NameRasp);
                    NewlistViewRaspsZvonkov.Items.Refresh();

                    // сохранить в файл все расписания
                    NewSaveAllRasps();
                    RaspVr = null;
                }
            }
        }

        private void DeleteSelectedRasp_Click(object sender, RoutedEventArgs e) // Удалить выбранное расписание
        {
            if (NewlistViewRaspsZvonkov.SelectedItems.Count > 0)
            {
                while (NewlistViewRaspsZvonkov.SelectedItems.Count > 0) // Пока есть выделенные элементы
                {
                    Log.Write($"[Расп-я звонков] Удалено расп-е звонков: [{((RaspZvonkov)NewlistViewRaspsZvonkov.SelectedItems[0]).NameRasp}]");
                    AllRasps.Remove((RaspZvonkov)NewlistViewRaspsZvonkov.SelectedItems[0]); // Удаляем все выделенные
                }

                for (int i = 0; i < AllRasps.Count; i++) // Восстанавливаем нумерацию
                {
                    AllRasps[i].Number = i + 1;
                }

                GetTimeBells("Удалены расп-я звонков");
                NewlistViewRaspsZvonkov.Items.Refresh();
                NewSaveAllRasps(); // сохранить в файл все расписания
            }
        }

        private void MainWindow_AoPEdited(object sender, string msg) // Изменена Активность или приоритет в расписании
        {
            GetTimeBells(msg + " " + (sender as RaspZvonkov).NameRasp);
        }

        // ===================================================================================================================================================

        private void NewSaveAllRasps() // [NEW NEW NEW]  Сохранить все расписания звонков (сериализовать)
        {
            using (FileStream fs = new FileStream(FolderSettings + @"\Rasps.json", FileMode.Create))
            {
                jsonSerializerRasps.WriteObject(fs, AllRasps);
            }
        }

        private void NewLoadAllRasps() // [NEW NEW NEW]  Загрузить все расписания звонков (десериализовать)
        {
            if (!System.IO.File.Exists(FolderSettings + @"\Rasps.json")) // если файла нет
            {
                System.IO.File.Create(FolderSettings + @"\Rasps.json"); // создать файл
                //ShowMBinNewThread(MessageBoxIcon.Exclamation, "Файл с настройками расписаний не обнаружен!");
            }
            else // Файл есть - попытаться считать
            {
                if (System.IO.File.ReadAllLines(FolderSettings + @"\Rasps.json").Length != 0) // Если файл не пуст
                {
                    try // пытаемся считать
                    {
                        using (FileStream fs = new FileStream(FolderSettings + @"\Rasps.json", FileMode.Open))
                        {
                            AllRasps = (ObservableCollection<RaspZvonkov>)jsonSerializerRasps.ReadObject(fs);

                            for (int i = 0; i < AllRasps.Count; i++) // Перебираем все расписания
                            {
                                AllRasps[i].AoPEdited += MainWindow_AoPEdited; // Привязываем событие изменение активности или приоритета
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        ShowErrorMB("Ошибка при десереализации расписаний звонков", e.Message, e.ToString());
                    }
                }
            }
        }

        private void NewlistViewRaspsZvonkov_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e) // Двойной жмак по расписанию
        {
            var dataContext = ((FrameworkElement)e.OriginalSource).DataContext;

            if (dataContext is RaspZvonkov)
            {
                EditSelectedRasp_Click(NewlistViewRaspsZvonkov, null);
            }
        }

        // ===================================================================================================================================================
    }
}
