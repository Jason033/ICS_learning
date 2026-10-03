// 自製整合模型：沒有公司SDK、網路、音訊、GPIO或設備控制。
var cases = new Dictionary<string, Action> {
 ["trace"] = LabScenarios.Trace, ["layers"] = LabScenarios.Layers,
 ["icd"] = LabScenarios.Icd, ["commands"] = LabScenarios.Commands,
 ["sessions"] = LabScenarios.Sessions, ["boundaries"] = LabScenarios.Boundaries,
 ["evidence"] = LabScenarios.Evidence, ["recovery"] = LabScenarios.Recovery,
 ["troubleshooting"] = LabScenarios.Troubleshooting
};
string choice = args.Length == 0 ? "all" : args[0];
try {
 if(choice=="all") foreach(var item in cases) { Console.WriteLine($"=== {item.Key} ==="); item.Value(); }
 else if(cases.TryGetValue(choice,out var run)) run();
 else throw new ArgumentException("scenario: all, trace, layers, icd, commands, sessions, boundaries, evidence, recovery, troubleshooting");
 Console.WriteLine("PASS all assertions (Lab model only)"); return 0;
} catch(Exception ex) { Console.Error.WriteLine(ex.Message); return 1; }

internal sealed record LabCommand(string RequestId, int Session, string Action, string Target, int Value);
internal sealed record LabReceipt(bool Accepted, string Code);
internal sealed record LabCompletion(string RequestId, int Session, string Result);
internal interface LabAdapterPort : IDisposable {
 event Action<LabCompletion>? Completed;
 LabReceipt Send(LabCommand command);
}
internal sealed class LabScriptedAdapter : LabAdapterPort {
 public event Action<LabCompletion>? Completed;
 public int HandlerCount => Completed?.GetInvocationList().Length ?? 0;
 public List<LabCommand> Sent { get; } = [];
 public bool Disposed { get; private set; }
 public bool ThrowOnSend { get; set; }
 public LabReceipt Send(LabCommand c) {
  ObjectDisposedException.ThrowIf(Disposed,this);
  if(ThrowOnSend) throw new IOException("Lab local enqueue failure");
  if(c.Action!="SetLevel" && c.Action!="QueryState") return new(false,"UnsupportedAction");
  if(c.Action=="SetLevel" && (c.Value<0 || c.Value>100)) return new(false,"OutOfRange");
  Sent.Add(c); Console.WriteLine($"SEND req={c.RequestId} session={c.Session} action={c.Action} target={c.Target} value={c.Value}");
  return new(true,"Accepted"); // 只表示模型已收，未完成操作。
 }
 public void Emit(LabCompletion e) { ObjectDisposedException.ThrowIf(Disposed,this); Completed?.Invoke(e); }
 public void Dispose() { if(Disposed)return; Disposed=true; Completed=null; }
}
internal sealed class LabController : IDisposable {
 private readonly LabAdapterPort _adapter; // 借用；root是owner。
 private static int _nextOwner;
 private readonly int _ownerId = Interlocked.Increment(ref _nextOwner);
 private int _counter;
 public int Session { get; private set; } = 1;
 public bool Connected { get; set; } = true;
 public bool Authenticated { get; set; } = true;
 public bool DeviceReady { get; set; } = true;
 public bool Closed { get; private set; }
 public int Applied { get; private set; }
 public int Ignored { get; private set; }
 public Dictionary<string,string> Results { get; } = [];
 public LabController(LabAdapterPort adapter) { _adapter=adapter; _adapter.Completed += OnComplete; }
 public string Submit(string action, int value, string target="D1") {
  if(Closed || !Connected || !Authenticated || !DeviceReady) throw new InvalidOperationException("Lab operation not permitted");
  string id=$"C{_ownerId}-S{Session}-R{++_counter}";
  Results[id]="Pending"; // 接收能力先建立；LabAdapter此處不重入，但其他實作可能會。
  try {
   var receipt=_adapter.Send(new(id,Session,action,target,value));
   if(!receipt.Accepted) Results[id]=$"Rejected:{receipt.Code}";
  } catch { Results[id]="LocalException"; throw; }
  return id;
 }
 private void OnComplete(LabCompletion e) {
  if(Closed || e.Session!=Session || !Results.TryGetValue(e.RequestId,out var state) || state.StartsWith("Rejected:") || state=="LocalException" || (state=="Completed" || state=="Failed")) {
   Ignored++; Console.WriteLine($"IGNORE req={e.RequestId} session={e.Session} result={e.Result}"); return;
  }
  Results[e.RequestId]=e.Result; Applied++;
  Console.WriteLine($"APPLY req={e.RequestId} session={e.Session} result={e.Result}");
 }
 public void Timeout(string id) { if(Results.TryGetValue(id,out var state) && state=="Pending")Results[id]="Unknown"; }
 public void Reconnect() { Session++; Results.Clear(); Authenticated=false; DeviceReady=false; Connected=true; }
 public void Dispose() { if(Closed)return; Closed=true; _adapter.Completed -= OnComplete; }
}
internal sealed class LabAudioPath {
 public bool Capturing {get;set;}
 public bool SendingAttached {get;set;}
 public bool ReceivingAttached {get;set;}
 public bool SpeakerConnected {get;set;}
 public bool Tx => Capturing && SendingAttached;
 public bool Rx => ReceivingAttached && SpeakerConnected;
}
internal static class LabScenarios {
 private static void Check(bool value,string label) { if(!value)throw new Exception($"FAIL {label}"); Console.WriteLine($"PASS {label}"); }
 public static void Trace() {
  using var adapter=new LabScriptedAdapter(); using var controller=new LabController(adapter);
  string id=controller.Submit("SetLevel",60);
  Check(adapter.Sent.Single().RequestId==id,"same_request_crosses_call_chain");
  Check(controller.Results[id]=="Pending","return_is_not_completion");
  adapter.Emit(new(id,controller.Session,"Completed"));
  Check(controller.Results[id]=="Completed" && controller.Applied==1,"completion_returns_by_event");
 }
 public static void Layers() {
  using var root=new LabScriptedAdapter(); using var a=new LabController(root); using var b=new LabController(root);
  LabAdapterPort alias=root;
  Check(ReferenceEquals(alias,root),"interface_reference_same_object");
  string aid=a.Submit("QueryState",0); string bid=b.Submit("QueryState",0);
  root.Emit(new(aid,a.Session,"Completed"));
  Check(aid!=bid && a.Results[aid]=="Completed" && b.Results[bid]=="Pending","shared_request_ids_are_isolated");
  a.Dispose();
  Check(!root.Disposed && root.HandlerCount==1,"borrower_does_not_dispose_shared_adapter");
  string id=b.Submit("QueryState",0); root.Emit(new(id,b.Session,"Completed"));
  Check(b.Results[id]=="Completed","other_borrower_keeps_working");
 }
 public static void Icd() {
  using var adapter=new LabScriptedAdapter();
  Check(!adapter.Send(new("R1",1,"SetLevel","D1",101)).Accepted,"reject_out_of_range");
  Check(!adapter.Send(new("R2",1,"setlevel","D1",20)).Accepted,"action_is_case_sensitive");
  Check(adapter.Send(new("R3",1,"SetLevel","D1",0)).Accepted,"lower_boundary_valid");
  Check(adapter.Send(new("R4",1,"SetLevel","D1",100)).Accepted,"upper_boundary_valid");
 }
 public static void Commands() {
  using var adapter=new LabScriptedAdapter(); using var controller=new LabController(adapter);
  string a=controller.Submit("SetLevel",40); string b=controller.Submit("SetLevel",80);
  adapter.Emit(new(b,1,"Completed")); adapter.Emit(new(a,1,"Completed")); adapter.Emit(new(a,1,"Completed"));
  Check(controller.Results[a]=="Completed" && controller.Results[b]=="Completed","out_of_order_matched_by_id");
  Check(controller.Applied==2 && controller.Ignored==1,"duplicate_completion_is_ignored");
 }
 public static void Sessions() {
  using var adapter=new LabScriptedAdapter(); using var controller=new LabController(adapter);
  string old=controller.Submit("QueryState",0); controller.Reconnect();
  bool blocked=false; try{controller.Submit("QueryState",0);}catch(InvalidOperationException){blocked=true;}
  Check(blocked,"reconnect_does_not_restore_readiness");
  controller.Authenticated=controller.DeviceReady=true;
  string current=controller.Submit("QueryState",0);
  adapter.Emit(new(old,1,"Completed")); adapter.Emit(new(current,1,"Completed"));
  Check(controller.Results[current]=="Pending" && controller.Applied==0,"current_id_old_session_is_rejected");
  adapter.Emit(new(current,2,"Completed"));
  Check(controller.Applied==1 && controller.Ignored==2,"old_session_is_not_applied");
 }
 public static void Boundaries() {
  using var adapter=new LabScriptedAdapter(); using var controller=new LabController(adapter);
  string id=controller.Submit("QueryState",0); adapter.Emit(new(id,1,"Completed"));
  var audio=new LabAudioPath{Capturing=false,SendingAttached=true,ReceivingAttached=true,SpeakerConnected=true};
  Check(controller.Results[id]=="Completed" && !audio.Tx && audio.Rx,"control_success_audio_one_way");
  audio.Capturing=true; Check(audio.Tx && audio.Rx,"capture_fix_preserves_control");
 }
 public static void Evidence() {
  var command=new LabCommand("R9",3,"SetLevel","D2",75);
  var receipt=new LabReceipt(true,"Accepted"); var completion=new LabCompletion("R9",3,"Completed");
  Check(command.RequestId==completion.RequestId && command.Session==completion.Session,"correlation_identity_matches");
  Console.WriteLine($"input req={command.RequestId} session={command.Session} target={command.Target} value={command.Value} return={receipt.Code} event={completion.Result}");
  var other=completion with{Session=2}; Check(other.RequestId==command.RequestId && other.Session!=command.Session,"same_request_text_not_same_session");
 }
 public static void Recovery() {
  using var adapter=new LabScriptedAdapter(); using var controller=new LabController(adapter);
  string id=controller.Submit("SetLevel",50); controller.Timeout(id);
  Check(controller.Results[id]=="Unknown" && adapter.Sent.Count==1,"timeout_does_not_resend");
  adapter.Emit(new(id,1,"Completed")); Check(controller.Results[id]=="Completed","current_late_result_can_resolve_unknown");
  adapter.ThrowOnSend=true; bool failed=false;
  try{controller.Submit("QueryState",0);}catch(IOException){failed=true;}
  Check(failed && controller.Results.Values.Contains("LocalException"),"local_exception_is_visible");
 }
 public static void Troubleshooting() {
  using var adapter=new LabScriptedAdapter(); using var controller=new LabController(adapter);
  string rejected=controller.Submit("SetLevel",-1); adapter.Emit(new(rejected,1,"Completed"));
  Check(controller.Results[rejected].StartsWith("Rejected:"),"rejected_command_cannot_be_completed");
  string good=controller.Submit("SetLevel",70); controller.Dispose(); adapter.Emit(new(good,1,"Completed"));
  Check(controller.Results[good]=="Pending" && adapter.HandlerCount==0,"closed_controller_does_not_receive");
  Check(!adapter.Disposed,"root_still_owns_adapter");
 }
}
