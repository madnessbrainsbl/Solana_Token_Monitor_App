using System.Collections.Generic;

namespace TokenMonitorApp
{
    public class FilterSettings
    {
        public MinCapFilter MinCap { get; set; } = new();
        public AvgCapFilter AvgCap { get; set; } = new();
        public DevFilter Dev { get; set; } = new();
        public MigrationFilter Migration { get; set; } = new();
        public LastFilter Last { get; set; } = new();
        public bool AutoOpenAxiom { get; set; }
    }

    public class MinCapFilter { public bool Enabled { get; set; } public int N { get; set; } = 2; public decimal MinValue { get; set; } = 14500; public bool AutoOpen { get; set; } }
    public class AvgCapFilter { public bool Enabled { get; set; } public int N { get; set; } = 2; public decimal Value { get; set; } = 20300; public bool AutoOpen { get; set; } }
    public class DevFilter { public bool Enabled { get; set; } public HashSet<string> Addresses { get; set; } = new(); public bool AutoOpen { get; set; } }
    public class MigrationFilter { public bool Enabled { get; set; } public int N { get; set; } = 2; public int Percent { get; set; } = 50; public bool AutoOpen { get; set; } }
    public class LastFilter { public bool Enabled { get; set; } public decimal MinMigratedAthUsd { get; set; } = 0m; public bool AutoOpen { get; set; } }
}

