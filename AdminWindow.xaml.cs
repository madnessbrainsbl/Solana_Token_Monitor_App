using Firebase.Database;
using Firebase.Database.Query;
using System;
using System.Threading.Tasks;
using System.Windows;

namespace TokenMonitorApp
{
    public partial class AdminWindow : Window
    {
        private readonly FirebaseClient _firebase = new FirebaseClient("https://your-firebase-project.firebaseio.com/");

        public AdminWindow()
        {
            InitializeComponent();
        }

        private async void GenerateKeyButton_Click(object sender, RoutedEventArgs e)
        {
            var key = Guid.NewGuid().ToString();
            var access = new UserAccess { Expiry = DateTime.UtcNow.AddDays(7), Revoked = false };
            await _firebase.Child("users").Child(key).PutAsync(access);
            keyOutput.Text = key;
        }

        private async Task RevokeKey(string key)
        {
            await _firebase.Child("users").Child(key).Child("Revoked").PutAsync(true);
        }

        private async void RevokeButton_Click(object sender, RoutedEventArgs e)
        {
            await RevokeKey(revokeKeyBox.Text);
            MessageBox.Show("Отозвано");
        }
    }
}
