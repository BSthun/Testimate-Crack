using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Management;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using Testiamte.ViewModel;
using System.Threading.Tasks;
using System.IO;
using Testimate;
using System.Drawing;
using System.Drawing.Imaging;

namespace Testiamte
{
    public partial class Credit : Window, IComponentConnector
    {

        public Credit()
        {
            this.InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }

        private async void TestScreenshot_Click(object sender, RoutedEventArgs e)
        {

            this.DialogResult = true;
            // Util.CaptureScreen(this);
            MessageBox.Show("Capturing 5 screenshots with interval of 2s. Try switching window for make sure that the program can see only Testimate window.", "Camera Capture", MessageBoxButton.OK, MessageBoxImage.Information);
            await Task.Delay(500);

            for (int i = 0; i < 5; i++)
            {
                Util.CaptureWindowToFile(WinGetHandle("MainWindow"), ImageFormat.Png);
                await Task.Delay(2000);
            }

            Process.Start("explorer.exe", Path.Combine(Path.GetTempPath(), "Testimate", "Screenshots"));

            Credit credit = new Credit();
            credit.ShowDialog();
        }

        private void TestCamera_Click(object sender, RoutedEventArgs e)
        {
            Util.StartCamera();
            string capturePath = Util.CaptureCamera();
            Process.Start("explorer.exe", string.Format("/select, \"{0}\"", capturePath));
            Util.StopCamera();
        }
        public static IntPtr WinGetHandle(string wName)
        {
            string names = String.Empty;
            foreach (Process pList in Process.GetProcesses())
            {
                if (pList.MainWindowTitle == "")
                    continue;
                names += pList.MainWindowTitle + "\n";
                if (pList.MainWindowTitle.Contains(wName))
                {
                    return pList.MainWindowHandle;
                }
            }
            MessageBox.Show(names, "No window handle found", MessageBoxButton.OK, MessageBoxImage.Error);
            return IntPtr.Zero;
        }
    }
}
