// Independent teaching model. No SIP SDK, sockets, audio device or company APIs.
using System.Globalization;
using System.Text;
var output = args.Length > 0 ? Path.GetFullPath(args[0]) : Path.Combine(Path.GetTempPath(), "voip-maintenance-results");
Directory.CreateDirectory(output);
void Check(bool ok, string reason) { if (!ok) throw new InvalidOperationException(reason); }
const int rate = 8000, channels = 1, bits = 16, ms = 20;
int blockAlign = channels * bits / 8;
int byteRate = rate * blockAlign;
int framesPerPacket = rate * ms / 1000;
Check((blockAlign,byteRate,framesPerPacket)==(2,16000,160),"PCM units");
var samples = Enumerable.Range(0,rate).Select(i=>(short)Math.Round(8000*Math.Sin(2*Math.PI*1000*i/rate))).ToArray();
var silent = new short[rate];
short[] Scale(short[] input, double gain) => input.Select(x=>(short)Math.Clamp(Math.Round(x*gain),short.MinValue,short.MaxValue)).ToArray();
var clipped = Scale(samples, 5);
Check(clipped.Count(x=>x==short.MinValue||x==short.MaxValue)>0,"clipping");
void WriteWave(string name,short[] pcm) {
 using var f=File.Create(Path.Combine(output,name));using var w=new BinaryWriter(f,Encoding.ASCII);
 int len=pcm.Length*2;w.Write(Encoding.ASCII.GetBytes("RIFF"));w.Write(36+len);w.Write(Encoding.ASCII.GetBytes("WAVEfmt "));
 w.Write(16);w.Write((short)1);w.Write((short)channels);w.Write(rate);w.Write(byteRate);w.Write((short)blockAlign);w.Write((short)bits);
 w.Write(Encoding.ASCII.GetBytes("data"));w.Write(len);foreach(short x in pcm)w.Write(x);
}
WriteWave("tone-8k-mono.wav",samples);WriteWave("silence-8k-mono.wav",silent);WriteWave("clipped-8k-mono.wav",clipped);
Check(new FileInfo(Path.Combine(output,"tone-8k-mono.wav")).Length==16044,"44byte WAV +16000 PCM");
var arrivals = new[] { new Arrival(10,0,25), new Arrival(11,160,45), new Arrival(12,320,82), new Arrival(13,480,85), new Arrival(14,640,105) };
var rows=new List<string>{"seq,timestamp,arrival_ms,deadline_40_ms,late_40,deadline_60_ms,late_60"};
foreach(var a in arrivals){double media=a.Timestamp/(double)rate*1000;double d40=media+40,d60=media+60;rows.Add(string.Join(',',a.Seq,a.Timestamp,a.Milliseconds,d40,a.Milliseconds>d40,d60,a.Milliseconds>d60));}
Check(arrivals.Count(a=>a.Milliseconds>a.Timestamp/(double)rate*1000+40)==1,"40ms buffer late count");
Check(arrivals.Count(a=>a.Milliseconds>a.Timestamp/(double)rate*1000+60)==0,"60ms buffer late count");
File.WriteAllLines(Path.Combine(output,"playout.csv"),rows);
double jitter=0;
for(int i=1;i<arrivals.Length;i++){double d=(arrivals[i].Milliseconds-arrivals[i-1].Milliseconds)*rate/1000.0-(arrivals[i].Timestamp-arrivals[i-1].Timestamp);jitter+=(Math.Abs(d)-jitter)/16;}
Console.WriteLine($"PCM: rate={rate}, channels={channels}, bits={bits}, blockAlign={blockAlign}, byteRate={byteRate}");
Console.WriteLine($"20ms: {framesPerPacket} frames, {framesPerPacket*blockAlign} PCM bytes; PCMU would be 160 bytes");
foreach(int ptime in new[]{10,20,40}){int pcmu=rate*ptime/1000;int packets=1000/ptime;int ipBits=(pcmu+12+8+20)*packets*8;Console.WriteLine($"PCMU ptime={ptime}ms payload={pcmu} packets/s={packets} IPv4bit/s={ipBits}");}
Console.WriteLine(FormattableString.Invariant($"J after example = {jitter} RTP units = {jitter/rate*1000} ms"));
Console.WriteLine($"PASS: PCM lengths, WAV structure, clipping, packetization and playout deadlines. output={output}");
record Arrival(int Seq,int Timestamp,double Milliseconds);
