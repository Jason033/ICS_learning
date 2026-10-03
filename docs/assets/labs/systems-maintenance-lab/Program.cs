using System.Net;
using System.Globalization;

CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
var modes = new Dictionary<string, Action>(StringComparer.OrdinalIgnoreCase) {
    ["evidence"] = Evidence, ["ownership"] = Ownership, ["startup"] = Startup,
    ["endpoints"] = Endpoints, ["devices"] = Devices, ["time"] = Time,
    ["pressure"] = Pressure, ["diagnose"] = Diagnose
};
try {
    var chosen = args.Length == 0 ? "all" : args[0];
    if (chosen.Equals("all", StringComparison.OrdinalIgnoreCase)) {
        foreach (var (name, action) in modes) { Console.WriteLine($"MODE {name}"); action(); }
    } else if (modes.TryGetValue(chosen, out var action)) { Console.WriteLine($"MODE {chosen}"); action(); }
    else throw new ArgumentException("Use all or: " + string.Join(", ", modes.Keys));
    Console.WriteLine("PASS end");
} catch (Exception error) {
    Console.Error.WriteLine($"FAIL {error.GetType().Name}: {error.Message}");
    Environment.ExitCode = 1;
}

static void Check(string name, bool condition) {
    if (!condition) throw new InvalidOperationException(name);
    Console.WriteLine("PASS " + name);
}
static void Equal<T>(string name, T actual, T expected) where T : notnull {
    Console.WriteLine($"DATA {name} actual={actual} expected={expected}");
    Check(name, EqualityComparer<T>.Default.Equals(actual, expected));
}
static void Reject(string name, Action action) {
    bool rejected = false;
    try { action(); } catch (InvalidOperationException) { rejected = true; }
    Check(name, rejected);
}
static string Classify(bool? exists, bool? endpoint, bool? ready) {
    if (exists is null) return "NeedEvidence";
    if (exists == false) return "NoProcess";
    if (endpoint is null) return "NeedEvidence";
    if (endpoint == false) return "EndpointMismatch";
    return ready is null ? "NeedEvidence" : ready == true ? "Ready" : "NotReady";
}
static void Evidence() {
    Equal("missing_observation", Classify(null, true, true), "NeedEvidence");
    Equal("no_process_with_unknown_later_stages", Classify(false, null, null), "NoProcess");
    Equal("endpoint_mismatch", Classify(true, false, true), "EndpointMismatch");
    Equal("not_ready", Classify(true, true, false), "NotReady");
    Equal("ready", Classify(true, true, true), "Ready");
}
static void Ownership() {
    var counter = new LabLeaseCounter();
    using (var normal = counter.Open()) { Check("normal_acquired", counter.Active == 1); }
    Equal("normal_cleanup", counter.Active, 0);
    try { using var failing = counter.Open(); throw new ApplicationException("Synthetic processing fault"); }
    catch (ApplicationException) { }
    Equal("exception_cleanup", counter.Active, 0);
    Equal("opened_matches_closed", counter.Opened, counter.Closed);
    double perMinute = 30.0 * (2 - 1);
    Equal("fd_after_five_minutes", 200 + perMinute * 5, 350.0);
    Equal("operations_to_limit", 1024 - 350, 674);
    Console.WriteLine($"DATA minutes_to_limit={(1024 - 350) / perMinute:F6}");
    using var stream = new MemoryStream();
    stream.WriteByte(7);
    Check("standard_stream_example", stream.Length == 1);
}
static string Readiness(bool configExists, bool canOpen, bool deviceReady) {
    if (!configExists) return "MissingConfig";
    if (!canOpen) return "AccessDenied";
    return deviceReady ? "Ready" : "DevicePending";
}
static int? FirstReady(int start, int interval, int readyAt, int totalWait) {
    if (interval <= 0 || totalWait < 0) throw new ArgumentOutOfRangeException();
    int deadline = checked(start + totalWait);
    for (int now = start; now <= deadline; now = checked(now + interval))
        if (now >= readyAt) return now;
    return null;
}
static void Startup() {
    Equal("config_missing", Readiness(false, true, true), "MissingConfig");
    Equal("resource_denied", Readiness(true, false, true), "AccessDenied");
    Equal("device_pending", Readiness(true, true, false), "DevicePending");
    Equal("business_ready", Readiness(true, true, true), "Ready");
    Equal("ready_observed_ms", FirstReady(30, 100, 900, 1500) ?? -1, 930);
    Check("deadline_stops_pending", FirstReady(30, 100, 2000, 1500) is null);
    Equal("ordering_does_not_imply_ready", Readiness(true, true, 40 >= 900), "DevicePending");
    // Native-platform paths: do not pretend Linux Path.Combine resolves Windows paths.
    var folderA = Path.Combine(Path.GetTempPath(), "LabDesktop");
    var folderB = Path.Combine(Path.GetTempPath(), "LabService");
    Check("working_directory_changes_relative_source", Path.Combine(folderA, "bridge.json") != Path.Combine(folderB, "bridge.json"));
}
static uint V4(string text) {
    var bytes = IPAddress.Parse(text).GetAddressBytes();
    if (bytes.Length != 4) throw new ArgumentException("IPv4 model only");
    return ((uint)bytes[0] << 24) | ((uint)bytes[1] << 16) | ((uint)bytes[2] << 8) | bytes[3];
}
static string ChooseRoute(IEnumerable<LabRoute> routes, string destination) {
    uint address = V4(destination);
    return routes.Where(r => r.Matches(address)).OrderByDescending(r => r.Prefix).ThenBy(r => r.Metric).First().Interface;
}
static bool EndpointMatches(string bound, string protocol, int port, string destination, string wantedProtocol, int wantedPort) =>
    protocol == wantedProtocol && port == wantedPort && (bound == "0.0.0.0" || bound == destination);
