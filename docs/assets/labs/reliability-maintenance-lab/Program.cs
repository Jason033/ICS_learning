using System.Threading.Channels;

// Fictional maintenance model. Logical ticks are not measured device latency.
string mode = args.FirstOrDefault() ?? "all";
var cases = new Dictionary<string, Action> {
    ["deadline"] = Deadline, ["retry"] = Retry, ["health"] = Health,
    ["queue"] = QueueModel, ["ordering"] = Ordering, ["race"] = Race,
    ["async"] = AsyncModel, ["evidence"] = Evidence
};
if (mode == "all") foreach (var pair in cases) { Console.WriteLine($"\n[{pair.Key}]"); pair.Value(); }
else if (cases.TryGetValue(mode, out var run)) run();
else { Console.Error.WriteLine("Choose all/deadline/retry/health/queue/ordering/race/async/evidence"); return 2; }
Console.WriteLine("PASS: fictional invariants checked; no network/device access.");
return 0;

static void Check(bool value, string label) { if (!value) throw new InvalidOperationException(label); }
static void Deadline() {
    int deadline = 250, perAttempt = 100, now = 0;
    for (int attempt = 1; attempt <= 3; attempt++) {
        int remaining = deadline - now;
        if (remaining <= 0) break;
        int allowed = Math.Min(perAttempt, remaining);
        Console.WriteLine($"attempt={attempt} start={now} allowed={allowed}");
        now += allowed;
        if (attempt < 3) now = Math.Min(deadline, now + 30);
    }
    Check(now == deadline, "overall deadline");
    Console.WriteLine("local waiting expired at 250; remote outcome is not inferred.");
}
static void Retry() {
    var remote = new LabIdempotentService();
    var first = remote.Apply("request-7", "START");
    var repeated = remote.Apply("request-7", "START");
    Check(first == repeated && remote.SideEffects == 1, "deduplication");
    Console.WriteLine($"same ID: effects={remote.SideEffects}, result={repeated}");
    remote.Apply("request-8", "START");
    Check(remote.SideEffects == 2, "new intent");
    try { remote.Apply("request-7", "STOP"); throw new Exception("expected conflict"); }
    catch (ArgumentException) { Console.WriteLine("same ID / different payload: conflict"); }
    int window = 100;
    var random = new Random(7);
    for (int retry = 1; retry <= 4; retry++) {
        int delay = random.Next(0, window + 1);
        Check(delay <= window, "backoff window");
        Console.WriteLine($"retry={retry} window={window} selected={delay}");
        window = Math.Min(400, window * 2);
    }
}
static void Health() {
    var state = new LabRecovery();
    state.Transport = true;
    Console.WriteLine($"transport=true businessReady={state.Ready}");
    Check(!state.Ready, "transport alone");
    state.Authenticated = true; state.Subscribed = true; state.SnapshotConfirmed = true;
    Console.WriteLine($"all verified businessReady={state.Ready}");
    Check(state.Ready, "recovery chain");
    state.Subscribed = false;
    Check(!state.Ready, "subscription failed");
    Console.WriteLine("subscription lost: businessReady=false; cached state must be labelled stale.");
}
static void QueueModel() {
    int queued = 0, droppedTotal = 0;
    for (int second = 1; second <= 6; second++) {
        int remaining = queued + 8 - 5;
        int dropped = Math.Max(0, remaining - 12);
        queued = Math.Min(12, remaining); droppedTotal += dropped;
        Console.WriteLine($"second={second} depth={queued} dropped={dropped}");
    }
    Check(queued == 12 && droppedTotal == 6, "queue budget");
    var channel = Channel.CreateBounded<int>(new BoundedChannelOptions(2) { FullMode = BoundedChannelFullMode.Wait });
    Check(channel.Writer.TryWrite(1) && channel.Writer.TryWrite(2), "two slots");
    Check(!channel.Writer.TryWrite(3), "bounded queue rejects immediate third write");
    Console.WriteLine("Channel capacity=2 / Wait: TryWrite(3)=false");
    channel.Writer.TryComplete();
}
static void Ordering() {
    var gate = new LabStateGate("B");
    Check(!gate.Apply(new("A", 999, "Ready")), "old session rejected");
    Check(gate.Apply(new("B", 2, "Busy")), "new state");
    Check(!gate.Apply(new("B", 1, "Ready")), "old version rejected");
    Check(!gate.Apply(new("B", 2, "Busy")), "duplicate rejected");
    Check(gate.Value == "Busy", "state preserved");
    Console.WriteLine($"session=B version={gate.Version} value={gate.Value}");
}
static void Race() {
    int count = 0;
    using var barrier = new Barrier(2);
    void Broken() { int old = count; barrier.SignalAndWait(); count = old + 1; }
    var a = new Thread(Broken); var b = new Thread(Broken);
    a.Start(); b.Start(); a.Join(); b.Join();
    Check(count == 1, "forced lost update");
    Console.WriteLine($"forced interleaving: count={count}");
    count = 0; object guard = new();
    void Safe() { lock (guard) { int old = count; count = old + 1; } }
    a = new Thread(Safe); b = new Thread(Safe);
    a.Start(); b.Start(); a.Join(); b.Join();
    Check(count == 2, "complete critical section");
    Console.WriteLine($"lock protected: count={count}");
    // No actual deadlock is started: the wait graph is inspected instead.
    Console.WriteLine("wait graph: A holds X -> waits Y; B holds Y -> waits X (cycle)");
}
static void AsyncModel() {
    // A logical single-thread dispatcher: ready jobs run in order.
    int availableAt = 0;
    foreach (var job in new (string Name, int Ready, int Cost)[] { ("long-handler", 0, 500), ("status-callback", 20, 2), ("heartbeat", 100, 1) }) {
        int starts = Math.Max(availableAt, job.Ready);
        Console.WriteLine($"{job.Name}: ready={job.Ready} starts={starts} queueWait={starts-job.Ready}");
        availableAt = starts + job.Cost;
    }
    Check(availableAt == 503, "logical dispatcher ordering");
    Console.WriteLine("logical UI model only: .NET Console has no WPF Dispatcher.");
}
static void Evidence() {
    var traces = new[] {
        new LabTrace("op-7", 1, "B", "enqueue", 0),
        new LabTrace("op-7", 1, "B", "dequeue", 80),
        new LabTrace("op-7", 1, "B", "send", 82),
        new LabTrace("op-7", 1, "B", "accepted", 90),
        new LabTrace("op-7", 1, "B", "completed", 110),
        new LabTrace("op-7", 1, "B", "display", 150)
    };
    foreach (var x in traces) Console.WriteLine($"{x.Id},{x.Attempt},{x.Session},{x.Stage},{x.Tick}");
    int queueWait = traces[1].Tick - traces[0].Tick;
    int sendToComplete = traces[4].Tick - traces[2].Tick;
    int displayWait = traces[5].Tick - traces[4].Tick;
    Check(queueWait == 80 && sendToComplete == 28 && displayWait == 40, "stage durations");
    Console.WriteLine($"queue={queueWait}, send-to-complete={sendToComplete}, completed-to-display={displayWait}");
}

sealed class LabIdempotentService {
    readonly Dictionary<string, (string Payload, string Result)> completed = new();
    public int SideEffects { get; private set; }
    public string Apply(string id, string payload) {
        if (completed.TryGetValue(id, out var old)) {
            if (old.Payload != payload) throw new ArgumentException("same ID with different intent");
            return old.Result;
        }
        string result = $"applied-{++SideEffects}";
        completed.Add(id, (payload, result));
        return result;
    }
    // This single-thread, in-memory model has no crash/durability guarantee.
}
sealed class LabRecovery {
    public bool Transport, Authenticated, Subscribed, SnapshotConfirmed;
    public bool Ready => Transport && Authenticated && Subscribed && SnapshotConfirmed;
}
record LabStateEvent(string Session, long Version, string Value);
sealed class LabStateGate(string session) {
    public long Version { get; private set; } = -1;
    public string Value { get; private set; } = "Unknown";
    public bool Apply(LabStateEvent e) {
        if (e.Session != session || e.Version <= Version) return false;
        Version = e.Version; Value = e.Value; return true;
    }
}
record LabTrace(string Id, int Attempt, string Session, string Stage, int Tick);
