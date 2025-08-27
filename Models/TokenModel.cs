namespace TokenMonitorApp
{
    public class TokenModel
    {
        public string TokenAddress { get; set; } = string.Empty;
        public string DevAddress { get; set; } = string.Empty;
        public string Ticker { get; set; } = string.Empty;
        public decimal MarketCap { get; set; }
        public System.DateTime CreationTime { get; set; }
        public string AxiomLink { get; set; } = string.Empty;

        // Extended fields for filters/UI
        public string Chain { get; set; } = "solana"; // default
        public long CreatedUnix { get; set; }
        public decimal MarketCapUsd { get; set; }
        public string Launchpad { get; set; } = ""; // pump.fun or bonk
    }
}

