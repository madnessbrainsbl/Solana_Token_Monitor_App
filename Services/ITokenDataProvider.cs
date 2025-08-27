using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TokenMonitorApp.Services
{
    public interface ITokenDataProvider
    {
        // Returns recent tokens created by the dev (most recent first)
        Task<IReadOnlyList<TokenModel>> GetRecentTokensByDevAsync(string devAddress, int limit, CancellationToken ct = default);

        // Returns true if token is considered migrated from launchpad to DEX/liquidity stage
        Task<bool> IsTokenMigratedAsync(string tokenAddress, CancellationToken ct = default);

        // Enrich current token with USD market cap and other meta if needed
        Task<TokenModel> EnrichTokenAsync(TokenModel token, CancellationToken ct = default);
    }
}
