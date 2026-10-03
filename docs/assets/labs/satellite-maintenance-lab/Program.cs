using System.Globalization;
CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
var modes = new Dictionary<string, Action>
{
    ["latency"] = Latency, ["budget"] = Budget, ["capacity"] = Capacity,
    ["queues"] = Queues, ["health"] = Health, ["failover"] = Failover,
    ["diagnose"] = Diagnose
};
string mode = args.Length == 0 ? "all" : args[0];
try
{
    if (mode == "all") foreach (var pair in modes) { Console.WriteLine($"MODE {pair.Key}"); pair.Value(); }
    else if (modes.TryGetValue(mode, out var run)) { Console.WriteLine($"MODE {mode}"); run(); }
    else throw new ArgumentException("mode: all|latency|budget|capacity|queues|health|failover|diagnose");
    Console.WriteLine("PASS all requested checks");
}
catch (Exception ex) { Console.Error.WriteLine($"FAIL {ex.Message}"); Environment.ExitCode = 1; }

static void Check(bool ok, string name) { if (!ok) throw new InvalidOperationException(name); Console.WriteLine($"PASS {name}"); }
static bool Near(double x, double y) => Math.Abs(x - y) < 1e-8;
static double Delay(double km, int bytes, double bitRate, double queueMs, double processMs)
{
    if (km < 0 || bytes < 0 || bitRate <= 0 || queueMs < 0 || processMs < 0) throw new ArgumentOutOfRangeException(nameof(km));
    return km / 300_000 * 1000 + bytes * 8.0 / bitRate * 1000 + queueMs + processMs;
}
static double PayloadRate(double baud, int bitsPerSymbol, double codeRate, double payloadFraction)
{
    if (baud <= 0 || bitsPerSymbol < 1 || codeRate <= 0 || codeRate > 1 || payloadFraction <= 0 || payloadFraction > 1) throw new ArgumentOutOfRangeException(nameof(codeRate));
    return baud * bitsPerSymbol * codeRate * payloadFraction;
}
static void Latency()
{
    double one = Delay(72_000, 1000, 1_000_000, 20, 5);
    double round = one * 2 + 30;
    Console.WriteLine($"oneWayMs={one:F3} roundTripWithWorkMs={round:F3}");
    Check(Near(one, 273) && Near(round, 576), "distance serialization queue work have separate units");
    Check(round > 500 && round <= 650, "500ms wait times out while 650ms admits baseline");
    double fast = Delay(2_400, 1000, 1_000_000, 20, 5);
    Check(Near(fast, 41), "lower geometric path does not remove queues");
}
static void Budget()
{
    double freeSpace = 92.45 + 20 * Math.Log10(40_000) + 20 * Math.Log10(12);
    double received = 50 - 2 + 30 - freeSpace + 40 - 2 - 3;
    double margin = received - (-100);
    Console.WriteLine($"fsplDb={freeSpace:F6} receivedDbm={received:F6} marginDb={margin:F6}");
    Check(Math.Abs(freeSpace - 206.07482474751174) < 1e-6, "km GHz free space formula");
    Check(margin > 6 && margin < 7 && margin - 8 < 0, "8dB extra loss crosses required receive power");
    Check(Near(20 * Math.Log10(2), 6.020599913279624), "doubling distance adds roughly 6dB loss");
}
static void Capacity()
{
    double net = PayloadRate(200_000, 2, 0.75, 0.8);
    double robust = PayloadRate(200_000, 2, 0.5, 0.8);
    Console.WriteLine($"normalPayloadBps={net:F0} robustPayloadBps={robust:F0}");
    Check(Near(net, 240_000) && Near(robust, 160_000), "code rate is a fraction not bits per second");
    Check(180_000 > robust && 180_000 < net, "quality recovery may still overload payload service");
    double[] before = [0.96, 0.98, 1.00, 1.02, 1.04];
    double[] after = [0.90, 0.95, 1.00, 1.10, 1.20];
    Check(after.Max()-after.Min() > before.Max()-before.Min(), "quality and delay variation need separate observations");
}
static void Queues()
{
    double bdpBytes = 2_000_000 * 0.6 / 8;
    double windowRate = 32_000 * 8 / 0.6;
    double bytes = 0;
    for (int second = 0; second < 5; second++) bytes = Math.Max(0, bytes + (180_000 - 160_000) / 8.0);
    double waitMs = bytes * 8 / 160_000 * 1000;
    Console.WriteLine($"bdpBytes={bdpBytes:F0} windowBoundBps={windowRate:F3} queueBytes={bytes:F0} waitMs={waitMs:F0}");
    Check(Near(bdpBytes, 150_000) && windowRate < 430_000, "flight window and link capacity are distinct");
    Check(Near(bytes, 12_500) && Near(waitMs, 625), "persistent offered excess accumulates delay");
    Check(180_000 * 0.8 < 160_000, "admission reduction creates drain capacity");
}
static void Health()
{
    var primary = new LabHealth("A", 1, 800, true, true, true, 1000);
    var backup = new LabHealth("B", 2, 70, true, true, false, 1000);
    Check(Select([primary, backup], 1100, 500, true) == "A", "working destination beats low RTT without service reachability");
    Check(Select([primary, backup], 1700, 500, true) is null, "stale success cannot prove current health");
    var currentBackup = backup with { Service = true, ObservedMs = 1700 };
    Check(Select([primary, currentBackup], 1750, 500, true) == "B", "fresh verified backup is eligible");
    var a = new LabFailureDetector(3);
    Check(!a.Observe(false) && !a.Observe(true) && !a.Observe(false), "success resets consecutive misses");
    Check(!a.Observe(false) && a.Observe(false), "three consecutive misses enter failed decision");
}
static string? Select(LabHealth[] links, long now, long freshness, bool requireService) => links
    .Where(l => now >= l.ObservedMs && now - l.ObservedMs <= freshness && l.Carrier && l.Address && (!requireService || l.Service))
    .OrderBy(l => l.Priority).ThenBy(l => l.RttMs).Select(l => l.Id).FirstOrDefault();
