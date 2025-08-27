using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Text.Json;

namespace TokenMonitorApp
{
    public partial class CalculatorWindow : Window
    {
        private readonly HttpClient httpClient = new HttpClient();
        private double solPriceRub = 0.0;
        private double solPriceUsd = 0.0;
        private bool isRubToSol = true;

        private List<string> savedAddresses = new List<string>();

        public CalculatorWindow()
        {
            InitializeComponent();
            _ = FetchSolPrice();
            SavedAddressesList.ItemsSource = savedAddresses;
        }

        private async Task FetchSolPrice()
        {
            try
            {
                var response = await httpClient.GetStringAsync("https://api.coingecko.com/api/v3/simple/price?ids=solana&vs_currencies=usd,rub");
                using var doc = JsonDocument.Parse(response);
                var sol = doc.RootElement.GetProperty("solana");
                solPriceUsd = sol.GetProperty("usd").GetDouble();
                solPriceRub = sol.GetProperty("rub").GetDouble();
            }
            catch
            {
                MessageBox.Show("❌ Не удалось загрузить курс SOL", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ToggleDirection_Click(object sender, RoutedEventArgs e)
        {
            isRubToSol = !isRubToSol;
            ToggleDirectionButton.Content = isRubToSol ? "🔁 Рубли → SOL" : "🔁 SOL → Рубли";
            InputBox_TextChanged(null, null);
        }

        private void InputBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!double.TryParse(InputBox.Text.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out double value)
                || solPriceRub <= 0)
            {
                ResultText.Text = "";
                return;
            }

            if (isRubToSol)
            {
                double sol = value / solPriceRub;
                ResultText.Text = $"≈ {sol:0.####} SOL";
            }
            else
            {
                double rub = value * solPriceRub;
                ResultText.Text = $"≈ {rub:0.##} ₽";
            }
        }

        private void CopyResult_Click(object sender, RoutedEventArgs e)
        {
            var text = ResultText.Text.Replace("≈", "").Trim();
            if (!string.IsNullOrWhiteSpace(text))
            {
                Clipboard.SetText(text);
            }
        }

        private async void CheckBalance_Click(object sender, RoutedEventArgs e)
        {
            var address = WalletAddressBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(address)) return;

            try
            {
                var body = new
                {
                    jsonrpc = "2.0",
                    id = 1,
                    method = "getBalance",
                    @params = new object[] { address }
                };

                var content = new StringContent(System.Text.Json.JsonSerializer.Serialize(body), System.Text.Encoding.UTF8, "application/json");
                var response = await httpClient.PostAsync("https://api.mainnet-beta.solana.com", content);
                using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                var lamports = doc.RootElement.GetProperty("result").GetProperty("value").GetUInt64();
                var sol = lamports / 1_000_000_000.0;

                WalletBalanceResult.Text =
                    $"🟢 Баланс: {sol:0.####} SOL\n" +
                    $"≈ {sol * solPriceUsd:0.##} USD\n" +
                    $"≈ {sol * solPriceRub:0.##} ₽";
            }
            catch
            {
                WalletBalanceResult.Text = "❌ Ошибка при получении баланса";
            }
        }

        private void PinAddress_Click(object sender, RoutedEventArgs e)
        {
            var address = WalletAddressBox.Text.Trim();
            if (!string.IsNullOrWhiteSpace(address) && !savedAddresses.Contains(address))
            {
                savedAddresses.Add(address);
                RefreshSavedAddresses();
            }
        }

        private void RefreshSavedAddresses()
        {
            SavedAddressesList.ItemsSource = null;
            SavedAddressesList.ItemsSource = savedAddresses;
        }

        public void RemoveAddress_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            var address = btn?.Tag?.ToString();
            if (!string.IsNullOrWhiteSpace(address) && savedAddresses.Contains(address))
            {
                savedAddresses.Remove(address);
                RefreshSavedAddresses();
            }
        }
    }
}

