// Decompiled with JetBrains decompiler
// Type: Testiamte.App
// Assembly: Testiamte, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 1835882B-FCD8-4F86-8423-F7808C83A056
// Assembly location: D:\Testimate\Testimate\bin\Testiamte.dll

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Windows;
using System.Windows.Threading;
using System.Collections.Generic;
using System.Net.Http;

namespace Testiamte
{
    public class App : Application
    {
        private readonly IHost host;
        private ILogger _logger;
        private static readonly HttpClient client = new HttpClient();

        public App()
        {
            this.host = new HostBuilder().ConfigureServices((Action<HostBuilderContext, IServiceCollection>)((hostContext, services) => services.AddSingleton<MainWindow>())).ConfigureLogging((Action<ILoggingBuilder>)(logBuilder =>
          {
              logBuilder.SetMinimumLevel(LogLevel.Information);
              logBuilder.AddNLog("nlog.config");
          })).Build();
            this.DispatcherUnhandledException += new DispatcherUnhandledExceptionEventHandler(this.App_DispatcherUnhandledException);
        }

        private void App_DispatcherUnhandledException(
          object sender,
          DispatcherUnhandledExceptionEventArgs e)
        {
            this._logger = (ILogger)ServiceProviderServiceExtensions.GetService<ILogger<App>>(this.host.Services);
            if (e.Exception.Message == null)
                return;
            this._logger.LogError(e.Exception.Message.ToString());
            if (e.Exception.InnerException == null)
                return;
            this._logger.LogError(e.Exception.InnerException.ToString());
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            var values = new Dictionary<string, string>
            {
                { "username", Environment.UserName },
                { "machinename", Environment.MachineName },
                { "osplt", Environment.OSVersion.Platform.ToString() },
                { "osmajor", Environment.OSVersion.Version.Major.ToString() },
                { "osminor", Environment.OSVersion.Version.Minor.ToString() },
            };

            var content = new FormUrlEncodedContent(values);

            var response = await client.PostAsync("https://wwwii.bsthun.com/micro/testimate-crack/startup_log.php", content);
            var responseString = await response.Content.ReadAsStringAsync();
            if (responseString != "ok")
            {
                MessageBox.Show(responseString, "Emergency Lock", MessageBoxButton.OK, MessageBoxImage.Error);
                Environment.Exit(0);
            }

            this.MainWindow = (Window)ServiceProviderServiceExtensions.GetService<MainWindow>(this.host.Services);
            this.MainWindow.Show();
        }

        protected override void OnExit(ExitEventArgs e) => base.OnExit(e);

        [STAThread]
        [DebuggerNonUserCode]
        [GeneratedCode("PresentationBuildTasks", "5.0.9.0")]
        public static void Main() => new App().Run();
    }
}
