using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace GoogleAssistantWindows
{
    /// <summary>
    /// Interaction logic for GoogleAccountControl.xaml
    /// </summary>
    public partial class GoogleAccountControl : UserControl
    {
        private readonly UserManager _userManager;

        public GoogleAccountControl()
        {
            InitializeComponent();
            _userManager = UserManager.Instance;
            _userManager.OnUserUpdate += OnUserUpdate;
        }

        private void OnUserUpdate(UserManager.GoogleUserData userdata)
        {
            UpdateProfile(userdata);
        }

        private void UpdateProfile(UserManager.GoogleUserData userData)
        {
            if (TextBlockName.Dispatcher.CheckAccess())
            {
                GridSignedIn.Visibility = userData != null ? Visibility.Visible : Visibility.Hidden;
                ImageGoogleSignIn.Visibility = userData == null ? Visibility.Visible : Visibility.Hidden;
               
                if (userData != null)
                {
                    TextBlockName.Text = userData.name;
                    ImageAvatar.Source = new BitmapImage(new Uri(Utils.GetDataStoreFolder() + userData.id + ".png"));
                }
            }
            else
            {
                TextBlockName.Dispatcher.BeginInvoke(new Action(() => UpdateProfile(userData)));
            }
        }

        private void ImageGoogleSignIn_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _userManager.GetOrRefreshCredential();
        }

        private void TextBlockSignOut_OnPreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            SignOut();
        }

        private void SignOut()
        {
            UpdateProfile(null);
            _userManager.SignOut();
        }
    }
}
