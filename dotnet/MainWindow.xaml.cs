// Decompiled with JetBrains decompiler
// Type: Testiamte.MainWindow
// Assembly: Testiamte, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 1835882B-FCD8-4F86-8423-F7808C83A056
// Assembly location: D:\Testimate\Testimate\bin\Testiamte.dll

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
using Testiamte.ViewModel;
using Testimate;

namespace Testiamte
{
    public partial class MainWindow : System.Windows.Window, IComponentConnector
    {
        public static CoreWebView2Environment webView2Environment;
        private GeoCoordinateWatcher watcher;
        private bool hasToken;
        private double lat;
        private double lng;
        private ExamInfo examInfo;
        private List<LogApp> logApps;
        private static string lastTitle;
        private static int lastHandle;
        private GlobalDatetime globalDatetime;
        private bool isStart;
        private static DispatcherTimer dispatcherTimer;
        private static DispatcherTimer dispatcherTimerLog;
        private static DispatcherTimer dispatcherTimerSendLog;
        private static DispatcherTimer dispatcherTimerToken;
        private const string CLIENT_KEY = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855";
        private const string MAIN_URL = "https://main-api.testimate.app";
        private const string SITE_URL = "";
        private const string Exam_URL = "join.testimate.app";
        private static bool IsNavigatedToExam;
        private Setting setting;
        private readonly ILogger _logger;
        private IntPtr ptrHook;
        private MainWindow.LowLevelKeyboardProc objKeyboardProcess;

        public static EventHandler<EventArgs> OnCameraDeviceChanged { get; set; }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        public MainWindow(ILogger<MainWindow> logger)
        {
            this._logger = (ILogger)logger;
            this._logger.LogInformation("starting main window");
            MainWindow.dispatcherTimer = new DispatcherTimer();
            MainWindow.dispatcherTimerLog = new DispatcherTimer();
            MainWindow.dispatcherTimerSendLog = new DispatcherTimer();
            MainWindow.dispatcherTimerToken = new DispatcherTimer();
            this.logApps = new List<LogApp>();
            this.globalDatetime = new GlobalDatetime();
            this.InitializeComponent();
            this.FullScreen();
            this.addressBar.Text = "Paste or type a URL here..";
            this.addressBar.GotFocus += new RoutedEventHandler(this.ButtonGo_GotFocus);
            this.addressBar.LostFocus += new RoutedEventHandler(this.ButtonGo_LostFocus);
            this.addressBar.Foreground = (System.Windows.Media.Brush)System.Windows.Media.Brushes.Gray;
        }

        private void ButtonGo_LostFocus(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(this.addressBar.Text))
                return;
            this.addressBar.Text = "Paste or type a URL here..";
            this.addressBar.Foreground = (System.Windows.Media.Brush)System.Windows.Media.Brushes.Gray;
        }

        private void ButtonGo_GotFocus(object sender, RoutedEventArgs e)
        {
            if (!(this.addressBar.Text == "Paste or type a URL here.."))
                return;
            this.addressBar.Text = "";
            this.addressBar.Foreground = (System.Windows.Media.Brush)System.Windows.Media.Brushes.Black;
        }

        protected virtual void OnActivated(EventArgs e)
        {
            base.OnActivated(e);
            if (this.setting == null)
                return;
            this.setting.Hide();
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            base.OnKeyUp(e);
            if (e.Key != Key.Return || string.IsNullOrEmpty(this.addressBar.Text) || MainWindow.IsNavigatedToExam)
                return;
            this.NavigateToExam();
        }

        private void FullScreen()
        {
            this.WindowState = WindowState.Maximized;
            this.WindowStyle = WindowStyle.None;
        }

        private void EnsureHttps(object sender, CoreWebView2NavigationStartingEventArgs args)
        {
            if (args.Uri.StartsWith("https://"))
                return;
            args.Cancel = true;
        }

        private void ButtonGo_Click(object sender, RoutedEventArgs e) => this.NavigateToExam();

