using System.Buffers.Binary;

// Lab types only. No SIP/RTP, network, SDK, audio device or physical radio.
var mode = args.FirstOrDefault() ?? "all";
var modes = new[] { "audio", "observer", "experiment", "timeline", "cross-layer", "regression" };
if (mode != "all" && !modes.Contains(mode)) throw new ArgumentException("mode: all|audio|observer|experiment|timeline|cross-layer|regression");
foreach (string selected in mode == "all" ? modes : new[] { mode })
{
    Console.WriteLine($"MODE {selected}");
    switch (selected)
    {
        case "audio":
            foreach (var fault in new[] { "normal", "route", "old-session", "zero", "late", "mute" })
            {
                var p = LabAudio.Run(fault);
                Console.WriteLine($"{fault}: control={p.ControlAccepted} produced={p.Produced} sent={p.Sent} received={p.Received} accepted={p.Accepted} late={p.Late} played={p.Played} energetic={p.Energetic} lastRms={p.LastRms:F0}");
                Check(fault == "normal" ? p.Energetic == 3 : p.Energetic == 0, $"audio_{fault}");
            }
            Check(LabAudio.Run("old-session").Received == 3 && LabAudio.Run("old-session").Accepted == 0, "receive_is_not_accept");
            Check(LabAudio.Run("zero").Played == 3 && LabAudio.Run("zero").LastRms == 0, "play_is_not_signal");
            break;
        case "observer":
            var hidden = LabAudio.Run("normal", observe: false);
            Console.WriteLine($"observer-off: actualReceived={hidden.Received} recordedReceived={hidden.RecordedReceived} energetic={hidden.Energetic}");
            Check(hidden.Received == 3 && hidden.RecordedReceived == 0 && hidden.Energetic == 3, "missing_record_not_missing_action");
            break;
        case "experiment":
            foreach (int load in new[] { 1, 2 }) foreach (bool verbose in new[] { false, true })
            {
                int cost = LabExperiment.Latency(load, verbose);
                Console.WriteLine($"load={load} verbose={verbose} latency={cost} late={cost > 50}");
                Check((cost > 50) == (load == 2 && verbose), $"factorial_{load}_{verbose}");
            }
            break;
        case "timeline":
            // Lab shared true time; production cannot assume it is observable.
            long send = 100, receive = 130, offsetB = -50;
            long rawDelta = (receive + offsetB) - send;
            var interval = LabTime.DelayInterval(send, receive + offsetB, offsetB, 5);
            Console.WriteLine($"raw={rawDelta} corrected=[{interval.Low},{interval.High}]ms");
            Check(rawDelta == -20 && interval.Low == 25 && interval.High == 35, "offset_not_negative_transport");
            var rows = new[] { new LabIdentity("R1", 7, "B"), new LabIdentity("R1", 8, "B"), new LabIdentity("R1", 8, "C") };
            Check(rows.Count(x => x == new LabIdentity("R1", 8, "B")) == 1, "join_full_identity");
            break;
        case "cross-layer":
            var guarded = LabPtt.SendTimes(80, 110, true);
            var eager = LabPtt.SendTimes(80, 110, false);
            Console.WriteLine($"ptt guarded=[{string.Join(',', guarded)}] eager=[{string.Join(',', eager)}]");
            Check(guarded.SequenceEqual(new[] { 80, 100 }), "ptt_waits_for_ready_and_stops_on_release");
            var acceptedEager = LabPtt.AcceptedTimes(eager, 80, 110);
            Console.WriteLine($"ptt device-accepted=[{string.Join(',', acceptedEager)}]");
            Check(eager.Length - acceptedEager.Length == 4, "eager_drops_head_at_device");
            var link = new LabLink();
            var old = link.Plan("R1"); link.SwitchTo("backup");
            Check(!link.Apply(old), "old_route_result_does_not_promote_current");
            Check(link.Apply(link.Plan("R2")), "current_route_result_applies");
            break;
        case "regression":
            // Repairs have to preserve validation, not merely enable more output.
            var repaired = LabAudio.Run("normal");
            Check(repaired.Energetic == 3, "repair_positive");
            Check(LabAudio.Run("old-session").Accepted == 0, "repair_stale_negative");
            Check(LabAudio.Run("mute").Played == 0, "repair_mute_negative");
            Check(LabAudio.Run("late").Played == 0, "repair_deadline_negative");
            var ids = new[] { new LabIdentity("R1", 8, "B"), new LabIdentity("R1", 8, "C") };
            Check(ids.Distinct().Count() == 2, "repair_target_isolation");
            break;
    }
}
Console.WriteLine("PASS all selected scenarios");

