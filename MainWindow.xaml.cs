using System;
using System.Windows;
using System.Windows.Forms;

namespace GoogleAssistantWindows
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const int NormalHeight = 100;
        private const int DebugHeight = 350;

        private readonly UserManager _userManager;
        private readonly Assistant _assistant;

        private readonly KeyboardHook _hook;

        private readonly NotifyIcon _notifyIcon;

        private readonly AudioOut _audioOut;

        public MainWindow()
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
            _notifyIcon.Icon = new System.Drawing.Icon("Mic.ico");
            _notifyIcon.Text = "Google Assistant Windows";            
            _notifyIcon.DoubleClick +=
                delegate
                {
                    _notifyIcon.Visible = false;
                    Show();
                    WindowState = WindowState.Normal;
                };

            _assistant = new Assistant();
            _assistant.OnDebug += Output;
            _assistant.OnStoppedListening += OnStoppedListening;

            _userManager = UserManager.Instance;
            _userManager.OnUserUpdate += OnUserUpdate;
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

        private void OnStoppedListening()
        {
            if (ButtonRecord.Dispatcher.CheckAccess())
                ButtonRecord.Content = "Press";
            else
                ButtonRecord.Dispatcher.BeginInvoke(new Action(OnStoppedListening));
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
            StartListening();
        }

        private void StartListening()
        {
            if (_assistant.IsInitialised())
            {
                _assistant.NewConversation();                
                ButtonRecord.Content = "Listening...";
                _audioOut.PlayNotification();
            }
        }

        public void Output(string output)
        {
            if (ListBoxOutput.Dispatcher.CheckAccess())
            {
                // stop using memory for old debug lines.
                if (ListBoxOutput.Items.Count > 500)
                    ListBoxOutput.Items.RemoveAt(0);

                ListBoxOutput.Items.Add(output);
                ListBoxOutput.ScrollIntoView(ListBoxOutput.Items[ListBoxOutput.Items.Count-1]);

                if (output.StartsWith("Error") && Height == NormalHeight)
                    Height = DebugHeight;
            }
            else
                ListBoxOutput.Dispatcher.BeginInvoke(new Action(() => Output(output)));
        }

        private void DebugButton_OnClick(object sender, RoutedEventArgs e)
        {
            Height = (Height == NormalHeight ? DebugHeight : NormalHeight);
        }
    }
}
