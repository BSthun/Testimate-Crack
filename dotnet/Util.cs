using Microsoft.Extensions.Logging;
using Microsoft.Web.WebView2.Core;
using Newtonsoft.Json;
using OpenCvSharp;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Device.Location;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Management;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Threading;
using Testiamte.Enum;
using Testiamte;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Drawing.Imaging;

namespace Testimate
{
    internal class Util
    {
        private static VideoCapture videoCapture;
        public static System.Drawing.Image CaptureWindow(IntPtr handle)
        {
            // get te hDC of the target window
            IntPtr hdcSrc = User32.GetWindowDC(handle);
            // get the size
            User32.RECT windowRect = new User32.RECT();
            User32.GetWindowRect(handle, ref windowRect);
            int width = windowRect.right - windowRect.left;
            int height = windowRect.bottom - windowRect.top;
            // create a device context we can copy to
            IntPtr hdcDest = GDI32.CreateCompatibleDC(hdcSrc);
            // create a bitmap we can copy it to,
            // using GetDeviceCaps to get the width/height
            IntPtr hBitmap = GDI32.CreateCompatibleBitmap(hdcSrc, width, height);
            // select the bitmap object
            IntPtr hOld = GDI32.SelectObject(hdcDest, hBitmap);
            // bitblt over
            GDI32.BitBlt(hdcDest, 0, 0, width, height, hdcSrc, 0, 0, GDI32.SRCCOPY);
            // restore selection
            GDI32.SelectObject(hdcDest, hOld);
            // clean up 
            GDI32.DeleteDC(hdcDest);
            User32.ReleaseDC(handle, hdcSrc);
            // get a .NET image object for it
            System.Drawing.Image img = System.Drawing.Image.FromHbitmap(hBitmap);
            // free up the Bitmap object
            GDI32.DeleteObject(hBitmap);
            return img;
        }

        public static string CaptureWindowToFile(IntPtr handle, ImageFormat format)
        {
            string str = Path.Combine(Path.GetTempPath(), "Testimate", "Screenshots");
            if (!Directory.Exists(str))
                Directory.CreateDirectory(str);
            string path2 = "ScreenCapture-" + DateTime.Now.ToString("ddMMyyyy-hhmmss") + ".png";
            string filename = Path.Combine(str, path2);

            System.Drawing.Image img = CaptureWindow(handle);
            img.Save(filename, format);

            return filename;
        }

        public static string CaptureWindowWrapper()
        {
            string wrap = CaptureWindowToFile(WinGetHandle(), ImageFormat.Png);
            return wrap;
        }


        public static IntPtr WinGetHandle(string wName = "MainWindow")
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

        public static string CaptureScreen(System.Windows.Window w)
        {
            double virtualScreenLeft = SystemParameters.VirtualScreenLeft;
            double virtualScreenTop = SystemParameters.VirtualScreenTop;
            double virtualScreenWidth = SystemParameters.VirtualScreenWidth;
            double virtualScreenHeight = SystemParameters.VirtualScreenHeight;
            PresentationSource presentationSource = PresentationSource.FromVisual((Visual)w);
            double num1 = 1.0;
            double num2 = 1.0;
            if (presentationSource != null)
            {
                num1 = presentationSource.CompositionTarget.TransformToDevice.M11;
                num2 = presentationSource.CompositionTarget.TransformToDevice.M22;
            }
            double num3 = virtualScreenWidth * num1;
            double num4 = virtualScreenHeight * num2;
            string str = Path.Combine(Path.GetTempPath(), "Testimate", "Screenshots");
            if (!Directory.Exists(str))
                Directory.CreateDirectory(str);
            string path2 = "ScreenCapture-" + DateTime.Now.ToString("ddMMyyyy-hhmmss") + ".png";
            string filename = Path.Combine(str, path2);
            Bitmap bitmap1 = new Bitmap((int)num3, (int)num4);
            using (Graphics graphics = Graphics.FromImage((System.Drawing.Image)bitmap1))
            {
                // this.Opacity = 0.0;
                graphics.CopyFromScreen((int)virtualScreenLeft, (int)virtualScreenTop, 0, 0, bitmap1.Size);
                // this.Opacity = 1.0;
            }
            int x1 = 0;
            int y1 = 0;
            int x2 = 0;
            int y2 = 0;
            int num5 = 1280;
            int num6 = 720;
            float num7 = (float)num5 / (float)num3;
            float num8 = (float)num6 / (float)num4;
            float num9;
            if ((double)num8 < (double)num7)
            {
                num9 = num8;
                x2 = (int)Convert.ToInt16(((double)num5 - num3 * (double)num9) / 2.0);
            }
            else
            {
                num9 = num7;
                y2 = (int)Convert.ToInt16(((double)num6 - num4 * (double)num9) / 2.0);
            }
            int width = (int)(num3 * (double)num9);
            int height = (int)(num4 * (double)num9);
            using (Bitmap bitmap2 = new Bitmap(1280, 720))
            {
                bitmap2.SetResolution(bitmap1.HorizontalResolution, bitmap1.VerticalResolution);
                using (Graphics graphics = Graphics.FromImage((System.Drawing.Image)bitmap2))
                {
                    graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    graphics.DrawImage((System.Drawing.Image)bitmap1, new Rectangle(x2, y2, width, height), new Rectangle(x1, y1, (int)num3, (int)num4), GraphicsUnit.Pixel);
                    bitmap2.Save(filename);
                }
            }
            bitmap1.Dispose();
            return filename;
        }

