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
    public partial class MainWindow
    {
        NotifyIcon notifyIcon = new NotifyIcon();
        ContextMenuStrip cms = new ContextMenuStrip();
        // -----------------------------------
        #region Трей
        public static string FirstUpper(string str) // Сделать первый символ строки заглавным, остальные маленькими
        {
            return str.Substring(0, 1).ToUpper() + (str.Length > 1 ? str.Substring(1) : "");
        }
        protected override void OnStateChanged(EventArgs e) // Свернуть в трей
        {
            if (WindowState == System.Windows.WindowState.Minimized) // свернули
            {
                Hide();
                if (!NowLoadSettings)
                {
                    SettingsINI.WriteINI("Main", "VolumeMusic", axWmpMusic.settings.volume.ToString());
                    SettingsINI.WriteINI("Main", "VolumeDops", axWmpDops.settings.volume.ToString()); // сохраняем настройку громкости плееров

                    tabControl.SelectedIndex = 0; // выбираем вкладку "главная"
                    ListViewHolidaysOneDate.SelectedItems.Clear(); // Убираем выделения в списках
                    ListViewHolidaysTwoDate.SelectedItems.Clear();
                    NewlistViewRaspsZvonkov.SelectedIndex = -1;
                    NewlistViewBells.SelectedIndex = -1;
                    NewlistViewDops.SelectedIndex = -1;
                }
            }

            base.OnStateChanged(e);
        }

        private void SetNotifyIcon() // Установить меню и иконку
        {
            cms.Cursor = System.Windows.Forms.Cursors.Hand;
            cms.Items.Add("Открыть LessonBell");
            cms.Items[0].Image = Properties.Resources.bigBell;
            cms.Items[0].Click +=
                delegate (object sender, EventArgs e)
                {
                    Show();
                    ShowInTaskbar = true;
                    WindowState = WindowState.Normal;
                };


            ToolStripSeparator stripSeparator1 = new ToolStripSeparator();
            stripSeparator1.Alignment = ToolStripItemAlignment.Right;//right alignment
            cms.Items.Add(stripSeparator1);

            //cms.Items.Add("О программе");
            //cms.Items[2].Click +=
            //    delegate (object sender, EventArgs e)
            //    {
            //        // о программе
            //    };

            cms.Items.Add("Выход");
            //cms.Items[3].Image = Properties.Resources.powerOFF;
            cms.Items[2].Click +=
                delegate (object sender, EventArgs e)
                {
                    SettingsINI.WriteINI("Main", "VolumeMusic", axWmpMusic.settings.volume.ToString());
                    SettingsINI.WriteINI("Main", "VolumeDops", axWmpDops.settings.volume.ToString()); // сохраняем настройку громкости плееров
                    Close();
                };


            notifyIcon.Icon = Properties.Resources.bigICO;
            notifyIcon.Visible = true;
            notifyIcon.DoubleClick +=
                delegate (object sender, EventArgs e)
                {
                    Show();
                    ShowInTaskbar = true;
                    WindowState = WindowState.Normal;
                };
            notifyIcon.ContextMenuStrip = cms;
            notifyIcon.MouseUp += notifyIcon1_MouseUp;
            notifyIcon.Text = "LessonBell\nАвтоматическая подача звонков и музыка на переменах";

        }

        private void notifyIcon1_MouseUp(object sender, System.Windows.Forms.MouseEventArgs e) // Показать меню по нажатию ПКМ
        {
            if (e.Button == MouseButtons.Right)
            {
                MethodInfo mi = typeof(NotifyIcon).GetMethod("ShowContextMenu", BindingFlags.Instance | BindingFlags.NonPublic);
                mi.Invoke(notifyIcon, null);
            }
        }

        #endregion
        // -----------------------------------

        private void ShowMBinNewThread(MessageBoxIcon icon, string text) // Показ MessageBox в новом потоке
        {
            //System.Windows.MessageBox.Show(this, text, this.Title, MessageBoxButton.OK, MessageBoxImage.Information,
            //    MessageBoxResult.OK, System.Windows.MessageBoxOptions.ServiceNotification); // Сообщение с ошибкой
            //new Thread(() => System.Windows.MessageBox.Show(this, text, this.Title, MessageBoxButton.OK, MessageBoxImage.Information,
            //    MessageBoxResult.OK, System.Windows.MessageBoxOptions.ServiceNotification)).Start(); // Сообщение с ошибкой


            new Thread(() => System.Windows.Forms.MessageBox.Show(text, "LessonBell - Подача звонков и музыка на переменах", MessageBoxButtons.OK, icon,
                MessageBoxDefaultButton.Button1, System.Windows.Forms.MessageBoxOptions.ServiceNotification)).Start(); // Сообщение с ошибкой
        }

        private void ShowErrorMB(string Msg, string eMessage, string eToString)
        {
            //System.Windows.MessageBox.Show(this, Msg + "\n\n---------------\n\n" + eMessage + "\n\n---------------\n\n" + eToString,
            //    "LessonBell - Ошибка!", MessageBoxButton.OK, MessageBoxImage.Error,
            //    MessageBoxResult.OK, System.Windows.MessageBoxOptions.ServiceNotification); // Сообщение с ошибкой
            //new Thread(() => System.Windows.MessageBox.Show(this, Msg + "\n\n---------------\n\n" + eMessage + "\n\n---------------\n\n" + eToString,
            //    "LessonBell - Ошибка!", MessageBoxButton.OK, MessageBoxImage.Error,
            //    MessageBoxResult.OK, System.Windows.MessageBoxOptions.ServiceNotification)).Start(); // Сообщение с ошибкой


            new Thread(() => System.Windows.Forms.MessageBox.Show(Msg + "\n\n---------------\n\n" + eMessage + "\n\n---------------\n\n" + eToString,
                "LessonBell - Ошибка!", MessageBoxButtons.OK, MessageBoxIcon.Error,
                MessageBoxDefaultButton.Button1, System.Windows.Forms.MessageBoxOptions.ServiceNotification)).Start(); // Сообщение с ошибкой
            Log.Write("-");
            Log.Write(Msg);
            Log.Write("-");
            Log.Write(eMessage);
            Log.Write("-");
            Log.Write(eToString);
            Log.Write("-");
        }


        // -----------------------------------
        #region Сделать неактивной кнопку "Закрыть"
        [DllImport("user32.dll", EntryPoint = "GetSystemMenu")]
        private static extern IntPtr GetSystemMenu(IntPtr hwnd, int revert);

        [DllImport("user32.dll", EntryPoint = "GetMenuItemCount")]
        private static extern int GetMenuItemCount(IntPtr hmenu);

        [DllImport("user32.dll", EntryPoint = "RemoveMenu")]
        private static extern int RemoveMenu(IntPtr hmenu, int npos, int wflags);

        [DllImport("user32.dll", EntryPoint = "DrawMenuBar")]
        private static extern int DrawMenuBar(IntPtr hwnd);

        private const int MF_BYPOSITION = 0x0400;
        private const int MF_DISABLED = 0x0002;



        private void MainWindow_SourceInitialized(object sender, EventArgs e)
        {
            WindowInteropHelper helper = new WindowInteropHelper(this);
            IntPtr windowHandle = helper.Handle; //Get the handle of this window
            IntPtr hmenu = GetSystemMenu(windowHandle, 0);
            int cnt = GetMenuItemCount(hmenu);
            //remove the button
            RemoveMenu(hmenu, cnt - 1, MF_DISABLED | MF_BYPOSITION);
            //remove the extra menu line
            RemoveMenu(hmenu, cnt - 2, MF_DISABLED | MF_BYPOSITION);
            DrawMenuBar(windowHandle); //Redraw the menu bar
        }
        #endregion
        // -----------------------------------
    }
}
