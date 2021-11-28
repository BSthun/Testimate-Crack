using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Management;
using System.Windows;
using System.Windows.Markup;
using Testiamte.ViewModel;
using System.Threading.Tasks;
using System.IO;
using Testimate;
using System.Drawing.Imaging;
using System.Drawing;
using Microsoft.Web.WebView2.Wpf;

namespace Testiamte
{
    public partial class Credit : Window, IComponentConnector
    {

        private WebView2 webView;

        public Credit(WebView2 webView)
        {
            this.InitializeComponent();
            this.webView = webView;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }

        private async void TestScreenshot_Click(object sender, RoutedEventArgs e)
        {

            this.DialogResult = true;

            MessageBox.Show("Capturing 5 screenshots with interval of 2s. Try joining the exam and switching (Alt+Tab) between windows for make sure that the program can capture only Testimate screen.", "Camera Capture", MessageBoxButton.OK, MessageBoxImage.Information);
            await Task.Delay(1000);

            for (int i = 0; i < 5; i++)
            {
                await Util.CaptureWindowWrapper(webView);
                await Task.Delay(2000);
            }

            Process.Start("explorer.exe", Path.Combine(Path.GetTempPath(), "Testimate", "Screenshots"));

            Credit credit = new Credit(this.webView);
            credit.ShowDialog();
        }

        private void TestCamera_Click(object sender, RoutedEventArgs e)
        {
            Util.StartCamera();
            string capturePath = Util.CaptureCamera();
            Process.Start("explorer.exe", string.Format("/select, \"{0}\"", capturePath));
            Util.StopCamera();
        }
    }
}
