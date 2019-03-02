using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    /// Interaction logic for WelcomeWindow.xaml
    /// </summary>
    public partial class WelcomeWindow : Window
    {
        private readonly Settings settings;

        public WelcomeWindow(Settings settings)
        {
            InitializeComponent();

            this.settings = settings;

            this.DataContext = settings;
    
            viewer.Markdown = File.ReadAllText("GettingStarted.md");
        }

        private void OnHyperlinkClick(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            Process.Start(e.Parameter.ToString());
        }

        private void OnWindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            settings.Save();
        }
    }
}
