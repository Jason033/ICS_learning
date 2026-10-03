// 自製 Lab 模型；沒有 Ozeki 參照、SIP 網路、音訊裝置或公司設備。
// 事件由測試同步觸發，不能推論真實 SDK 的執行緒或時序保證。
var scenario = args.Length == 0 ? "all" : args[0];
var scenarios = new Dictionary<string, Action>
{
    ["graph"] = LabScenarios.Graph,
    ["registration"] = LabScenarios.Registration,
    ["calls"] = LabScenarios.Calls,
    ["media"] = LabScenarios.Media,
    ["lifecycle"] = LabScenarios.Lifecycle,
    ["version"] = LabScenarios.Version,
    ["evidence"] = LabScenarios.Evidence
};
try
{
    if (scenario == "all") foreach (var entry in scenarios) { Console.WriteLine($"=== {entry.Key} ==="); entry.Value(); }
    else if (scenarios.TryGetValue(scenario, out var action)) action();
    else throw new ArgumentException("scenario: all, graph, registration, calls, media, lifecycle, version, evidence");
    Console.WriteLine("PASS all assertions (local model only)");
}
catch (Exception ex) { Console.Error.WriteLine(ex.Message); return 1; }
return 0;

internal enum LabCallState { Created, Dialing, Ringing, Active, Ended }
internal sealed record LabRegistrationEvent(string LineId, string State, int SipStatus);
internal sealed record LabCallEvent(string CallId, LabCallState State);
internal sealed class LabLine(string id)
{
    public string Id { get; } = id;
    public string State { get; private set; } = "NotRegistered";
    public event Action<LabRegistrationEvent>? Changed;
    public void Register(int status)
    {
        State = status == 200 ? "Registered" : "Failed";
        Changed?.Invoke(new(Id, State, status));
    }
}
internal sealed class LabCall(string id, LabLine line, bool incoming = false)
{
    public string Id { get; } = id;
    public LabLine Line { get; } = line;
    public bool Incoming { get; } = incoming;
    public LabCallState State { get; private set; } = LabCallState.Created;
    public event Action<LabCallEvent>? Changed;
    public int HandlerCount => Changed?.GetInvocationList().Length ?? 0;
    public void Start() { if (Incoming) throw new InvalidOperationException("inbound call"); Emit(LabCallState.Dialing); }
    public void Answer() { if (!Incoming) throw new InvalidOperationException("outbound call"); Emit(LabCallState.Active); }
    public void HangUp() => Emit(LabCallState.Ended);
    public void Emit(LabCallState state) { State = state; Changed?.Invoke(new(Id, state)); }
}
internal sealed class LabEngine
{
    public LabLine MakeLine(string id) => new(id);
    public LabCall MakeCall(string id, LabLine line, bool incoming = false) => new(id, line, incoming);
}
internal sealed class LabMedia : IDisposable
{
    private readonly HashSet<(string Source, string Target)> _edges = [];
    public string? SenderCall { get; private set; }
    public string? ReceiverCall { get; private set; }
    public bool MicStarted { get; private set; }
    public bool SpeakerStarted { get; private set; }
    public bool Disposed { get; private set; }
    public int EdgeCount => _edges.Count;
    public int DisposeCount { get; private set; }
    public void Connect(string source, string target) { ObjectDisposedException.ThrowIf(Disposed, this); _edges.Add((source,target)); }
    public void Attach(string callId) { ObjectDisposedException.ThrowIf(Disposed, this); SenderCall = ReceiverCall = callId; }
    public void AttachSenderOnly(string callId) { ObjectDisposedException.ThrowIf(Disposed, this); SenderCall = callId; }
    public void Start() { ObjectDisposedException.ThrowIf(Disposed, this); MicStarted = SpeakerStarted = true; }
    public bool Tx(string callId) => !Disposed && MicStarted && SenderCall == callId && _edges.Contains(("mic","sender"));
    public bool Rx(string callId) => !Disposed && SpeakerStarted && ReceiverCall == callId && _edges.Contains(("receiver","speaker"));
    public void StopAndDetach() { MicStarted = SpeakerStarted = false; SenderCall = ReceiverCall = null; }
    public void Dispose() { if (Disposed) return; StopAndDetach(); _edges.Clear(); Disposed = true; DisposeCount++; }
}
internal sealed class LabSession : IDisposable
{
    public LabCall Call { get; }
    public LabMedia Media { get; } = new();
    public int AppliedEvents { get; private set; }
    public bool Ended { get; private set; }
    public LabSession(LabCall call)
    {
        Call = call;
        Media.Connect("mic", "sender"); Media.Connect("receiver", "speaker");
        call.Changed += OnState;
    }
    private void OnState(LabCallEvent e)
    {
        if (Ended || e.CallId != Call.Id) return;
        AppliedEvents++;
        Console.WriteLine($"APPLY call={e.CallId} state={e.State}");
        if (e.State == LabCallState.Active) { Media.Attach(Call.Id); Media.Start(); }
        if (e.State == LabCallState.Ended) Dispose();
    }
    public void Dispose()
    {
        if (Ended) return;
        Ended = true; Call.Changed -= OnState; Media.Dispose();
    }
}
internal static class LabScenarios
{
    private static void Check(bool valid, string label) { if (!valid) throw new Exception($"FAIL {label}"); Console.WriteLine($"PASS {label}"); }
    public static void Graph()
    {
        var engine = new LabEngine(); var line = engine.MakeLine("L1");
        var first = engine.MakeCall("C1", line); var second = engine.MakeCall("C2", line);
        var alias = first; alias.Emit(LabCallState.Active);
        Console.WriteLine($"C1={first.State} C2={second.State} sharedLine={ReferenceEquals(first.Line,second.Line)}");
        Check(ReferenceEquals(alias,first), "alias_same_object");
        Check(!ReferenceEquals(first,second), "calls_are_distinct");
        Check(second.State == LabCallState.Created, "second_state_unchanged");
    }
    public static void Registration()
    {
        var engine = new LabEngine(); var bad = engine.MakeLine("L-old"); var seen = 0;
        bad.Register(200); bad.Changed += _ => seen++;
        Console.WriteLine($"late-subscription observed={seen} actual={bad.State}");
        Check(seen == 0 && bad.State == "Registered", "reproduce_missed_event");
        var good = engine.MakeLine("L-new"); var observed = "NotRegistered";
        good.Changed += e => { observed = e.State; Console.WriteLine($"REG line={e.LineId} sip={e.SipStatus} state={e.State}"); };
        good.Register(403); Check(observed == "Failed", "failure_is_observed");
        good.Register(200); Check(observed == "Registered", "success_is_observed");
    }
    public static void Calls()
    {
        var line = new LabLine("L1"); var old = new LabCall("C1",line); var current = new LabCall("C2",line);
        var displayed = current.Id;
        Action<LabCallEvent> bad = e => { if(e.State == LabCallState.Ended) displayed = "none"; };
        old.Changed += bad; old.HangUp(); old.Changed -= bad;
        Check(displayed == "none", "reproduce_old_event_clears_new_call");
        displayed = current.Id;
        Action<LabCallEvent> guarded = e => { if(e.CallId == displayed && e.State == LabCallState.Ended) displayed = "none"; };
        old.Changed += guarded; old.HangUp(); old.Changed -= guarded;
        Check(displayed == "C2", "old_event_does_not_clear_C2");
        using var session = new LabSession(current);
        current.Start(); current.Emit(LabCallState.Active);
        Check(session.Media.Tx("C2") && session.Media.Rx("C2"), "current_call_bidirectional");
    }
    public static void Media()
    {
        using var reversed = new LabMedia();
        reversed.Connect("sender","mic"); reversed.Connect("receiver","speaker"); reversed.Attach("C1"); reversed.Start();
        Console.WriteLine($"wrong-direction tx={reversed.Tx("C1")} rx={reversed.Rx("C1")}");
        Check(!reversed.Tx("C1") && reversed.Rx("C1"), "reproduce_one_way_audio");
        using var missing = new LabMedia();
        missing.Connect("mic","sender"); missing.Connect("receiver","speaker"); missing.AttachSenderOnly("C1"); missing.Start();
        Check(missing.Tx("C1") && !missing.Rx("C1"), "missing_receiver_attachment");
        missing.Attach("C1");
        Check(missing.Tx("C1") && missing.Rx("C1"), "both_paths_pass_after_fix");
        Check(!missing.Tx("C2"), "attachment_belongs_to_one_call");
    }
    public static void Lifecycle()
    {
        var line = new LabLine("L1");
        for (var i=1;i<=3;i++)
        {
            var call = new LabCall($"C{i}",line); using var session = new LabSession(call);
            call.Start(); call.Emit(LabCallState.Active);
            Check(session.Media.Tx(call.Id) && session.Media.Rx(call.Id), $"C{i}_media_active");
            call.HangUp(); session.Dispose(); call.Emit(LabCallState.Ended);
            Check(call.HandlerCount == 0 && session.Media.EdgeCount == 0 && session.Media.DisposeCount == 1, $"C{i}_cleanup_once");
        }
        var probe = new LabCall("duplicate",line); var count = 0;
        Action<LabCallEvent> handler = _ => count++;
        probe.Changed += handler; probe.Changed += handler; probe.Start();
        Check(count == 2, "duplicate_subscription_calls_twice");
        probe.Changed -= handler;
        Check(probe.HandlerCount == 1, "one_remove_leaves_one_subscription");
        probe.Changed -= handler;
        Check(probe.HandlerCount == 0, "all_subscriptions_removed");
    }
    public static void Version()
    {
        var manifest = new LabManifest("App", "11.2.4.290", "net48", "x86", "C:/app/VoIPSDK.dll");
        var observed = manifest with { AssemblyVersion = "11.2.4.290", Path = "C:/temp/VoIPSDK.dll" };
        Console.WriteLine($"expected={manifest.Path} observed={observed.Path} version={observed.AssemblyVersion}");
        Check(manifest.AssemblyVersion == observed.AssemblyVersion && manifest.Path != observed.Path, "same_version_different_path");
        observed = manifest;
        Check(manifest == observed, "manifest_identity_matches");
        Console.WriteLine("Values are fictional; no Ozeki DLL was loaded.");
    }
    public static void Evidence()
    {
        var samples = new[] { new LabEvidence("A",true,true,false,true), new LabEvidence("B",true,false,true,true) };
        foreach (var item in samples)
        {
            var suspect = item.CaptureFrames ? "sender attachment or RTP path" : "device or capture path";
            Console.WriteLine($"case={item.Id} active={item.Active} capture={item.CaptureFrames} attached={item.Attached} rx={item.Rx} next={suspect}");
        }
        Check(samples.All(s => s.Active && s.Rx), "same_symptom_active_and_receive_only");
        Check(samples[0].CaptureFrames && !samples[0].Attached, "A_attachment_fault");
        Check(!samples[1].CaptureFrames && samples[1].Attached, "B_capture_fault");
    }
}
internal sealed record LabManifest(string App,string AssemblyVersion,string Target,string Arch,string Path);
internal sealed record LabEvidence(string Id,bool Active,bool CaptureFrames,bool Attached,bool Rx);
