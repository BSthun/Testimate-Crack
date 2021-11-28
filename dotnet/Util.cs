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
using System.Windows.Threading;
using Testiamte.Enum;
using Testiamte;
using Newtonsoft.Json.Linq;
using Microsoft.Web.WebView2.Wpf;
using System.Drawing.Imaging;

namespace Testimate
{
    internal class Util
    {
        private static VideoCapture videoCapture;

        public static Bitmap CaptureWindowScreen(IntPtr handle)
        {
            var rect = new User32.RECT();
            User32.GetWindowRect(handle, ref rect);
            var bounds = new Rectangle(rect.left, rect.top, rect.right - rect.left, rect.bottom - rect.top);
            var result = new Bitmap(bounds.Width, bounds.Height);

            using (var graphics = Graphics.FromImage(result))
            {
                graphics.CopyFromScreen(new System.Drawing.Point(bounds.Left, bounds.Top), System.Drawing.Point.Empty, bounds.Size);
            }

            return result;
        }

        public static Bitmap CaptureWindowRegion(IntPtr handle)
        {
            User32.RECT rc = new User32.RECT();
            User32.GetWindowRect(handle, ref rc);

            Bitmap bmp = new Bitmap(rc.right - rc.left, rc.bottom - rc.top, PixelFormat.Format32bppArgb);
            Graphics gfxBmp = Graphics.FromImage(bmp);
            IntPtr hdcBitmap;
            try
            {
                hdcBitmap = gfxBmp.GetHdc();
            }
            catch
            {
                return null;
            }
            bool succeeded = User32.PrintWindow(handle, hdcBitmap, 0);
            gfxBmp.ReleaseHdc(hdcBitmap);
            if (!succeeded)
            {
                gfxBmp.FillRectangle(new SolidBrush(Color.Gray), new Rectangle(System.Drawing.Point.Empty, bmp.Size));
            }
            IntPtr hRgn = GDI32.CreateRectRgn(0, 0, 0, 0);
            User32.GetWindowRgn(handle, hRgn);
            Region region = Region.FromHrgn(hRgn);
            if (!region.IsEmpty(gfxBmp))
            {
                gfxBmp.ExcludeClip(region);
                gfxBmp.Clear(Color.Transparent);
            }
            gfxBmp.Dispose();
            return bmp;
        }

        public static async Task<string> CaptureWindowWrapper(WebView2 webview)
        {
            IntPtr handle = WinGetHandle();

            string str = Path.Combine(Path.GetTempPath(), "Testimate", "Screenshots");
            if (!Directory.Exists(str))
                Directory.CreateDirectory(str);
            string path2 = "ScreenCapture-" + DateTime.Now.ToString("ddMMyyyy-hhmmss") + ".png";
            string filename = Path.Combine(str, path2);

            // Method 1: Make the window foreground and capture it.

            // User32.SetForegroundWindow(handle);
            // System.Drawing.Image img = CaptureWindow(handle);
            // img.Save(filename, ImageFormat.Png);

            // Method 2: Window region
            System.Drawing.Image img = CaptureWindowRegion(handle);
            await OverlayWebview(filename, img, webview);
            return filename;
        }

        public static async Task<string> OverlayWebview(string filename, System.Drawing.Image window, WebView2 webview)
        {
            string r3 = await webview.CoreWebView2.CallDevToolsProtocolMethodAsync("Page.captureScreenshot", "{}");
            JObject o3 = JObject.Parse(r3);
            JToken data = o3["data"];
            string data_str = data.ToString();
            System.Drawing.Image img = Util.Base64Image(data_str);

            // Hard-coded border removal (14px margin)
            System.Drawing.Image comp = new Bitmap(window.Width - 14, window.Height - 14);
            using (Graphics gr = Graphics.FromImage(comp))
            {
                gr.DrawImage(window, new System.Drawing.Point(-7, -7));
                gr.DrawImage(img, new System.Drawing.Point((window.Width - img.Width - 14) / 2, window.Height - img.Height - 14));
            }

            System.Drawing.Image compResized = FixedSize(comp, 1280, 720);

            compResized.Save(filename, ImageFormat.Png);

            img.Dispose();
            comp.Dispose();
            compResized.Dispose();

            return filename;
        }

        static System.Drawing.Image FixedSize(System.Drawing.Image imgPhoto, int Width, int Height)
        {
            int sourceWidth = imgPhoto.Width;
            int sourceHeight = imgPhoto.Height;
            int sourceX = 0;
            int sourceY = 0;
            int destX = 0;
            int destY = 0;

            float nPercent = 0;
            float nPercentW = 0;
            float nPercentH = 0;

            nPercentW = ((float)Width / (float)sourceWidth);
            nPercentH = ((float)Height / (float)sourceHeight);
            if (nPercentH < nPercentW)
            {
                nPercent = nPercentH;
                destX = System.Convert.ToInt16((Width -
                              (sourceWidth * nPercent)) / 2);
            }
            else
            {
                nPercent = nPercentW;
                destY = System.Convert.ToInt16((Height -
                              (sourceHeight * nPercent)) / 2);
            }

            int destWidth = (int)(sourceWidth * nPercent);
            int destHeight = (int)(sourceHeight * nPercent);

            Bitmap bmPhoto = new Bitmap(Width, Height,
                              PixelFormat.Format24bppRgb);
            bmPhoto.SetResolution(imgPhoto.HorizontalResolution,
                             imgPhoto.VerticalResolution);

            Graphics grPhoto = Graphics.FromImage(bmPhoto);
            grPhoto.Clear(Color.White);
            grPhoto.InterpolationMode =
                    InterpolationMode.HighQualityBicubic;

            grPhoto.DrawImage(imgPhoto,
                new Rectangle(destX, destY, destWidth, destHeight),
                new Rectangle(sourceX, sourceY, sourceWidth, sourceHeight),
                GraphicsUnit.Pixel);

            grPhoto.Dispose();
            return bmPhoto;
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
            PresentationSource presentationSource = PresentationSource.FromVisual(w);
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
            using (Graphics graphics = Graphics.FromImage(bitmap1))
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
                    graphics.DrawImage(bitmap1, new Rectangle(x2, y2, width, height), new Rectangle(x1, y1, (int)num3, (int)num4), GraphicsUnit.Pixel);
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

        public static System.Drawing.Image Base64Image(string base64)
        {
            //data:image/gif;base64,
            //this image is a single pixel (black)
            byte[] bytes = Convert.FromBase64String(base64);

            System.Drawing.Image image;
            using (MemoryStream ms = new MemoryStream(bytes))
            {
                image = System.Drawing.Image.FromStream(ms);
            }

            return image;
        }

        private class GDI32
        {
            [DllImport("gdi32.dll")]
            public static extern IntPtr CreateRectRgn(int nLeftRect, int nTopRect, int nRightRect, int nBottomRect);
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
            public static extern IntPtr GetWindowRect(IntPtr hWnd, ref RECT rect);

            [DllImport("user32.dll")]
            public static extern bool SetForegroundWindow(IntPtr hWnd);

            [DllImport("user32.dll")]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool PrintWindow(IntPtr hwnd, IntPtr hDC, uint nFlags);

            [DllImport("user32.dll")]
            public static extern int GetWindowRgn(IntPtr hWnd, IntPtr hRgn);
        }
    }

}
