using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace TokenMonitorApp.Services
{
    /// <summary>
    /// Minimal Moralis Web3 API client.
    ///
    /// If an API key is provided (via constructor or MORALIS_API_KEY env var or local store),
    /// the client sets the X-API-Key header. If no key is available, the client remains usable
    /// for endpoints that do not require authentication (or as a placeholder), without throwing.
    /// </summary>
    public sealed class MoralisApiClient : IDisposable
    {
        private readonly HttpClient _http;
        private bool _disposed;

        public MoralisApiClient(string? apiKey = null, HttpMessageHandler? handler = null)
        {
            var key = apiKey ?? Environment.GetEnvironmentVariable("MORALIS_API_KEY");
            if (string.IsNullOrWhiteSpace(key))
            {
                // fallback to encrypted local store (may be null)
                key = Services.CredentialStore.GetMoralisKey();
            }

            _http = handler is null ? new HttpClient() : new HttpClient(handler, disposeHandler: false);
            // Base URL for Moralis Web3 API v2
            _http.BaseAddress = new Uri("https://deep-index.moralis.io/api/v2/");
            if (!string.IsNullOrWhiteSpace(key))
            {
                _http.DefaultRequestHeaders.Add("X-API-Key", key);
            }
            _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public async Task<JsonDocument> GetAsync(string relativePath, CancellationToken ct = default)
        {
            using var resp = await _http.GetAsync(relativePath, ct).ConfigureAwait(false);
            resp.EnsureSuccessStatusCode();
            var stream = await resp.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
            return await JsonDocument.ParseAsync(stream, cancellationToken: ct).ConfigureAwait(false);
        }

        public async Task<JsonDocument> PostJsonAsync(string relativePath, object payload, CancellationToken ct = default)
        {
            var json = JsonSerializer.Serialize(payload);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            using var resp = await _http.PostAsync(relativePath, content, ct).ConfigureAwait(false);
            resp.EnsureSuccessStatusCode();
            var stream = await resp.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
            return await JsonDocument.ParseAsync(stream, cancellationToken: ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Example: Get native balance for an EVM address on a given chain (e.g., eth, polygon, bsc, etc.).
        /// chain param examples: eth, 0x1, polygon, 0x89, bsc, 0x38
        /// </summary>
        public Task<JsonDocument> GetNativeBalanceAsync(string address, string chain, CancellationToken ct = default)
        {
            // Docs: GET /{address}/balance?chain=xxx
            var path = $"{Uri.EscapeDataString(address)}/balance?chain={Uri.EscapeDataString(chain)}";
            return GetAsync(path, ct);
        }

        /// <summary>
        /// Example: Get token price by token address and chain.
        /// </summary>
        public Task<JsonDocument> GetTokenPriceAsync(string tokenAddress, string chain, CancellationToken ct = default)
        {
            // Docs: GET /erc20/{address}/price?chain=xxx
            var path = $"erc20/{Uri.EscapeDataString(tokenAddress)}/price?chain={Uri.EscapeDataString(chain)}";
            return GetAsync(path, ct);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _http.Dispose();
        }
    }
}