        public static string CaptureCamera()
        {
            if (videoCapture != null)
            {
                if (videoCapture.IsDisposed || true)
                {
                    try
                    {
                        Mat image = new Mat();
                        string str = Path.Combine(Path.GetTempPath(), "Testimate", nameof(CaptureCamera));
                        if (!Directory.Exists(str))
                            Directory.CreateDirectory(str);
                        string path2 = "CaptureCamera-" + DateTime.Now.ToString("ddMMyyyy-hhmmss") + ".jpeg";
                        string fileName = Path.Combine(str, path2);
                        videoCapture.Read(image);
                        if (image.Empty())
                            return string.Empty;
                        image.SaveImage(fileName, (int[])null);
                        return fileName;
                    }
                    catch (Exception ex)
                    {
                        return string.Empty;
                    }
                }
                else
                {
                    MessageBox.Show("aaa");
                }
            }
            return string.Empty;
        }

        public static void StartCamera()
        {
            videoCapture = new VideoCapture(VideoCaptureConfig.CameraId, VideoCaptureAPIs.DSHOW);
            if (!videoCapture.IsOpened())
                return;
            videoCapture.FrameWidth = VideoCaptureConfig.Width;
            videoCapture.FrameHeight = VideoCaptureConfig.Height;
            videoCapture.AutoFocus = VideoCaptureConfig.AutoFocus;
        }

        public static void StopCamera()
        {
            try
            {
                if (videoCapture == null)
                    return;
                videoCapture.Release();
            }
            catch (Exception ex)
            {
            }
        }
        private class GDI32
        {

            public const int SRCCOPY = 0x00CC0020; // BitBlt dwRop parameter
            [DllImport("gdi32.dll")]
            public static extern bool BitBlt(IntPtr hObject, int nXDest, int nYDest,
                int nWidth, int nHeight, IntPtr hObjectSource,
                int nXSrc, int nYSrc, int dwRop);
            [DllImport("gdi32.dll")]
            public static extern IntPtr CreateCompatibleBitmap(IntPtr hDC, int nWidth,
                int nHeight);
            [DllImport("gdi32.dll")]
            public static extern IntPtr CreateCompatibleDC(IntPtr hDC);
            [DllImport("gdi32.dll")]
            public static extern bool DeleteDC(IntPtr hDC);
            [DllImport("gdi32.dll")]
            public static extern bool DeleteObject(IntPtr hObject);
            [DllImport("gdi32.dll")]
            public static extern IntPtr SelectObject(IntPtr hDC, IntPtr hObject);
        }

        private class User32
        {
            [StructLayout(LayoutKind.Sequential)]
            public struct RECT
            {
                public int left;
                public int top;
                public int right;
                public int bottom;
            }
            [DllImport("user32.dll")]
            public static extern IntPtr GetDesktopWindow();
            [DllImport("user32.dll")]
            public static extern IntPtr GetWindowDC(IntPtr hWnd);
            [DllImport("user32.dll")]
            public static extern IntPtr ReleaseDC(IntPtr hWnd, IntPtr hDC);
            [DllImport("user32.dll")]
            public static extern IntPtr GetWindowRect(IntPtr hWnd, ref RECT rect);
        }
    }
}
