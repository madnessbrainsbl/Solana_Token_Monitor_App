using System;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Text.Json;

namespace TokenMonitorApp
{
    public partial class MainWindow : Window
    {
        private ClientWebSocket webSocket;
        private CancellationTokenSource cts;
        private long startUnixTime;
        private double solPrice = 0.0;
        private readonly HttpClient httpClient = new HttpClient();
        private readonly System.Timers.Timer solPriceTimer = new System.Timers.Timer(60000);

        private int totalChecked = 0;
        private int resultCount = 0;

        private FilterSettings currentFilters = Services.SettingsStore.Load();

        public MainWindow()
        {
            InitializeComponent();
            _ = FetchSolPrice();
            solPriceTimer.Elapsed += async (s, e) => await FetchSolPrice();
            solPriceTimer.Start();
        }

        private async Task FetchSolPrice()
        {
            try
            {
                var response = await httpClient.GetStringAsync("https://api.coingecko.com/api/v3/simple/price?ids=solana&vs_currencies=usd");
                using var doc = JsonDocument.Parse(response);
                solPrice = doc.RootElement.GetProperty("solana").GetProperty("usd").GetDouble();
                Dispatcher.Invoke(() => { SolPriceLabel.Text = $"💲 SOL: ${solPrice:0.00}"; });
            }
            catch { }
        }

