using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Google.Assistant.Embedded.V1Alpha1;
using Google.Protobuf;
using Grpc.Core;
using NAudio.Wave;
using Timer = System.Timers.Timer;

namespace GoogleAssistantWindows
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {       
        private readonly UserManager _userManager;
        private readonly Assistant _assistant;

        public MainWindow()
        {
            InitializeComponent();

            _assistant = new Assistant();
            _assistant.OnDebug += Output;

            _userManager = UserManager.Instance;
            _userManager.OnUserUpdate += OnUserUpdate;
        }

        private void OnUserUpdate(UserManager.GoogleUserData userData)
        {
            ButtonRecord.IsEnabled = false;
            _assistant.Shutdown();
            if (userData != null)
            {
                _assistant.InitAssistantForUser(_userManager.GetChannelCredential());
                ButtonRecord.IsEnabled = true;
            }
        }

        private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            if (Utils.HasTokenFile()) 
                _userManager.GetOrRefreshCredential();     // we don't need to wait for this UserManager will throw an event on loaded.       
        }
       
        private void ButtonRecord_OnClick(object sender, RoutedEventArgs e)
        {
            _assistant.NewConversation();
        }
                      
        public void Output(string output)
        {
            if (TextBoxOutput.Dispatcher.CheckAccess())
            {
                TextBoxOutput.Text = TextBoxOutput.Text + output + Environment.NewLine;
                Console.WriteLine(output);
            }
            else
                TextBoxOutput.Dispatcher.BeginInvoke(new Action(() => Output(output)));
        }        
    }
}
