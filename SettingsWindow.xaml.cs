using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using TokenMonitorApp.Services;

namespace TokenMonitorApp
{
    public partial class SettingsWindow : Window
    {
        private FilterSettings _settings;

        public SettingsWindow()
        {
            InitializeComponent();

            // Moralis key no longer used
            MoralisStatus.Text = "Moralis ключ не используется";

            // Load filter settings
            _settings = SettingsStore.Load();
            PopulateUiFromSettings();
        }

        private void PopulateUiFromSettings()
        {
            // MinCap
            MinCapEnabled.IsChecked = _settings.MinCap.Enabled;
            MinCapN.SelectedIndex = Math.Clamp(_settings.MinCap.N, 1, 3) - 1;
            MinCapValue.Text = _settings.MinCap.MinValue.ToString();
            MinCapAutoOpen.IsChecked = _settings.MinCap.AutoOpen;

            // AvgCap
            AvgCapEnabled.IsChecked = _settings.AvgCap.Enabled;
            AvgCapN.SelectedIndex = Math.Clamp(_settings.AvgCap.N, 1, 3) - 1;
            AvgCapValue.Text = _settings.AvgCap.Value.ToString();
            AvgCapAutoOpen.IsChecked = _settings.AvgCap.AutoOpen;

            // Dev
            DevEnabled.IsChecked = _settings.Dev.Enabled;
            DevList.ItemsSource = _settings.Dev.Addresses.ToList();
            DevAutoOpen.IsChecked = _settings.Dev.AutoOpen;

            // Migration
            MigEnabled.IsChecked = _settings.Migration.Enabled;
            MigN.SelectedIndex = Math.Clamp(_settings.Migration.N, 1, 10) - 1;
            MigPercent.Text = _settings.Migration.Percent.ToString();
            MigAutoOpen.IsChecked = _settings.Migration.AutoOpen;

            // Last
            LastEnabled.IsChecked = _settings.Last.Enabled;
            LastMinAthValue.Text = _settings.Last.MinMigratedAthUsd.ToString();
            LastAutoOpen.IsChecked = _settings.Last.AutoOpen;
        }

        private void SaveUiToSettings()
        {
            // MinCap
            _settings.MinCap.Enabled = MinCapEnabled.IsChecked == true;
            _settings.MinCap.N = (MinCapN.SelectedIndex >= 0 ? MinCapN.SelectedIndex + 1 : 1);
            if (decimal.TryParse(MinCapValue.Text, out var minCap)) _settings.MinCap.MinValue = minCap;
            _settings.MinCap.AutoOpen = MinCapAutoOpen.IsChecked == true;

            // AvgCap
            _settings.AvgCap.Enabled = AvgCapEnabled.IsChecked == true;
            _settings.AvgCap.N = (AvgCapN.SelectedIndex >= 0 ? AvgCapN.SelectedIndex + 1 : 1);
            if (decimal.TryParse(AvgCapValue.Text, out var avgCap)) _settings.AvgCap.Value = avgCap;
            _settings.AvgCap.AutoOpen = AvgCapAutoOpen.IsChecked == true;

            // Dev
            _settings.Dev.Enabled = DevEnabled.IsChecked == true;
            _settings.Dev.Addresses = DevList.Items.Cast<object>().Select(x => x.ToString() ?? string.Empty).Where(s => !string.IsNullOrWhiteSpace(s)).ToHashSet();
            _settings.Dev.AutoOpen = DevAutoOpen.IsChecked == true;

            // Migration
            _settings.Migration.Enabled = MigEnabled.IsChecked == true;
            _settings.Migration.N = (MigN.SelectedIndex >= 0 ? MigN.SelectedIndex + 1 : 1);
            if (int.TryParse(MigPercent.Text, out var migPct)) _settings.Migration.Percent = Math.Clamp(migPct, 0, 100);
            _settings.Migration.AutoOpen = MigAutoOpen.IsChecked == true;

            // Last
            _settings.Last.Enabled = LastEnabled.IsChecked == true;
            if (decimal.TryParse(LastMinAthValue.Text, out var lastMinAth)) _settings.Last.MinMigratedAthUsd = lastMinAth;
            _settings.Last.AutoOpen = LastAutoOpen.IsChecked == true;
        }

        private void SaveMoralisKey_Click(object sender, RoutedEventArgs e)
        {
            // Moralis key storage disabled
            MoralisKeyBox.Password = string.Empty;
            MoralisStatus.Text = "Moralis ключ не используется";
            MoralisStatus.Foreground = System.Windows.Media.Brushes.Gray;
        }

        private void ClearMoralisKey_Click(object sender, RoutedEventArgs e)
        {
            // Moralis key storage disabled
            MoralisStatus.Text = "Moralis ключ не используется";
            MoralisStatus.Foreground = System.Windows.Media.Brushes.Gray;
        }

        private void SaveFilters_Click(object sender, RoutedEventArgs e)
        {
            SaveUiToSettings();
            SettingsStore.Save(_settings);
            MessageBox.Show("Фильтры сохранены", "Settings", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void AddDev_Click(object sender, RoutedEventArgs e)
        {
            var w = new DevAddressWindow { Owner = this };
            if (w.ShowDialog() == true)
            {
                if (!string.IsNullOrWhiteSpace(w.Address))
                {
                    DevList.Items.Add(w.Address.Trim());
                }
            }
        }

        private void RemoveDev_Click(object sender, RoutedEventArgs e)
        {
            if (DevList.SelectedItem != null)
            {
                DevList.Items.Remove(DevList.SelectedItem);
            }
        }
    }
}
