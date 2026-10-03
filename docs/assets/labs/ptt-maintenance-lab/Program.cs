// 原創純記憶體模型；不連 GPIO、電台、音訊或網路。
var cases=new Dictionary<string,Action> {
 ["press"]=LabScenarios.Press,["interface"]=LabScenarios.Interface,
 ["state"]=LabScenarios.State,["grant"]=LabScenarios.Grant,
 ["timing"]=LabScenarios.Timing,["release"]=LabScenarios.Release,
 ["evidence"]=LabScenarios.Evidence,["troubleshooting"]=LabScenarios.Troubleshooting
};
try {
 string choice=args.Length==0?"all":args[0];
 if(choice=="all")foreach(var item in cases){Console.WriteLine($"=== {item.Key} ===");item.Value();}
 else if(cases.TryGetValue(choice,out var run))run(); else throw new ArgumentException("unknown Lab scenario");
 Console.WriteLine("PASS all assertions (Lab only; no device control)");return 0;
}catch(Exception ex){Console.Error.WriteLine(ex.Message);return 1;}
internal enum LabPttState { Idle, WaitingGrant, Keying, Tx, Fault }
internal sealed record LabPttEvidence(long Ms,int Epoch,string Event,LabPttState State,bool Held,bool Ptt,bool Ready,bool Audio);
internal sealed class LabPttController {
 public LabPttState State {get;private set;}=LabPttState.Idle;
 public bool Held {get;private set;}
 public bool Ptt {get;private set;}
 public bool RadioReady {get;private set;}
 public bool AudioOpen {get;private set;}
 public bool Busy {get;set;}
 public bool RequireGrant {get;set;}
 public int Epoch {get;private set;}
 public long MaxHoldMs {get;set;}=1000;
 public int SentSamples {get;private set;}
 public int DroppedSamples {get;private set;}
 public List<LabPttEvidence> Evidence {get;}=[];
 private long _pressedAt;
 public bool Press(long now) {
  if(State!=LabPttState.Idle || Held || Busy){Log(now,"PressDenied");return false;}
  Held=true;_pressedAt=now;Epoch++;RadioReady=false;AudioOpen=false;
  if(RequireGrant){State=LabPttState.WaitingGrant;Ptt=false;}
  else {State=LabPttState.Keying;Ptt=true;}
  Log(now,"PressAccepted");return true;
 }
 public void Grant(int epoch,long now) {
  if(epoch!=Epoch || !Held || State!=LabPttState.WaitingGrant){Log(now,"GrantIgnored");return;}
  Ptt=true;State=LabPttState.Keying;Log(now,"GrantAccepted");
 }
 public void Ready(int epoch,long now) {
  if(epoch!=Epoch || !Held || !Ptt || State!=LabPttState.Keying){Log(now,"ReadyIgnored");return;}
  RadioReady=true;AudioOpen=true;State=LabPttState.Tx;Log(now,"RadioReady");
 }
 public bool Frame(int samples,long now) {
  if(samples<0)throw new ArgumentOutOfRangeException(nameof(samples));
  bool allowed=Held && Ptt && RadioReady && AudioOpen && State==LabPttState.Tx;
  if(allowed)SentSamples+=samples; else DroppedSamples+=samples;
  Log(now,allowed?"FrameSent":"FrameDropped");return allowed;
 }
 public void Release(long now) {
  AudioOpen=false;Ptt=false;RadioReady=false;Held=false;State=LabPttState.Idle;Epoch++;Log(now,"Release");
 }
 public void Revoke(long now) { AudioOpen=false;Ptt=false;RadioReady=false;State=LabPttState.Fault;Epoch++;Log(now,"Revoke"); }
 public void Tick(long now) { if(Held && State!=LabPttState.Fault && now-_pressedAt>=MaxHoldMs)Revoke(now); }
 private void Log(long now,string name) { Evidence.Add(new(now,Epoch,name,State,Held,Ptt,RadioReady,AudioOpen)); }
}
internal sealed class LabPinMap(bool activeLow) {
 public bool LevelHigh(bool asserted)=>activeLow?!asserted:asserted;
 public bool Decode(bool high)=>activeLow?!high:high;
}
internal sealed class LabDebouncer(long stableMs) {
 private bool _candidate;
 private long _since;
 public bool Stable {get;private set;}
 public bool Sample(bool raw,long now) {
  if(raw!=_candidate){_candidate=raw;_since=now;}
  if(now-_since>=stableMs)Stable=_candidate;
  return Stable;
 }
}
internal static class LabScenarios {
 private static void Check(bool ok,string name){if(!ok)throw new Exception($"FAIL {name}");Console.WriteLine($"PASS {name}");}
 public static void Press(){var c=new LabPttController();c.Press(0);Check(c.Ptt&&!c.AudioOpen,"ptt_is_not_audio_readiness");c.Frame(160,10);c.Ready(c.Epoch,60);c.Frame(160,80);Check(c.SentSamples==160&&c.DroppedSamples==160,"readiness_gates_audio");}
 public static void Interface(){var map=new LabPinMap(true);Check(!map.LevelHigh(true)&&map.LevelHigh(false),"active_low_mapping");var d=new LabDebouncer(20);d.Sample(true,0);d.Sample(false,4);d.Sample(true,8);Check(!d.Sample(true,27)&&d.Sample(true,28),"stable_window_removes_bounce");}
 public static void State(){var c=new LabPttController{RequireGrant=true};c.Press(0);int e=c.Epoch;c.Ready(e,10);Check(!c.Ptt&&!c.AudioOpen,"ready_before_grant_ignored");c.Grant(e,20);c.Ready(e,40);Check(c.State==LabPttState.Tx,"legal_transition_reaches_tx");c.Release(50);c.Ready(e,60);Check(c.State==LabPttState.Idle&&!c.Ptt&&!c.AudioOpen,"release_invalidates_old_ready");}
 public static void Grant(){var busy=new LabPttController{Busy=true};Check(!busy.Press(0)&&!busy.Ptt,"busy_denies_new_press");var c=new LabPttController{RequireGrant=true};c.Press(0);int old=c.Epoch;c.Release(10);c.Press(20);c.Grant(old,30);Check(!c.Ptt&&c.State==LabPttState.WaitingGrant,"old_grant_cannot_key_new_press");c.Grant(c.Epoch,40);Check(c.Ptt&&!c.AudioOpen,"grant_is_not_radio_ready");}
 public static void Timing(){var c=new LabPttController();c.Press(0);c.Frame(160,20);c.Ready(c.Epoch,40);c.Frame(160,60);c.Release(70);c.Frame(160,80);Check(c.SentSamples==160&&c.DroppedSamples==320,"audio_only_in_valid_window");}
 public static void Release(){var c=new LabPttController{MaxHoldMs=100};c.Press(0);c.Ready(c.Epoch,10);c.Tick(100);Check(c.State==LabPttState.Fault&&!c.Ptt&&!c.AudioOpen,"timeout_cuts_model_outputs");int faultEpoch=c.Epoch;c.Tick(100);Check(c.Epoch==faultEpoch,"repeated_timeout_does_not_create_new_fault_epoch");Check(!c.Press(110),"held_timeout_cannot_auto_rearm");c.Release(120);Check(c.Press(130),"release_then_new_press_rearms");}
 public static void Evidence(){var c=new LabPttController{RequireGrant=true};c.Press(10);c.Grant(c.Epoch,40);c.Ready(c.Epoch,90);c.Frame(160,100);var press=c.Evidence.Single(x=>x.Event=="PressAccepted");var grant=c.Evidence.Single(x=>x.Event=="GrantAccepted");var ready=c.Evidence.Single(x=>x.Event=="RadioReady");Check(grant.Ms-press.Ms==30&&ready.Ms-grant.Ms==50,"stage_delays_separate");Check(press.Epoch==grant.Epoch&&grant.Epoch==ready.Epoch,"identity_links_same_press");}
 public static void Troubleshooting(){var c=new LabPttController{RequireGrant=true};c.Press(0);c.Grant(c.Epoch,10);c.Frame(160,20);Check(c.Ptt&&c.SentSamples==0,"keyed_but_not_ready_has_no_audio");c.Ready(c.Epoch,30);Check(c.Frame(160,40),"readiness_fix_allows_audio");int old=c.Epoch;c.Release(50);c.Ready(old,60);Check(!c.Ptt&&!c.AudioOpen,"late_ready_does_not_restart_tx");}
}
