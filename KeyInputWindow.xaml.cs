using System.Windows;

namespace TokenMonitorApp
{
    public partial class KeyInputWindow : Window
    {
        public string Key { get; private set; } = string.Empty;

        public KeyInputWindow()
        {
            InitializeComponent();
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            Key = keyBox.Password;
            DialogResult = true;
        }
    }
}

