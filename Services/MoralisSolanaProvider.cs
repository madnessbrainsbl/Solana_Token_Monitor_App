using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace TokenMonitorApp.Services
{
    // Scaffold provider that uses Moralis API where applicable
    public class MoralisSolanaProvider : ITokenDataProvider
    {
        private readonly MoralisApiClient _moralis;

        public MoralisSolanaProvider(MoralisApiClient moralis)
        {
            _moralis = moralis;
        }

        public async Task<IReadOnlyList<TokenModel>> GetRecentTokensByDevAsync(string devAddress, int limit, CancellationToken ct = default)
        {
            // Placeholder: This requires indexing of dev-created tokens. Depending on availability, this may
            // come from Moralis streams, on-chain program logs, or a secondary indexer. For now, return empty.
            await Task.CompletedTask;
            return Array.Empty<TokenModel>();
        }

        public async Task<bool> IsTokenMigratedAsync(string tokenAddress, CancellationToken ct = default)
        {
            // Placeholder: Migration heuristic may require reading liquidity events or program-specific states.
            await Task.CompletedTask;
            return false;
        }

        public async Task<TokenModel> EnrichTokenAsync(TokenModel token, CancellationToken ct = default)
        {
            // Example enrichment: try fetch token price to compute market cap if supply known.
            // For Solana SPL tokens, Moralis may expose price endpoints if token is recognized.
            try
            {
                JsonDocument priceDoc = await _moralis.GetAsync($"erc20/{Uri.EscapeDataString(token.TokenAddress)}/price?chain=solana", ct);
                if (priceDoc.RootElement.TryGetProperty("usdPrice", out var usdEl) && usdEl.ValueKind == JsonValueKind.Number)
                {
                    var usdPrice = usdEl.GetDecimal();
                    // If MarketCap already set in SOL, convert to USD if sol price known elsewhere; otherwise keep usdPrice for UI.
                    token.MarketCapUsd = token.MarketCapUsd == 0 ? usdPrice : token.MarketCapUsd;
                }
            }
            catch
            {
                // ignore price failures
            }
            return token;
        }
    }
}
