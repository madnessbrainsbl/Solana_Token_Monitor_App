using System.Windows;

namespace TokenMonitorApp
{
    public partial class DevAddressWindow : Window
    {
        public string Address { get; private set; } = string.Empty;
        public DevAddressWindow()
        {
            InitializeComponent();
        }
        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            Address = AddressBox.Text;
            DialogResult = true;
        }
    }
}