        private string FormatNumber(double number)
        {
            if (number >= 1_000_000) return $"{number / 1_000_000:0.#}M";
            if (number >= 1_000) return $"{number / 1_000:0.#}k";
            return number.ToString("0");
        }

private async Task ConnectToWebSocket()
{
    webSocket = new ClientWebSocket();
    cts = new CancellationTokenSource();
    startUnixTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

    // Prepare filter engine with persisted settings
    var filters = currentFilters; // snapshot
    using var moralisClient = new Services.MoralisApiClient();
    Services.ITokenDataProvider provider = new Services.MoralisSolanaProvider(moralisClient);
    var engine = new Services.FilterEngine(provider, filters);

    try
    {
        await webSocket.ConnectAsync(new Uri("wss://pumpportal.fun/api/data"), cts.Token);
        var subscribeJson = "{\"method\": \"subscribeNewToken\"}";
        var subscribeBytes = Encoding.UTF8.GetBytes(subscribeJson);
        await webSocket.SendAsync(new ArraySegment<byte>(subscribeBytes), WebSocketMessageType.Text, true, cts.Token);

        Dispatcher.Invoke(() =>
        {
            StatusLabel.Content = "STATUS: Соединение установлено ✅";
            StatusLabel.Foreground = new SolidColorBrush(Color.FromRgb(144, 238, 144));
        });

        var buffer = new byte[8192];
        while (webSocket.State == WebSocketState.Open && !cts.Token.IsCancellationRequested)
        {
            var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cts.Token);
            var message = Encoding.UTF8.GetString(buffer, 0, result.Count);

            try
            {
                using var doc = JsonDocument.Parse(message);
                var root = doc.RootElement;
                var symbol = root.TryGetProperty("symbol", out var s) ? s.GetString() : "???";
                double capSol = 0.0;
                if (root.TryGetProperty("marketCapSol", out var m))
                {
                    if (m.ValueKind == JsonValueKind.Number) capSol = m.GetDouble();
                    else if (double.TryParse(m.GetString(), out var d)) capSol = d;
                }
                var capUsd = capSol * solPrice;
                var ca = root.TryGetProperty("mint", out var mint) ? mint.GetString() : "???";
                long? createdUnix = root.TryGetProperty("createdTime", out var ctEl) && ctEl.ValueKind==JsonValueKind.Number ? ctEl.GetInt64() : null;
                if (createdUnix.HasValue && createdUnix.Value < startUnixTime) continue;

                var token = new TokenModel
                {
                    TokenAddress = ca ?? string.Empty,
                    DevAddress = root.TryGetProperty("creator", out var dev) ? dev.GetString() ?? string.Empty : string.Empty,
                    Ticker = symbol ?? string.Empty,
                    MarketCap = (decimal)capSol, // in SOL units for now
                    MarketCapUsd = (decimal)capUsd,
                    CreationTime = createdUnix.HasValue ? DateTimeOffset.FromUnixTimeSeconds(createdUnix.Value).UtcDateTime : DateTime.UtcNow,
                    CreatedUnix = createdUnix ?? 0,
                    Chain = "solana",
                    Launchpad = root.TryGetProperty("source", out var src) ? (src.GetString() ?? "") : ""
                };

                // Enrich and filter
                token = await provider.EnrichTokenAsync(token, cts.Token);
                var eval = await engine.EvaluateAsync(token, cts.Token);

                Dispatcher.Invoke(() =>
                {
                    totalChecked++;
                    TotalCheckedLabel.Text = $"Total checked: {totalChecked}";
                });

                if (!eval.Passes) continue;

                Dispatcher.Invoke(() =>
                {
                    double secondsAgo = createdUnix.HasValue ? (DateTimeOffset.UtcNow - DateTimeOffset.FromUnixTimeSeconds(createdUnix.Value)).TotalSeconds : 0;
                    var panel = new Border
                    {
                        Margin = new Thickness(0, 0, 0, 10),
                        Padding = new Thickness(10),
                        Background = Brushes.WhiteSmoke,
                        BorderBrush = Brushes.LightGray,
                        BorderThickness = new Thickness(1),
                        CornerRadius = new CornerRadius(8),
                        Child = new StackPanel()
                    };
                    var stack = (StackPanel)panel.Child;
                    stack.Children.Add(new TextBlock { Text = $"$ {token.Ticker}", FontSize = 14, FontWeight = FontWeights.Bold });
                    stack.Children.Add(new TextBlock { Text = $"💰 MC: ${FormatNumber((double)token.MarketCapUsd)}", FontSize = 13 });
                    stack.Children.Add(new TextBlock { Text = $"⏱ {secondsAgo:0} sec ago", FontSize = 13 });
                    var caText = new TextBlock { Text = $"🔗 CA: {token.TokenAddress}", FontSize = 13, Cursor = System.Windows.Input.Cursors.Hand, TextDecorations = TextDecorations.Underline, Foreground = Brushes.DarkBlue, ToolTip = "Copy" };
                    caText.MouseLeftButtonUp += (s2, e2) => { Clipboard.SetText(token.TokenAddress ?? ""); StatusLabel.Content = "✅ STATUS: CA copied"; StatusLabel.Foreground = Brushes.Green; };
                    stack.Children.Add(caText);

                    TokensPanel.Children.Insert(0, panel);
                    resultCount++;
                    ResultLabel.Text = $"Result: {resultCount}";
                });

                if (eval.AutoOpenAxiom)
                {
                    var link = $"https://axiom.trade/meme/{token.TokenAddress}";
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = link, UseShellExecute = true });
                }
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                {
                    StatusLabel.Content = $"[Ошибка обработки]: {ex.Message}";
                    StatusLabel.Foreground = Brushes.OrangeRed;
                });
            }
        }
    }
    catch (Exception ex)
    {
        Dispatcher.Invoke(() => { StatusLabel.Content = $"[ERROR]: {ex.Message}"; StatusLabel.Foreground = Brushes.Red; });
    }
}

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            StartButton.IsEnabled = false;
            StopButton.IsEnabled = true;
            TokensPanel.Children.Clear();
            totalChecked = 0;
            resultCount = 0;
            TotalCheckedLabel.Text = "Total checked: 0";
            ResultLabel.Text = "Result: 0";
            StatusLabel.Content = "STATUS: ⏳ Ожидаем новые токены...";
            StatusLabel.Foreground = Brushes.DarkGoldenrod;
            _ = ConnectToWebSocket();
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            if (webSocket != null && webSocket.State == WebSocketState.Open)
            {
                cts.Cancel();
                webSocket.Abort();
                webSocket.Dispose();
                webSocket = null;
                StatusLabel.Content = "STATUS: Парсинг остановлен ⛔";
                StatusLabel.Foreground = Brushes.Gray;
            }
            StartButton.IsEnabled = true;
            StopButton.IsEnabled = false;
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var settingsWindow = new SettingsWindow { Owner = this };
            settingsWindow.ShowDialog();
            // Reload settings after closing
            currentFilters = Services.SettingsStore.Load();
        }

        private void OpenCalculator_Click(object sender, RoutedEventArgs e)
        {
            var calculator = new CalculatorWindow { Owner = this };
            calculator.ShowDialog();
        }

        private void ClearTokens_Click(object sender, RoutedEventArgs e)
        {
            TokensPanel.Children.Clear();
        }
    }
}
