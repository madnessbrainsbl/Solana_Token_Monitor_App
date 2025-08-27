using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace TokenMonitorApp
{
    public class DataService
    {
        private readonly GraphQLHttpClient _client;
        private DateTime _startTime;
        private int _processed = 0;
        private int _shown = 0;
        private readonly FilterSettings _filters;

        public event Action<TokenModel> NewTokenAdded;
        public event Action<int, int> CountersUpdated;

        public DataService(string apiKey, FilterSettings filters)
        {
            _client = new GraphQLHttpClient("https://graphql.bitquery.io/", new NewtonsoftJsonSerializer());
            _client.HttpClient.DefaultRequestHeaders.Add("X-API-KEY", apiKey);
            _filters = filters;
        }

        public async Task StartMonitoring()
        {
            _startTime = DateTime.UtcNow;
            await Task.CompletedTask; // placeholder: attach subscriptions via Bitquery when ready
        }

        private void ProcessToken(TokenModel token)
        {
            // Placeholder filtering logic until real data is wired
            bool passes = true;
            if (_filters.MinCap.Enabled) { passes = passes && (token.MarketCap >= _filters.MinCap.MinValue); }
            if (_filters.AvgCap.Enabled) { /* compute avg against history once available */ }
            if (_filters.Dev.Enabled && _filters.Dev.Addresses.Contains(token.DevAddress)) { passes = true; }
            if (_filters.Migration.Enabled) { /* compute migration % */ }
            // Last filter now uses MinMigratedAthUsd and is evaluated in FilterEngine; no-op here to keep placeholder compiling

            if (passes)
            {
                _shown++;
                token.AxiomLink = $"https://axiom.trade/meme/{token.TokenAddress}";
                NewTokenAdded?.Invoke(token);

                if (_filters.AutoOpenAxiom)
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = token.AxiomLink, UseShellExecute = true });
                }
            }

            CountersUpdated?.Invoke(_processed, _shown);
        }

        public async Task<decimal> GetSolanaPrice()
        {
            using var http = new HttpClient();
            var _ = await http.GetStringAsync("https://api.coingecko.com/api/v3/simple/price?ids=solana&vs_currencies=usd");
            return 150m;
        }
    }
}
