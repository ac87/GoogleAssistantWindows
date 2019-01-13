using Autofac;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace GoogleAssistantWindows
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static IContainer Container { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            var builder = new ContainerBuilder();
            builder.RegisterType<MainWindow>();
            builder.RegisterType<SettingsWindow>();

            builder.RegisterType<Settings>()
                .OnActivating(settings =>
                    {
                        settings.Instance.Load();
                    })
                .SingleInstance();
            //builder.RegisterType<ICustomerService, CustomerService>();
            //container.RegisterType<IShoppingCartService, ShoppingCartService>();

            Container = builder.Build();

            MainWindow mainWindow = Container.Resolve<MainWindow>();
            mainWindow.Show();
        }
    }
}