static void Endpoints() {
    var routes = new[] { new LabRoute(V4("0.0.0.0"), 0, 5, "WAN"), new LabRoute(V4("198.51.100.0"), 24, 80, "VPN"), new LabRoute(V4("198.51.100.128"), 25, 100, "LAB") };
    Equal("longest_prefix_140", ChooseRoute(routes, "198.51.100.140"), "LAB");
    Equal("prefix_70", ChooseRoute(routes, "198.51.100.70"), "VPN");
    Equal("default_route", ChooseRoute(routes, "203.0.113.7"), "WAN");
    Check("loopback_does_not_match_lan", !EndpointMatches("127.0.0.1", "TCP", 9400, "192.0.2.10", "TCP", 9400));
    Check("protocol_matters", !EndpointMatches("0.0.0.0", "TCP", 9400, "192.0.2.10", "UDP", 9400));
    Check("loopback_match", EndpointMatches("127.0.0.1", "TCP", 9400, "127.0.0.1", "TCP", 9400));
}
static double FramesToMilliseconds(int frames, int rate) {
    if (frames < 0 || rate <= 0) throw new ArgumentOutOfRangeException();
    return frames / (double)rate * 1000;
}
static int BytesPerFrame(int bits, int channels) {
    if (bits <= 0 || bits % 8 != 0 || channels <= 0) throw new ArgumentOutOfRangeException();
    return checked(bits / 8 * channels);
}
static string DeviceOpen(bool present, bool canOpen, string actualDirection, string requestedDirection, bool supported) {
    if (!present) return "Absent";
    if (!canOpen) return "AccessDenied";
    if (actualDirection != requestedDirection) return "WrongDirection";
    return supported ? "Ready" : "UnsupportedFormat";
}
static void Devices() {
    Equal("bytes_per_frame", BytesPerFrame(16, 2), 4);
    Equal("callback_bytes", 960 * BytesPerFrame(16, 2), 3840);
    Equal("callback_ms", FramesToMilliseconds(960, 48000), 20.0);
    Equal("capacity_ms", FramesToMilliseconds(2880, 48000), 60.0);
    Equal("underflow_ms", Math.Max(0, 70 - FramesToMilliseconds(1920, 48000)), 30.0);
    Equal("wrong_direction", DeviceOpen(true, true, "playback", "capture", true), "WrongDirection");
    Equal("unsupported_format", DeviceOpen(true, true, "capture", "capture", false), "UnsupportedFormat");
    bool rejected = false;
    try { FramesToMilliseconds(10, 0); } catch (ArgumentOutOfRangeException) { rejected = true; }
    Check("invalid_rate_rejected", rejected);
}
static LabInterval CorrectTime(double observed, double offsetLow, double offsetHigh) {
    if (offsetLow > offsetHigh) throw new ArgumentException("Offset interval order");
    return new(observed - offsetHigh, observed - offsetLow);
}
static LabInterval Difference(LabInterval receive, LabInterval send) => new(receive.Low - send.High, receive.High - send.Low);
static void Time() {
    var send = CorrectTime(1000, 4, 8);
    var receive = CorrectTime(1020, -2, 2);
    Equal("corrected_send_low", send.Low, 992.0);
    Equal("corrected_send_high", send.High, 996.0);
    var delay = Difference(receive, send);
    Equal("delay_low_ms", delay.Low, 22.0);
    Equal("delay_high_ms", delay.High, 30.0);
    var overlapping = Difference(new(99, 103), new(98, 102));
    Equal("raw_delay_low", overlapping.Low, -3.0);
    Equal("causal_feasible_low", Math.Max(0, overlapping.Low), 0.0);
    Equal("causal_feasible_high", overlapping.High, 5.0);
    Equal("wall_clock_adjustment", 200 - 500, -300);
    Reject("different_instance_rejected", () => CoreEquivalents("I1", "I2", 120, 8, 60));
}
static double CoreEquivalents(string beforeId, string afterId, double beforeCpu, double afterCpu, double elapsedSeconds) {
    if (beforeId != afterId || elapsedSeconds <= 0 || afterCpu < beforeCpu)
        throw new InvalidOperationException("Invalid observation interval");
    return (afterCpu - beforeCpu) / elapsedSeconds;
}
static double SecondsToFull(double initial, double capacity, double production, double consumption) {
    if (initial < 0 || capacity < initial || production < 0 || consumption < 0) throw new ArgumentOutOfRangeException();
    return production <= consumption ? double.PositiveInfinity : (capacity - initial) / (production - consumption);
}
static void Pressure() {
    double cores = CoreEquivalents("I3", "I3", 20, 28, 8);
    Equal("core_equivalents", cores, 1.0);
    Equal("machine_cpu_percent", cores / 4 * 100, 25.0);
    Equal("queue_full_seconds", SecondsToFull(300, 900, 180, 150), 20.0);
    Equal("expanded_queue_seconds", SecondsToFull(300, 3900, 180, 150), 120.0);
    Equal("drain_seconds", 300.0 / (150 - 120), 10.0);
    Equal("balanced_queue_stays", Math.Max(0, 300 + (150 - 150) * 100), 300);
    Reject("cpu_generation_guard", () => CoreEquivalents("old", "new", 20, 28, 8));
}
static string FirstMismatch(bool? process, bool? endpoint, bool? resource, bool? ready) {
    if (process is null) return "NeedEvidence";
    if (process == false) return "NoProcess";
    if (endpoint is null) return "NeedEvidence";
    if (endpoint == false) return "EndpointMismatch";
    if (resource is null) return "NeedEvidence";
    if (resource == false) return "ResourceDenied";
    return ready is null ? "NeedEvidence" : ready == true ? "Ready" : "NotReady";
}
static void Diagnose() {
    Equal("case_A_first_boundary", FirstMismatch(true, false, null, null), "EndpointMismatch");
    Equal("case_B_resource_boundary", FirstMismatch(true, true, false, true), "ResourceDenied");
    Equal("early_negative_evidence", FirstMismatch(false, null, null, null), "NoProcess");
    Equal("case_C_underflow_ms", Math.Max(0, 65 - FramesToMilliseconds(2400, 48000)), 15.0);
    Equal("case_C_queue_full", SecondsToFull(200, 1000, 200, 160), 20.0);
    Equal("case_C_expanded_queue", SecondsToFull(200, 3000, 200, 160), 70.0);
    var completed = CorrectTime(500, 65, 75);
    Equal("remote_completed_low", completed.Low, 425.0);
    Equal("remote_completed_high", completed.High, 435.0);
    Equal("receive_after_completion_low", 450 - completed.High, 15.0);
    Equal("receive_after_completion_high", 450 - completed.Low, 25.0);
}
sealed class LabLeaseCounter {
    public int Opened { get; private set; }
    public int Closed { get; private set; }
    public int Active => Opened - Closed;
    public IDisposable Open() { Opened++; return new LabLease(this); }
    sealed class LabLease(LabLeaseCounter owner) : IDisposable {
        bool closed;
        public void Dispose() { if (!closed) { closed = true; owner.Closed++; } }
    }
}
sealed record LabRoute(uint Network, int Prefix, int Metric, string Interface) {
    public bool Matches(uint destination) {
        if (Prefix < 0 || Prefix > 32) throw new ArgumentOutOfRangeException();
        uint mask = Prefix == 0 ? 0u : uint.MaxValue << (32 - Prefix);
        return (destination & mask) == (Network & mask);
    }
}
readonly record struct LabInterval(double Low, double High);
