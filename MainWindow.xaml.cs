using Autofac;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
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

        private readonly KeyboardHook _hook;

        private readonly NotifyIcon _notifyIcon;

        private readonly AudioOut _audioOut;

        private AssistantState _assistantState = AssistantState.Inactive;

        private ObservableCollection<DialogResult> dialogResults;

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
            _notifyIcon.Text = Title;            
            _notifyIcon.DoubleClick +=
                delegate
                {
                    _notifyIcon.Visible = false;
                    Show();
                    WindowState = WindowState.Normal;
                };

            _assistant = new Assistant();
            _assistant.OnAssistantStateChanged += OnAssistantStateChanged;
            _assistant.OnAssistantDialogResult += OnAssistantDialogResult;
            _assistant.OnAssistantSpeechResult += OnAssistantSpeechResult;

            _userManager = UserManager.Instance;
            _userManager.OnUserUpdate += OnUserUpdate;

            dialogResults = new ObservableCollection<DialogResult>();
            DialogBox.ItemsSource = dialogResults;
        }

        private void OnAssistantSpeechResult(string message)
        {
            dialogResults.Add(new DialogResult() { Actor = DialogResultActor.User, Message = message });
        }

        private void OnAssistantDialogResult(string message)
        {
            dialogResults.Add(new DialogResult() { Actor = DialogResultActor.GoogleAssistant, Message = message });
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

        private childItem FindVisualChild<childItem>(DependencyObject obj) where childItem : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(obj, i);
                if (child != null && child is childItem)
                {
                    return (childItem)child;
                }
                else
                {
                    childItem childOfChild = FindVisualChild <childItem>(child);
                    if (childOfChild != null)
                    {
                        return childOfChild;
                    }
                }
            }
            return null;
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
    }
}
