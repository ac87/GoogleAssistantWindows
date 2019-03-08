using Autofac;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace GoogleAssistantWindows
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        public static IContainer Container { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            Application.Current.DispatcherUnhandledException += OnUnhandledException;

            var builder = new ContainerBuilder();
            builder.RegisterType<MainWindow>();
            builder.RegisterType<SettingsWindow>();
            builder.RegisterType<WelcomeWindow>();

            builder.RegisterType<Assistant>()
                .SingleInstance();
            builder.RegisterType<UserManager>()
                .SingleInstance();
            builder.RegisterType<DeviceRegistration>();

            builder.RegisterType<Settings>()
                .OnActivating(settings =>
                    {
                        settings.Instance.Load();
                    })
                .SingleInstance();

            Container = builder.Build();

            MainWindow mainWindow = Container.Resolve<MainWindow>();
            mainWindow.Show();
        }

        void OnUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            string typeName = GetTypeName(e.Exception);
            if(typeName != null)
                logger.Error("Unhandled Exception at \"{0}\": {1}", typeName, e.Exception.Message);
            else
                logger.Error("Unhandled Exception: {0}", e.Exception.Message);
        }

        private string GetTypeName(Exception ex)
        {
            try
            {
                StackTrace stackTrace = new StackTrace(ex);
                return stackTrace.GetFrame(0).GetMethod().DeclaringType.FullName;
            }
            catch
            {
                return null;
            }
        }
    }
}