        private void NavigateToExam()
        {
            if (this.addressBar.Text.Contains("join.testimate.app"))
            {
                if (this.webView == null || this.webView.CoreWebView2 == null)
                    return;
                this.webView.CoreWebView2.Navigate(this.addressBar.Text);
                this.webView.CoreWebView2.NavigationCompleted -= new EventHandler<CoreWebView2NavigationCompletedEventArgs>(this.CoreWebView2_NavigationCompleted);
                this.webView.CoreWebView2.NavigationCompleted += new EventHandler<CoreWebView2NavigationCompletedEventArgs>(this.CoreWebView2_NavigationCompleted);
            }
            else
            {
                int num = (int)MessageBox.Show("It looks like you have enter an incorrect url for the exam. Please make sure you have enter a valid exam url.");
            }
        }

        private void CoreWebView2_NavigationCompleted(
          object sender,
          CoreWebView2NavigationCompletedEventArgs e)
        {
            MainWindow.IsNavigatedToExam = true;
            this.SetClientKey();
        }

        private void Setting_Click(object sender, RoutedEventArgs e)
        {
            this.setting = new Setting();
            this.setting.Show();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Credit credit = new Credit();
            credit.ShowDialog();

            MainWindow mainWindow = this;
            try
            {
                mainWindow.GetLocation();
                mainWindow.WebView_Initialized();
                // Util.StartCamera();
                MainWindow.dispatcherTimer.Tick += new EventHandler(mainWindow.DispatcherTimer_Tick);
                MainWindow.dispatcherTimer.Interval = new TimeSpan(0, 0, 10);
                MainWindow.dispatcherTimer.Start();
                MainWindow.dispatcherTimerLog.Tick += new EventHandler(mainWindow.DispatcherTimer_GetActiveWindow);
                MainWindow.dispatcherTimerLog.Interval = new TimeSpan(0, 0, 1);
                MainWindow.dispatcherTimerLog.Start();
                MainWindow.dispatcherTimerSendLog.Tick += new EventHandler(mainWindow.DispatcherTimer_Sendlog);
                MainWindow.dispatcherTimerSendLog.Interval = new TimeSpan(0, 0, 10);
                MainWindow.dispatcherTimerSendLog.Start();
                MainWindow.dispatcherTimerToken.Tick += new EventHandler(mainWindow.DispatcherTimerToken_Tick);
                MainWindow.dispatcherTimerToken.Interval = new TimeSpan(0, 0, 3);
                MainWindow.dispatcherTimerToken.Start();
                // ISSUE: reference to a compiler-generated method
                // MainWindow.OnCameraDeviceChanged += new EventHandler<EventArgs>(mainWindow.Window_Loaded);
                
                // mainWindow.LockScreen();
            }
            catch (Exception ex)
            {
                int num = (int)MessageBox.Show("{0}", ex.Message);
            }
        }

        
        private bool CheckVM()
        {
            return false;
        }

