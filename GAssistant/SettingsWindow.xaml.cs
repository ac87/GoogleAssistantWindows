using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace GoogleAssistantWindows
{
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        private readonly Settings settings;

        public SettingsWindow(Settings settings)
        {
            InitializeComponent();
            this.settings = settings;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            clientIdTextBox.Text = settings.ClientId;
            deviceIdTextBox.Text = settings.DeviceId;
            deviceModelIdTextBox.Text = settings.DeviceModelId;
            languageCodeComboBox.Text = settings.LanguageCode;
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            settings.ClientId = clientIdTextBox.Text;
            settings.DeviceId = deviceIdTextBox.Text;
            settings.DeviceModelId = deviceModelIdTextBox.Text;
            settings.LanguageCode = languageCodeComboBox.Text;

            settings.Save();
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void OnSelectFileClick(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            bool? result = dialog.ShowDialog(this);

            if(result == true)
            {
                string fileName = dialog.FileName;
                clientIdTextBox.Text = File.ReadAllText(fileName);
            }
        }
    }
}
