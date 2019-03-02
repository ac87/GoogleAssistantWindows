using Autofac;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Forms;
using System.Windows.Media;

namespace GoogleAssistantWindows
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const int NormalHeight = 368;
        private const int DebugHeight = 618;

        private readonly UserManager _userManager;
        private readonly Assistant _assistant;
        private readonly Settings settings;
        private readonly DeviceRegistration deviceRegistration;

        private readonly KeyboardHook _hook;

        private readonly NotifyIcon _notifyIcon;

        private readonly AudioOut _audioOut;

        private AssistantState _assistantState = AssistantState.Inactive;

        private ObservableCollection<DialogResult> dialogResults;

        public MainWindow(Assistant assistant, UserManager userManager, Settings settings, DeviceRegistration deviceRegistration)
        {
            InitializeComponent();

            _audioOut = new AudioOut();          

            _hook = new KeyboardHook();
            _hook.KeyDown += OnHookKeyDown;
            void OnHookKeyDown(object sender, HookEventArgs e)
            {
                // Global keyboard hook for Ctrl+Alt+G to start listening.
                if (e.Control && e.Alt && e.Key == Keys.G)
                    StartListening();                
            }

            // When minimized it will hide in the tray. but the global keyboard hook should still work
            _notifyIcon = new NotifyIcon();
            _notifyIcon.Icon = ReadIcon("mic.ico");
            _notifyIcon.Text = Title;            
            _notifyIcon.DoubleClick +=
                delegate
                {
                    _notifyIcon.Visible = false;
                    Show();
                    WindowState = WindowState.Normal;
                };

            this._assistant = assistant;
            this._assistant.OnAssistantStateChanged += OnAssistantStateChanged;
            this._assistant.OnAssistantDialogResult += OnAssistantDialogResult;
            this._assistant.OnAssistantSpeechResult += OnAssistantSpeechResult;

            this._userManager = userManager;
            this._userManager.OnUserUpdate += OnUserUpdate;

            this.deviceRegistration = deviceRegistration;

            this.settings = settings;

            dialogResults = new ObservableCollection<DialogResult>();
            DialogBox.ItemsSource = dialogResults;
        }

        protected override void OnStateChanged(EventArgs e)
        {
            if (WindowState == WindowState.Minimized)
            {
                _notifyIcon.Visible = true;
                Hide();
            }
            base.OnStateChanged(e);
        }

        private void OnAssistantSpeechResult(string message)
        {
            dialogResults.Add(new DialogResult() { Actor = DialogResultActor.User, Message = message });
        }

        private void OnAssistantDialogResult(string message)
        {
            dialogResults.Add(new DialogResult() { Actor = DialogResultActor.GoogleAssistant, Message = message });
        }
               
        private void OnAssistantStateChanged(AssistantState state)
        {
            _assistantState = state;
            UpdateButtonText(state);
        }

        private void UpdateButtonText(AssistantState state)
        {
            if (ButtonRecord.Dispatcher.CheckAccess())
            {
                switch(state)
                {
                    case AssistantState.Listening:
                        ButtonRecordIcon.Text = "\xF12E";
                        ButtonRecordText.Text = state.ToString();
                        break;
                    case AssistantState.Processing:
                        ButtonRecordIcon.Text = "\xE9F5";
                        ButtonRecordText.Text = state.ToString();
                        break;
                    case AssistantState.Speaking:
                        ButtonRecordIcon.Text = "\xF5B0";
                        ButtonRecordText.Text = state.ToString();
                        break;
                    case AssistantState.Inactive:
                        ButtonRecordIcon.Text = "\xE720";
                        ButtonRecordText.Text = "Press";
                        break;
                }
            }
            else
                ButtonRecord.Dispatcher.BeginInvoke(new Action(() => UpdateButtonText(state)));
        }

        private async void OnUserUpdate(UserManager.GoogleUserData userData)
        {
            ButtonRecord.IsEnabled = false;
            _assistant.Shutdown();
            if (userData != null)
            {
                _assistant.InitAssistantForUser(_userManager.GetChannelCredential());
                ButtonRecord.IsEnabled = true;
            }

            if(string.IsNullOrEmpty(settings.DeviceModelId))
            {
                string deviceModelId = await deviceRegistration.RegisterDeviceModel(settings.ProjectId, _userManager.Credential);
                string deviceId = await deviceRegistration.RegisterDeviceInstance(settings.ProjectId, _userManager.Credential, deviceModelId);

                settings.DeviceModelId = deviceModelId;
                settings.DeviceId = deviceId;
            }

            if (string.IsNullOrEmpty(settings.DeviceId))
            {
                string deviceId = await deviceRegistration.RegisterDeviceInstance(settings.ProjectId, _userManager.Credential, settings.DeviceModelId);
                settings.DeviceId = deviceId;
            }

            settings.Save();
        }

        private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            if (Utils.HasTokenFile()) 
                _userManager.GetOrRefreshCredential();     // we don't need to wait for this UserManager will throw an event on loaded.  

            if (settings.ShowWelcome)
            {
                WelcomeWindow dialog = App.Container.Resolve<WelcomeWindow>();
                dialog.Owner = this;
                dialog.ShowDialog();
            }
        }

        private void ButtonRecord_OnClick(object sender, RoutedEventArgs e)
        {
            StartListening();
        }

        private void StartListening()
        {
            if (_assistant.IsInitialised() && _assistantState == AssistantState.Inactive)
            {
                _assistant.NewConversation();          
                _audioOut.PlayNotification();
            }
        }

        private void DebugButton_OnClick(object sender, RoutedEventArgs e)
        {
            Height = (Height == NormalHeight ? DebugHeight : NormalHeight);
        }

        private void OnProjectWebsiteClick(object sender, RoutedEventArgs e)
        {
            Process.Start("https://github.com/pieterderycke/GoogleAssistantWindows");
        }

        private void OnSettingsClick(object sender, RoutedEventArgs e)
        {
            SettingsWindow dialog = App.Container.Resolve<SettingsWindow>();
            dialog.Owner = this;
            dialog.ShowDialog();
        }

        private void OnLogFileClick(object sender, RoutedEventArgs e)
        {
            Process.Start(Utils.GetDataStoreFolder() + "log.txt");
        }

        private Icon ReadIcon(string fileName)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "GoogleAssistantWindows." + fileName;

            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            {
                return new System.Drawing.Icon(stream);
            }
        }
    }
}
