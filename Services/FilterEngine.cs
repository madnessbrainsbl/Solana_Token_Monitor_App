using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace TokenMonitorApp.Services
{
    public class FilterResult
    {
        public bool Passes { get; set; }
        public List<string> Reasons { get; } = new();
        public bool AutoOpenAxiom { get; set; }
    }

    public class FilterEngine
    {
        private readonly ITokenDataProvider _provider;
        private readonly FilterSettings _settings;

        public FilterEngine(ITokenDataProvider provider, FilterSettings settings)
        {
            _provider = provider;
            _settings = settings;
        }

        public async Task<FilterResult> EvaluateAsync(TokenModel token, CancellationToken ct = default)
        {
            var result = new FilterResult { Passes = true };

            // Last filter: optional constraint on dev's last migrated token ATH/MC
            if (_settings.Last.Enabled && _settings.Last.MinMigratedAthUsd > 0)
            {
                // Need recent tokens to determine last migrated one
                // We'll fetch later together with other filters if needed. If none, fetch minimal set here.
            }

            // Dev filter: if enabled and DevAddress is tracked, passes
            if (_settings.Dev.Enabled)
            {
                if (_settings.Dev.Addresses.Count > 0 && _settings.Dev.Addresses.Contains(token.DevAddress))
                {
                    result.Reasons.Add("Dev match");
                    if (_settings.Dev.AutoOpen) result.AutoOpenAxiom = true;
                }
                else
                {
                    // If only Dev filter were enabled, we might fail here; but combine with others using AND semantics where applicable
                }
            }

            // Fetch recent tokens by dev to evaluate MinCap/AvgCap/Migration
            IReadOnlyList<TokenModel> recent = Array.Empty<TokenModel>();
            int n = Math.Max(Math.Max(_settings.MinCap.N, _settings.AvgCap.N), _settings.Migration.N);
            if (_settings.MinCap.Enabled || _settings.AvgCap.Enabled || _settings.Migration.Enabled || (_settings.Last.Enabled && _settings.Last.MinMigratedAthUsd > 0))
            {
                if (!string.IsNullOrWhiteSpace(token.DevAddress))
                {
                    recent = await _provider.GetRecentTokensByDevAsync(token.DevAddress, Math.Clamp(n, 1, 10), ct);
                }
            }

            // MinCap: all of last N tokens must have cap >= MinValue
            if (_settings.MinCap.Enabled)
            {
                var lastN = recent.Take(Math.Clamp(_settings.MinCap.N, 1, 3)).ToList();
                bool ok = lastN.Count > 0 && lastN.All(t => t.MarketCapUsd >= _settings.MinCap.MinValue);
                if (!ok) { result.Passes = false; result.Reasons.Add("MinCap fail"); }
                else { result.Reasons.Add("MinCap ok"); if (_settings.MinCap.AutoOpen) result.AutoOpenAxiom = true; }
            }

            // AvgCap: average of last N tokens >= Value
            if (_settings.AvgCap.Enabled)
            {
                var lastN = recent.Take(Math.Clamp(_settings.AvgCap.N, 1, 3)).ToList();
                bool ok = lastN.Count > 0 && lastN.Average(t => t.MarketCapUsd) >= _settings.AvgCap.Value;
                if (!ok) { result.Passes = false; result.Reasons.Add("AvgCap fail"); }
                else { result.Reasons.Add("AvgCap ok"); if (_settings.AvgCap.AutoOpen) result.AutoOpenAxiom = true; }
            }

            // Migration: percent of last N tokens migrated >= threshold
            if (_settings.Migration.Enabled)
            {
                var lastN = recent.Take(Math.Clamp(_settings.Migration.N, 1, 10)).ToList();
                int migrated = 0;
                foreach (var t in lastN)
                {
                    if (await _provider.IsTokenMigratedAsync(t.TokenAddress, ct)) migrated++;
                }
                int percent = lastN.Count == 0 ? 0 : (int)Math.Round(100.0 * migrated / lastN.Count);
                bool ok = percent >= _settings.Migration.Percent;
                if (!ok) { result.Passes = false; result.Reasons.Add($"Migration {percent}% < {_settings.Migration.Percent}%"); }
                else { result.Reasons.Add($"Migration {percent}% ok"); if (_settings.Migration.AutoOpen) result.AutoOpenAxiom = true; }
            }

            // Last: require last migrated token's ATH/MC >= threshold
            if (_settings.Last.Enabled && _settings.Last.MinMigratedAthUsd > 0)
            {
                // Find the most recent migrated token in 'recent'
                TokenModel? lastMigrated = null;
                foreach (var t in recent)
                {
                    if (await _provider.IsTokenMigratedAsync(t.TokenAddress, ct)) { lastMigrated = t; break; }
                }
                if (lastMigrated == null)
                {
                    result.Passes = false; result.Reasons.Add("Last: no migrated token found");
                }
                else
                {
                    var ath = lastMigrated.MarketCapUsd; // TODO: replace with actual ATH when available
                    bool ok = ath >= _settings.Last.MinMigratedAthUsd;
                    if (!ok) { result.Passes = false; result.Reasons.Add($"Last: migrated ATH ${ath:0} < ${_settings.Last.MinMigratedAthUsd:0}"); }
                    else { result.Reasons.Add("Last: ok"); if (_settings.Last.AutoOpen) result.AutoOpenAxiom = true; }
                }
            }

            return result;
        }
    }
}
