// Lab-only contracts. No company SDK, network, actual UI, audio device or RF.
var mode = args.FirstOrDefault() ?? "all";
var modes = new[] { "paths", "contracts", "trace", "ownership" };
if (mode != "all" && !modes.Contains(mode)) throw new ArgumentException("mode: all|paths|contracts|trace|ownership");
foreach (string m in mode == "all" ? modes : new[] { mode })
{
    Console.WriteLine($"MODE {m}");
    switch(m)
    {
        case "paths":
            using(var a = new LabAdapter("A"))
            using(var c = new LabController(a,"B"))
            {
                var key = c.SubmitLevel(40);
                a.Emit(key, "Completed");
                var media = new LabMedia();
                media.Attach(0);
                bool old = media.Output(c.Session, true, new short[] { 600,-600 });
                Console.WriteLine($"control={c.States[key]} mediaAttached={media.AttachedSession} current={c.Session} output={old}");
                Check(c.States[key] == "Completed" && !old, "control_complete_media_stale");
                media.Attach(c.Session);
                Check(media.Output(c.Session, true, new short[] { 600,-600 }) && media.LastRms == 600, "current_media_nonzero");
                Check(!media.Output(c.Session, false, new short[] { 600,-600 }), "media_requires_ready");
                Check(media.Output(c.Session, true, new short[] {0,0}) && media.LastRms == 0, "output_call_not_nonzero");
            }
            break;
        case "contracts":
            using(var a = new LabAdapter("A"))
            using(var c = new LabController(a,"B"))
            {
                int converted = LabUnits.PercentToLevel(75);
                var key = c.SubmitLevel(converted);
                Console.WriteLine($"ui=75 percent deviceLevel={a.Requests.Single().Level} state={c.States[key]}");
                Check(a.Requests.Single().Level==75 && c.States[key]=="Pending", "accepted_not_completed");
                bool rejected=false;
                try { c.SubmitLevel(LabUnits.PercentToLevel(150)); }
                catch(ArgumentOutOfRangeException) {rejected=true;}
                Check(rejected && a.Requests.Count==1, "invalid_input_not_sent");
                a.Emit(new LabKey(key.Request,key.Session,"C"),"Completed");
                Check(c.States[key]=="Pending", "wrong_target_ignored");
                a.Emit(key,"InventedState");
                Check(c.States[key]=="Pending", "unknown_result_not_applied");
                c.Reconnect();
                var current = c.SubmitLevel(50);
                a.Emit(current with {Session=1},"Completed");
                Check(c.States[current]=="Pending", "old_session_current_request_ignored");
                // Historical full key still exists as Pending: lookup alone cannot reject it.
                a.Emit(key,"Completed");
                Console.WriteLine($"historical={c.States[key]} current={c.States[current]} applied={c.Applied}");
                Check(c.States[key]=="Pending" && c.States[current]=="Pending" && c.Applied==0,
                    "historical_pending_session_guard");
                a.Emit(current,"Completed");
                Check(c.States[current]=="Completed", "current_completion_applied");
            }
            break;
        case "trace":
            using(var a = new LabAdapter("A"))
            using(var c = new LabController(a,"B"))
            {
                a.CompleteDuringSend = true;
                var key = c.SubmitLevel(30);
                Console.WriteLine($"synchronous-event state={c.States[key]} applied={c.Applied}");
                Check(c.States[key]=="Completed" && c.Applied==1, "pending_before_callback");
                a.Emit(key,"Completed");
                Check(c.Applied==1, "duplicate_result_once");
                foreach(var row in c.Trace) Console.WriteLine(row);
            }
            break;
        case "ownership":
            using(var a = new LabAdapter("shared"))
            using(var wrong = new LabAdapter("other"))
            {
                var b = new LabController(a,"B");
                using(var c = new LabController(a,"C"))
                {
                    var kb=b.SubmitLevel(10); var kc=c.SubmitLevel(20);
                    wrong.Emit(kb,"Completed");
                    Check(b.States[kb]=="Pending", "publisher_instance_matters");
                    a.Emit(kb,"Completed");
                    Check(b.States[kb]=="Completed" && c.States[kc]=="Pending", "shared_target_isolation");
                    b.Dispose(); b.Dispose();
                    Console.WriteLine($"after-B-close adapterDisposed={a.Disposed} subscribers={a.SubscriberCount}");
                    Check(!a.Disposed && a.SubscriberCount==1,"borrower_unsubscribes_not_disposes_shared");
                    a.Emit(kc,"Completed");
                    Check(c.States[kc]=="Completed", "other_borrower_still_works");
                    int applied=b.Applied;
                    a.Emit(kb,"Completed");
                    Check(b.Applied==applied, "closed_controller_no_callback");
                }
                Check(a.SubscriberCount==0,"all_borrowers_detached");
            }
            break;
    }
}
Console.WriteLine("PASS all selected scenarios");
static void Check(bool ok,string name)
{
    if(!ok) throw new InvalidOperationException($"FAIL {name}");
    Console.WriteLine($"PASS {name}");
}
record LabKey(string Request,int Session,string Target);
record LabRequest(LabKey Key,int Level);
record LabCompletion(LabKey Key,string Result);
sealed class LabAdapter : IDisposable
{
    public string Name {get;}
    public bool CompleteDuringSend {get;set;}
    public bool Disposed {get;private set;}
    public List<LabRequest> Requests {get;} = new(); // Local API attempts, not wire sends.
    public event Action<LabCompletion>? Completed;
    public int SubscriberCount => Completed?.GetInvocationList().Length ?? 0;
    public LabAdapter(string name) => Name=name;
    public bool Send(LabRequest request)
    {
        ObjectDisposedException.ThrowIf(Disposed,this);
        Requests.Add(request);
        if(CompleteDuringSend) Emit(request.Key,"Completed");
        return true; // This Lab contract accepts the request; event decides completion.
    }
    public void Emit(LabKey key,string result)
    {
        ObjectDisposedException.ThrowIf(Disposed,this);
        Completed?.Invoke(new LabCompletion(key,result));
    }
    public void Dispose() {if(Disposed)return;Disposed=true;Completed=null;}
}
sealed class LabController : IDisposable
{
    readonly LabAdapter adapter; readonly string target;
    int next; bool closed;
    public int Session {get;private set;}=1;
    public int Applied {get;private set;}
    public Dictionary<LabKey,string> States {get;}=new();
    public List<string> Trace {get;}=new();
    public LabController(LabAdapter adapter,string target)
    {
        this.adapter=adapter; this.target=target;
        adapter.Completed+=OnCompleted;
    }
    public LabKey SubmitLevel(int level)
    {
        ObjectDisposedException.ThrowIf(closed,this);
        if(level<0 || level>100) throw new ArgumentOutOfRangeException(nameof(level));
        var key=new LabKey($"R{++next}",Session,target);
        States[key]="Pending"; // Must precede Send: this Lab allows synchronous completion.
        Trace.Add($"Submit target={target} session={Session} request={key.Request} level={level}");
        bool accepted=adapter.Send(new LabRequest(key,level));
        Trace.Add($"Send returned accepted={accepted} state={States[key]}");
        if(!accepted && States[key]=="Pending")States[key]="Rejected";
        return key;
    }
    void OnCompleted(LabCompletion value)
    {
        Trace.Add($"Callback publisher={adapter.Name} target={value.Key.Target} session={value.Key.Session} request={value.Key.Request}");
        if(closed || value.Key.Session!=Session || value.Key.Target!=target) return;
        if(value.Result != "Completed" && value.Result != "Rejected") return;
        if(!States.TryGetValue(value.Key,out string? state) || state!="Pending")return;
        States[value.Key]=value.Result; Applied++;
        Trace.Add($"Apply result={value.Result} applied={Applied}");
    }
    public void Reconnect() { Session++; Trace.Add($"Reconnect session={Session}"); }
    public void Dispose()
    {
        if(closed)return;
        closed=true;
        adapter.Completed-=OnCompleted; // Borrowed adapter: only the owner disposes it.
    }
}
static class LabUnits
{
    public static int PercentToLevel(double percent)
    {
        if(!double.IsFinite(percent) || percent<0 || percent>100)
            throw new ArgumentOutOfRangeException(nameof(percent));
        return (int)Math.Round(percent,MidpointRounding.AwayFromZero);
    }
}
sealed class LabMedia
{
    public int? AttachedSession {get;private set;}
    public double LastRms {get;private set;}
    public void Attach(int session)=>AttachedSession=session;
    public bool Output(int currentSession,bool ready,short[] samples)
    {
        if(AttachedSession!=currentSession || !ready || samples.Length==0)return false;
        double sum=0; foreach(double sample in samples)sum+=sample*sample;
        LastRms=Math.Sqrt(sum/samples.Length);
        return true; // Modeled output call, not physical playback.
    }
}
