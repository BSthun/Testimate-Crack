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
            await Task.Delay(1000);
            Process.Start("explorer.exe", Path.Combine(Path.GetTempPath(), "Testimate", "Screenshots"));
        }

        private async void TestCamera_Click(object sender, RoutedEventArgs e)
        {
            Util.StartCamera();
            string a = Util.CaptureCamera();
            MessageBox.Show(a, "Camera Capture", MessageBoxButton.OK, MessageBoxImage.Information);
            Process.Start("explorer.exe", Path.Combine(Path.GetTempPath(), "Testimate", "CaptureCamera"));
            Util.StopCamera();
        }
    }
}