        private async void WebView_Initialized() => await this.webView.EnsureCoreWebView2Async(await CoreWebView2Environment.CreateAsync((string)null, Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\Testimate", (CoreWebView2EnvironmentOptions)null));

        private async Task<ExamInfo> GetTokenFromJsAsync()
        {
            if (!this.hasToken)
            {
                while (!this.hasToken)
                {
                    string str = await this.webView.ExecuteScriptAsync("getExamInfo()");
                    if (str != "null")
                    {
                        this.hasToken = true;
                        return JsonConvert.DeserializeObject<ExamInfo>(str);
                    }
                }
            }
            return (ExamInfo)null;
        }

        private async void DispatcherTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                if (!this.hasToken)
                    return;
                if (!this.isStart)
                {
                    List<Log> dataLogs = new List<Log>();
                    List<Log> logList = dataLogs;
                    Log log1 = new Log();
                    log1.ExamTestResultId = this.examInfo.ExamTestResultId;
                    log1.Name = "testimate";
                    log1.LogUsageType = LogUsageType.Exam;
                    Log log2 = log1;
                    log2.StartDate = await this.GetServerDateTime();
                    logList.Add(log1);
                    await this.SendLogAsync(dataLogs);
                    logList = (List<Log>)null;
                    log2 = (Log)null;
                    log1 = (Log)null;
                    dataLogs = (List<Log>)null;
                    this.isStart = true;
                }
                string filePath = Util.CaptureScreen(this);
                string pathCamera = Util.CaptureWindowWrapper();
                UploadFile uplaodCaptureScreen = await this.UploadImageFileAsync(filePath);
                UploadFile webcam = await this.UploadImageFileAsync(pathCamera);
                //       if (!string.IsNullOrEmpty(pathCamera))
                // webcam = await this.UploadImageFileAsync(pathCamera);
                if (uplaodCaptureScreen != null || webcam != null)
                    await this.SendCaptureScreenAndWebCamAsync(uplaodCaptureScreen, webcam);
                string tempPath = Path.GetTempPath();
                string path1 = Path.Combine(tempPath, "Testimate", "Screenshots");
                string path2 = Path.Combine(tempPath, "Testimate", "CaptureCamera");
                foreach (FileSystemInfo file in new DirectoryInfo(path1).GetFiles())
                    file.Delete();
                foreach (FileSystemInfo file in new DirectoryInfo(path2).GetFiles())
                    file.Delete();
                pathCamera = (string)null;
                uplaodCaptureScreen = (UploadFile)null;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        private async void DispatcherTimerToken_Tick(object sender, EventArgs e)
        {
            MainWindow mainWindow = this;
            try
            {
                if (!mainWindow.hasToken)
                {
                    ExamInfo tokenFromJsAsync = await mainWindow.GetTokenFromJsAsync();
                    mainWindow.examInfo = tokenFromJsAsync;
                }
                else
                {
                    if (MainWindow.dispatcherTimerToken == null)
                        return;
                    MainWindow.dispatcherTimerToken.Tick -= new EventHandler(mainWindow.DispatcherTimerToken_Tick);
                    MainWindow.dispatcherTimerToken.Stop();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        private async void DispatcherTimer_GetActiveWindow(object sender, EventArgs e)
        {
            try
            {
                // Moded return for none activity logging.
                if (true)
                    return;

                if (!this.hasToken)
                    return;
                StringBuilder text = new StringBuilder(256);
                int handle = (int)MainWindow.GetForegroundWindow();
                List<LogApp> logAppList;
                LogApp logApp1;
                LogApp logApp2;

                // Always false for jumping to case of none changing handle
                if (MainWindow.GetWindowText(handle, text, 256) > 0 && false)
                {
                    if (handle == MainWindow.lastHandle && !(text.ToString() != MainWindow.lastTitle) || string.IsNullOrEmpty(text.ToString()))
                        return;
                    MainWindow.lastHandle = handle;
                    MainWindow.lastTitle = text.ToString();
                    logAppList = this.logApps;
                    logApp2 = new LogApp();
                    logApp2.Name = string.Format("{0}", (object)text);
                    logApp1 = logApp2;
                    logApp1.TimeStart = await this.GetServerDateTime();
                    logAppList.Add(logApp2);
                    logAppList = (List<LogApp>)null;
                    logApp1 = (LogApp)null;
                    logApp2 = (LogApp)null;
                }
                else
                {
                    if (handle == MainWindow.lastHandle && !(text.ToString() != MainWindow.lastTitle))
                        return;
                    logAppList = this.logApps;
                    logApp1 = new LogApp();
                    logApp1.Name = "none";
                    logApp2 = logApp1;
                    logApp2.TimeStart = await this.GetServerDateTime();
                    logAppList.Add(logApp1);
                    logAppList = (List<LogApp>)null;
                    logApp2 = (LogApp)null;
                    logApp1 = (LogApp)null;
                    MainWindow.lastHandle = handle;
                    MainWindow.lastTitle = "";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        private async void DispatcherTimer_Sendlog(object sender, EventArgs e)
        {
            try
            {
                if (!this.hasToken)
                    return;
                this.CreateLogFile(this.logApps);
                List<Log> dataLogs = this.ReadLogFiles();
                if (dataLogs.Count <= 0)
                    return;
                await this.SendLogAsync(dataLogs);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        private void GetLocation()
        {
            this.watcher = new GeoCoordinateWatcher(GeoPositionAccuracy.High);
            this.watcher.MovementThreshold = 20.0;
            this.watcher.StatusChanged += (EventHandler<GeoPositionStatusChangedEventArgs>)((sender, args) =>
           {
               if (args.Status != GeoPositionStatus.Ready)
                   return;
               try
               {
                   this.lat = this.watcher.Position.Location.Latitude;
                   this.lng = this.watcher.Position.Location.Longitude;
               }
               finally
               {
                   this.watcher.Stop();
               }
           });
            this.watcher.Start();
        }

        private void CheckApplication()
        {
            Process[] processes = Process.GetProcesses();
            List<LogApp> logAppList = new List<LogApp>();
            foreach (Process process in processes)
            {
                try
                {
                    string fileDescription = FileVersionInfo.GetVersionInfo(process.MainModule.FileName).FileDescription;
                    if (fileDescription == "Google Chrome")
                    {
                        if (!(process.MainWindowHandle == IntPtr.Zero))
                        {
                            foreach (AutomationElement automationElement in AutomationElement.FromHandle(process.MainWindowHandle).FindAll(TreeScope.Descendants, (System.Windows.Automation.Condition)new PropertyCondition(AutomationElement.ControlTypeProperty, (object)ControlType.TabItem)))
                                logAppList.Add(new LogApp()
                                {
                                    Name = fileDescription,
                                    Title = automationElement.Current.Name,
                                    TimeStart = new DateTime?(process.StartTime)
                                });
                        }
                        else
                            continue;
                    }
                    if (fileDescription == "Microsoft Edge")
                    {
                        if (!(process.MainWindowHandle == IntPtr.Zero))
                        {
                            foreach (AutomationElement automationElement in AutomationElement.FromHandle(process.MainWindowHandle).FindAll(TreeScope.Descendants, (System.Windows.Automation.Condition)new PropertyCondition(AutomationElement.ControlTypeProperty, (object)ControlType.TabItem)))
                                logAppList.Add(new LogApp()
                                {
                                    Name = fileDescription,
                                    Title = automationElement.Current.Name
                                });
                        }
                        else
                            continue;
                    }
                    if (fileDescription == "Fire Fox")
                    {
                        if (!(process.MainWindowHandle == IntPtr.Zero))
                        {
                            foreach (AutomationElement automationElement in AutomationElement.FromHandle(process.MainWindowHandle).FindAll(TreeScope.Descendants, (System.Windows.Automation.Condition)new PropertyCondition(AutomationElement.ControlTypeProperty, (object)ControlType.TabItem)))
                                logAppList.Add(new LogApp()
                                {
                                    Name = fileDescription,
                                    Title = automationElement.Current.Name
                                });
                        }
                        else
                            continue;
                    }
                    if (fileDescription == "Safari")
                    {
                        if (!(process.MainWindowHandle == IntPtr.Zero))
                        {
                            foreach (AutomationElement automationElement in AutomationElement.FromHandle(process.MainWindowHandle).FindAll(TreeScope.Descendants, (System.Windows.Automation.Condition)new PropertyCondition(AutomationElement.ControlTypeProperty, (object)ControlType.TabItem)))
                                logAppList.Add(new LogApp()
                                {
                                    Name = fileDescription,
                                    Title = automationElement.Current.Name
                                });
                        }
                    }
                }
                catch (Exception ex)
                {
                }
            }
        }

        private async Task<DateTime?> GetServerDateTime()
        {
            HttpResponseMessage async = await this.CreateApiClient().GetAsync("/api/Exams/CurrentDateTime");
            return !async.IsSuccessStatusCode ? new DateTime?() : new DateTime?(JsonConvert.DeserializeObject<GlobalDatetime>(async.Content.ReadAsStringAsync().Result).CurrentDateTime);
        }

        private async void SetClientKey()
        {
            string str = await this.webView.ExecuteScriptAsync("setClientKey('e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855')");
            if (str != "null")
                return;
            str.ToString();
        }

        private async Task<UploadFile> UploadImageFileAsync(string filePath)
        {
            try
            {
                HttpClient apiClient = this.CreateApiClient();
                StreamContent streamContent = new StreamContent((Stream)File.OpenRead(filePath));
                MultipartFormDataContent multipartFormDataContent1 = new MultipartFormDataContent();
                string fileName = ((IEnumerable<string>)filePath.Split('\\')).LastOrDefault<string>();
                multipartFormDataContent1.Add((HttpContent)streamContent, "file", fileName);
                MultipartFormDataContent multipartFormDataContent2 = multipartFormDataContent1;
                HttpResponseMessage httpResponseMessage = await apiClient.PostAsync("/api/Files/UploadImageFile", (HttpContent)multipartFormDataContent2);
                if (httpResponseMessage.IsSuccessStatusCode)
                    return JsonConvert.DeserializeObject<UploadFile>(await httpResponseMessage.Content.ReadAsStringAsync());
                Console.WriteLine(await httpResponseMessage.Content.ReadAsStringAsync());
                UploadFile uploadFile = new UploadFile();
                return uploadFile;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            return (UploadFile)null;
        }

        private async Task SendCaptureScreenAndWebCamAsync(UploadFile screen, UploadFile webcam)
        {
            try
            {
                HttpClient apiClient = this.CreateApiClient();
                StringContent stringContent1 = new StringContent(JsonConvert.SerializeObject((object)new Capture()
                {
                    UserCapture = webcam,
                    ScreenCapture = screen,
                    Latitude = this.lat,
                    Longitude = this.lng,
                    ExamTestResultId = this.examInfo.ExamTestResultId
                }), Encoding.UTF8, "application/json");
                string requestUri = "/api/Exams/" + this.examInfo.ExamId + "/ExamTestResult/" + this.examInfo.ExamTestResultId + "/Capture";
                StringContent stringContent2 = stringContent1;
                HttpResponseMessage httpResponseMessage = await apiClient.PostAsync(requestUri, (HttpContent)stringContent2);
                if (!httpResponseMessage.IsSuccessStatusCode)
                    throw new Exception(await httpResponseMessage.Content.ReadAsStringAsync());
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        private async Task SendLogAsync(List<Log> dataLogs)
        {
            try
            {
                HttpClient apiClient = this.CreateApiClient();
                if (dataLogs.Count <= 0)
                    return;
                StringContent stringContent = new StringContent(JsonConvert.SerializeObject((object)dataLogs), Encoding.UTF8, "application/json");
                HttpResponseMessage httpResponseMessage = await apiClient.PostAsync("/api/Exams/" + this.examInfo.ExamId + "/ExamTestResult/" + this.examInfo.ExamTestResultId + "/Usage", (HttpContent)stringContent);
                if (!httpResponseMessage.IsSuccessStatusCode)
                    throw new Exception(await httpResponseMessage.Content.ReadAsStringAsync());
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern int GetWindowText(int hWnd, StringBuilder text, int count);

        private void CreateLogFile(List<LogApp> logApps)
        {
            string str = Path.Combine(Path.GetTempPath(), "logs");
            string path2 = "log_" + DateTime.Now.ToString("ddMMyyyyhhmmss") + ".txt";
            string path = Path.Combine(str, path2);
            if (!Directory.Exists(str))
                Directory.CreateDirectory(str);
            if (File.Exists(path))
                return;
            using (StreamWriter text = File.CreateText(path))
            {
                foreach (LogApp logApp in logApps)
                    text.WriteLine(string.Format("{0};{1}", (object)logApp.TimeStart, (object)logApp.Name));
            }
        }

        private async void Window_Closing(object sender, CancelEventArgs e)
        {
            MainWindow mainWindow = this;
            MainWindow.dispatcherTimer.Tick -= new EventHandler(mainWindow.DispatcherTimer_Tick);
            MainWindow.dispatcherTimerLog.Tick -= new EventHandler(mainWindow.DispatcherTimer_GetActiveWindow);
            MainWindow.dispatcherTimerSendLog.Tick -= new EventHandler(mainWindow.DispatcherTimer_Sendlog);
            MainWindow.dispatcherTimerLog.Stop();
            MainWindow.dispatcherTimer.Stop();
            MainWindow.dispatcherTimerSendLog.Stop();
            if (MainWindow.dispatcherTimerToken != null)
            {
                MainWindow.dispatcherTimerToken.Tick -= new EventHandler(mainWindow.DispatcherTimerToken_Tick);
                MainWindow.dispatcherTimerToken.Stop();
            }
            Util.StopCamera();
            if (!mainWindow.hasToken)
                return;
            List<Log> dataLogs = new List<Log>();
            List<Log> logList = dataLogs;
            Log log1 = new Log();
            log1.ExamTestResultId = mainWindow.examInfo.ExamTestResultId;
            log1.Name = "exit-testimate";
            log1.LogUsageType = LogUsageType.Exam;
            Log log2 = log1;
            log2.StartDate = await mainWindow.GetServerDateTime();
            logList.Add(log1);
            await mainWindow.SendLogAsync(dataLogs);
            logList = (List<Log>)null;
            log2 = (Log)null;
            log1 = (Log)null;
            dataLogs = (List<Log>)null;
        }

        private List<Log> ReadLogFiles()
        {
            try
            {
                List<Log> logList = new List<Log>();
                string path = Path.Combine(Path.GetTempPath(), "logs");
                foreach (string str in ((IEnumerable<string>)Directory.GetFiles(path, "*.txt")).Select<string, string>(new Func<string, string>(Path.GetFileName)))
                {
                    foreach (string readAllLine in File.ReadAllLines(path + "/" + str))
                    {
                        Log log = new Log();
                        log.ExamTestResultId = this.examInfo.ExamTestResultId;
                        log.Name = ((IEnumerable<string>)readAllLine.Split(';')).ToList<string>().LastOrDefault<string>();
                        if (!string.IsNullOrEmpty(((IEnumerable<string>)readAllLine.Split(';')).ToList<string>().FirstOrDefault<string>()))
                            log.StartDate = new DateTime?(Convert.ToDateTime(((IEnumerable<string>)readAllLine.Split(';')).ToList<string>().FirstOrDefault<string>()));
                        log.LogUsageType = log.Name != nameof(MainWindow) ? LogUsageType.Program : LogUsageType.Exam;
                        logList.Add(log);
                    }
                }
                foreach (FileSystemInfo file in new DirectoryInfo(path).GetFiles())
                    file.Delete();
                return logList;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return (List<Log>)null;
            }
        }

        public HttpClient CreateApiClient()
        {
            HttpClient httpClient = new HttpClient()
            {
                BaseAddress = new Uri("https://main-api.testimate.app")
            };
            if (this.examInfo != null && !string.IsNullOrEmpty(this.examInfo.Access_Token))
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", this.examInfo.Access_Token);
            return httpClient;
        }

        private void Exit_Click(object sender, RoutedEventArgs e) => Application.Current.Shutdown();

        // LockScreen is originally called by Window_Loaded, but no longer used.
        // Disabling allows user to use Alt+Tab window switching.
        private void LockScreen()
        {
            ProcessModule mainModule = Process.GetCurrentProcess().MainModule;
            this.objKeyboardProcess = new MainWindow.LowLevelKeyboardProc(this.CaptureKey);
            this.ptrHook = MainWindow.SetWindowsHookEx(13, this.objKeyboardProcess, MainWindow.GetModuleHandle(mainModule.ModuleName), 0U);
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(
          int id,
          MainWindow.LowLevelKeyboardProc callback,
          IntPtr hMod,
          uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool UnhookWindowsHookEx(IntPtr hook);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(
          IntPtr hook,
          int nCode,
          IntPtr wp,
          IntPtr lp);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string name);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern short GetAsyncKeyState(Key key);

        private IntPtr CaptureKey(int nCode, IntPtr wp, IntPtr lp)
        {
            if (nCode >= 0)
            {
                MainWindow.KBDLLHOOKSTRUCT structure = (MainWindow.KBDLLHOOKSTRUCT)Marshal.PtrToStructure(lp, typeof(MainWindow.KBDLLHOOKSTRUCT));
                bool flag = this.HasAltModifier(structure.flags);
                if (structure.vkCode == 9 | flag)
                    return (IntPtr)1;
            }
            return MainWindow.CallNextHookEx(this.ptrHook, nCode, wp, lp);
        }

        private bool HasAltModifier(int flags) => (flags & 32) == 32;

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        private struct KBDLLHOOKSTRUCT
        {
            public int vkCode;
            public int scanCode;
            public int flags;
            public int time;
            public IntPtr extra;
        }
    }
}