static void Failover()
{
    var manager = new LabSwitchState();
    long old = manager.Epoch;
    manager.Begin("B");
    Check(manager.Active == "A" && !manager.SessionReady, "candidate does not immediately become active");
    Check(!manager.Commit(old, "B", true, true), "old epoch cannot commit new switch");
    Check(!manager.Commit(manager.Epoch, "B", true, false), "route without new session is insufficient");
    Check(manager.Commit(manager.Epoch, "B", true, true) && manager.Active == "B", "route and session evidence complete activation");
    Check(!manager.MayFailback(999, 1000, 1000) && manager.MayFailback(2000, 1000, 1000), "sustained recovery avoids immediate flapping");
    Console.WriteLine($"active={manager.Active} epoch={manager.Epoch} sessionReady={manager.SessionReady}");
}
static void Diagnose()
{
    double quiet = Delay(2400, 1000, 1_000_000, 0, 5);
    double loaded = Delay(2400, 1000, 1_000_000, 600, 5);
    Check(Near(loaded - quiet, 600), "same geometry plus traffic suggests queue delay not orbit change");
    Check(Select([new LabHealth("A", 1, 80, true, true, false, 500)], 550, 100, true) is null, "carrier up is not application recovery");
    Check(1220 + 80 > 1280 && 1100 + 80 <= 1280, "tunnel overhead can cross an example MTU");
}
record LabHealth(string Id, int Priority, double RttMs, bool Carrier, bool Address, bool Service, long ObservedMs);
sealed class LabFailureDetector(int threshold)
{
    int misses;
    public bool Observe(bool success) { misses = success ? 0 : misses + 1; return misses >= threshold; }
}
sealed class LabSwitchState
{
    public long Epoch { get; private set; } = 1;
    public string Active { get; private set; } = "A";
    public bool SessionReady { get; private set; } = true;
    string? candidate;
    public void Begin(string id) { Epoch++; candidate = id; SessionReady = false; }
    public bool Commit(long eventEpoch, string id, bool routeReady, bool sessionReady)
    {
        if (eventEpoch != Epoch || candidate != id || !routeReady || !sessionReady) return false;
        Active = id; SessionReady = true; candidate = null; return true;
    }
    public bool MayFailback(long now, long healthySince, long holdMs) => now >= healthySince && now-healthySince >= holdMs;
}