static void Check(bool value, string name)
{
    if (!value) throw new InvalidOperationException($"FAIL {name}");
    Console.WriteLine($"PASS {name}");
}

sealed class LabAudioResult
{
    public bool ControlAccepted = true;
    public int Produced, Sent, Received, RecordedReceived, Accepted, Late, Played, Energetic;
    public double LastRms;
}
record LabFrame(int Session, int Sequence, int Timestamp, byte[] Pcm);
static class LabAudio
{
    public static LabAudioResult Run(string fault, bool observe = true)
    {
        var result = new LabAudioResult();
        const int currentSession = 8, clock = 8000, bufferMs = 40;
        string destination = fault == "route" ? "C" : "B";
        bool muted = fault == "mute";
        for (int seq = 0; seq < 3; seq++)
        {
            var pcm = new byte[160 * 2];
            for (int i = 0; i < 160; i++)
                BinaryPrimitives.WriteInt16LittleEndian(pcm.AsSpan(i * 2, 2),
                    (short)(fault == "zero" ? 0 : i % 2 == 0 ? 1000 : -1000));
            result.Produced++;
            var frame = new LabFrame(fault == "old-session" ? 7 : currentSession, seq, seq * 160, pcm);
            result.Sent++;
            if (destination != "B") continue;
            result.Received++;
            if (observe) result.RecordedReceived++;
            if (frame.Session != currentSession) continue;
            result.Accepted++;
            double deadline = frame.Timestamp * 1000.0 / clock + bufferMs; // T0=S0=0 in Lab shared ms axis.
            double arrival = seq * 20 + 20 + (fault == "late" ? 30 : 0);
            if (arrival > deadline) { result.Late++; continue; }
            double squares = 0;
            for (int i = 0; i < pcm.Length; i += 2)
            {
                double sample = BinaryPrimitives.ReadInt16LittleEndian(pcm.AsSpan(i, 2));
                squares += sample * sample;
            }
            result.LastRms = Math.Sqrt(squares / (pcm.Length / 2));
            if (muted) continue;
            result.Played++;
            if (result.LastRms > 0) result.Energetic++;
        }
        return result;
    }
}
static class LabExperiment
{
    // A declared queue model: base processing 20ms, +15 per load increment,
    // synchronous verbose sink costs 10ms per active producer. Not a measured machine.
    public static int Latency(int load, bool verbose) => 20 + 15 * (load - 1) + (verbose ? 10 * load : 0);
}
static class LabTime
{
    // offset means remote clock minus reference clock; uncertainty applies to offset estimate.
    public static (long Low, long High) DelayInterval(long localSend, long remoteReceive, long offset, long uncertainty)
    {
        if (uncertainty < 0) throw new ArgumentOutOfRangeException(nameof(uncertainty));
        long corrected = remoteReceive - offset - localSend;
        return (corrected - uncertainty, corrected + uncertainty);
    }
}
record LabIdentity(string Request, int Session, string Target);
static class LabPtt
{
    public static int[] AcceptedTimes(int[] sent, int readyAt, int releaseAt) =>
        sent.Where(t => t >= readyAt && t < releaseAt).ToArray();
    public static int[] SendTimes(int readyAt, int releaseAt, bool guard) =>
        Enumerable.Range(0, 7).Select(i => i * 20).Where(t => t < releaseAt && (!guard || t >= readyAt)).ToArray();
}
record LabPlan(string Request, string Link, int Epoch);
sealed class LabLink
{
    public string Active { get; private set; } = "primary";
    public int Epoch { get; private set; } = 1;
    public LabPlan Plan(string request) => new(request, Active, Epoch);
    public void SwitchTo(string next) { Active = next; Epoch++; }
    public bool Apply(LabPlan result) => result.Epoch == Epoch && result.Link == Active;
}
