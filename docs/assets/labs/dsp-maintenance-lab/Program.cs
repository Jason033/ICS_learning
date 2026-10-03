using System.Numerics;
using System.Globalization;
CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
var modes = new Dictionary<string, Action> { ["spectrum"] = Spectrum, ["sampling"] = Sampling, ["filter"] = Filter,
 ["snr"] = Snr, ["coding"] = Coding, ["iq"] = Iq, ["pipeline"] = Pipeline, ["diagnose"] = Diagnose };
string mode = args.Length == 0 ? "all" : args[0];
try
{
 if (mode == "all") foreach(var pair in modes) { Console.WriteLine($"MODE {pair.Key}"); pair.Value(); }
 else if(modes.TryGetValue(mode, out var run)) { Console.WriteLine($"MODE {mode}"); run(); }
 else throw new ArgumentException("mode: all|spectrum|sampling|filter|snr|coding|iq|pipeline|diagnose");
 Console.WriteLine("PASS all requested checks");
}
catch(Exception ex) { Console.Error.WriteLine($"FAIL {ex.Message}"); Environment.ExitCode = 1; }
static void Check(bool ok, string name) { if(!ok) throw new InvalidOperationException(name); Console.WriteLine($"PASS {name}"); }
static bool Near(double x, double y) => Math.Abs(x-y)<1e-8;
static Complex[] Dft(Complex[] samples, bool inverse = false)
{
 int n = samples.Length; if(n == 0) throw new ArgumentException("empty record");
 var result = new Complex[n]; double sign = inverse ? 1 : -1;
 for(int k=0;k<n;k++) for(int i=0;i<n;i++) result[k] += samples[i] * Complex.FromPolarCoordinates(1,sign*2*Math.PI*k*i/n);
 if(inverse) for(int k=0;k<n;k++) result[k] /= n;
 return result;
}
static double[] Tone(double fs, double f, int count, double amplitude = 1) => Enumerable.Range(0,count).Select(n=>amplitude*Math.Cos(2*Math.PI*f*n/fs)).ToArray();
static double Power(double[] x) { if(x.Length==0) throw new ArgumentException("empty record"); return x.Select(v=>v*v).Average(); }
static double SnrDb(double[] signal, double[] noise) => 10*Math.Log10(Power(signal)/Power(noise));
static void Spectrum()
{
 Complex[] x=[1,0,-1,0]; var bins=Dft(x); var back=Dft(bins,true);
 Console.WriteLine($"X1=({bins[1].Real:F3},{bins[1].Imaginary:F3}) X3=({bins[3].Real:F3},{bins[3].Imaginary:F3})");
 Check(Near(bins[1].Magnitude,2)&&Near(bins[3].Magnitude,2),"real cosine splits amplitude between positive and negative bins");
 Check(back.Zip(x,(a,b)=>(a-b).Magnitude).Max()<1e-8,"forward unscaled inverse divided by N reconstructs samples");
 Check(Near(2*bins[1].Magnitude/x.Length,1),"interior single sided amplitude has factor two");
 Check(Near(8000.0/400,20)&&Near(400.0/8000,0.05),"bin spacing and acquisition duration share N and fs");
}
static void Sampling()
{
 var low=Tone(8000,1000,16); var high=Tone(8000,7000,16);
 Check(low.Zip(high,(a,b)=>Math.Abs(a-b)).Max()<1e-8,"1kHz and 7kHz cosines alias at 8ksps");
 var edge=Enumerable.Range(0,8).Select(n=>Math.Sin(Math.PI*n)).ToArray();
 Check(edge.Max(Math.Abs)<1e-8,"sine at exactly Nyquist may produce zeros");
 Check(Near(48000.0*2/3,32000),"rational rate change tracks interpolation and decimation");
 Check(Near(6000.0/2,3000),"filtered decimation metadata must use output fs");
}
static void Filter()
{
 var state=new LabFir([0.25,0.5,0.25]);
 double[] first=state.Process([1,2]); double[] second=state.Process([3,4]);
 var whole=new LabFir([0.25,0.5,0.25]).Process([1,2,3,4]);
 var reset=new LabFir([0.25,0.5,0.25]).Process([3,4]);
 Console.WriteLine($"fir={string.Join(',',whole.Select(v=>v.ToString("F3")))} resetChunk={reset[0]:F3}");
 Check(first.Concat(second).Zip(whole,(a,b)=>Math.Abs(a-b)).Max()<1e-8,"stream state survives block boundaries");
 Check(Near(second[0],2)&&Near(reset[0],0.75),"reset causes a repeatable boundary transient");
 Check(Near((3-1)/2.0/8000*1000,0.125),"symmetric FIR group delay converted from samples to ms");
 Check(new LabFir([0.25,0.5,0.25]).Process([1,-1,1,-1,1]).Skip(2).All(v=>Near(v,0)),"example filter rejects alternating Nyquist component");
}
static void Snr()
{
 double[] s=[1,-1,1,-1]; double[] n=[0.25,0.25,-0.25,-0.25];
 double db=SnrDb(s,n);
 Console.WriteLine($"signalPower={Power(s):F6} noisePower={Power(n):F6} snrDb={db:F6}");
 Check(Near(Power(s),1)&&Near(Power(n),0.0625)&&Near(db,12.041199826559248),"power uses mean square not mean amplitude");
 Check(Near(SnrDb(s.Select(v=>v*2).ToArray(),n),db+6.020599913279624),"doubling signal amplitude adds 6dB if noise stays fixed");
 double[] clipped=[1,-1,1,-1]; double[] original=[2,-2,2,-2];
 Check(Near(Power(clipped.Zip(original,(a,b)=>a-b).ToArray()),1),"clipping error is distortion even without random noise");
}
static void Coding()
{
 int[] original=[1,0,1,1]; var encoded=original.SelectMany(b=>new[]{b,b,b}).ToArray();
 encoded[1]^=1; encoded[7]^=1;
 var decoded=Enumerable.Range(0,4).Select(i=>encoded.Skip(i*3).Take(3).Sum()>=2?1:0).ToArray();
 int rawErrors=encoded.Zip(original.SelectMany(b=>new[]{b,b,b}),(a,b)=>a!=b?1:0).Sum();
 int postErrors=decoded.Zip(original,(a,b)=>a!=b?1:0).Sum();
 Console.WriteLine($"codedBER={rawErrors}/12 decodedBER={postErrors}/4 codeRate={4.0/12:F6}");
 Check(rawErrors==2&&postErrors==0,"single error in each repetition word is corrected");
 encoded[0]^=1; var failed=encoded.Take(3).Sum()>=2?1:0;
 Check(failed!=original[0],"two errors in same word exceed majority correction");
 Check(Near(1000*2*0.75,1500),"QPSK symbol rate and code fraction determine information rate");
}
static void Iq()
{
 Complex[] pos=Enumerable.Range(0,4).Select(n=>Complex.FromPolarCoordinates(1,2*Math.PI*n/4)).ToArray();
 var a=Dft(pos);var b=Dft(pos.Select(Complex.Conjugate).ToArray());
 Check(Near(a[1].Magnitude,4)&&Near(a[3].Magnitude,0),"complex rotation separates signed frequency");
 Check(Near(b[3].Magnitude,4)&&Near(b[1].Magnitude,0),"conjugating IQ reverses frequency sign");
 Complex z=new(0.6,0.8); Check(Near(z.Magnitude,1)&&Near(z.Magnitude*z.Magnitude,1),"IQ magnitude power is I squared plus Q squared");
 Complex rotated=z*Complex.FromPolarCoordinates(1,-Math.PI/2);
 Check(Near(rotated.Real,0.8)&&Near(rotated.Imaginary,-0.6),"negative phase rotation has an explicit sign convention");
}
static void Pipeline()
{
 double fs=8000; double[] input=Tone(fs,1000,32); var filtered=new LabFir([0.25,0.5,0.25]).Process(input);
 var output=filtered.Where((v,i)=>i%2==0).ToArray();
 Check(output.Length==16&&Near(fs/2,4000),"decimation changes count and declared rate together");
 double seconds=output.Length/(fs/2); Check(Near(seconds,input.Length/fs),"duration preserved when count and rate change together");
 var bins=Dft(output.Skip(4).Take(8).Select(v=>new Complex(v,0)).ToArray());
 int peak=Enumerable.Range(1,3).OrderByDescending(k=>bins[k].Magnitude).First();
 Check(peak==2&&Near(peak*(fs/2)/8,1000),"calibrated spectrum preserves tone frequency through rate change");
 Check(Near(output.Length/0.001,16000)&&Near(seconds,0.004),"CPU processing throughput does not redefine physical sample rate");
}
static void Diagnose()
{
 var x=Tone(8000,1000,8); var bins=Dft(x.Select(v=>new Complex(v,0)).ToArray());
 Check(Near(bins[1].Magnitude/8,0.5)&&Near(2*bins[1].Magnitude/8,1),"half amplitude display can be scaling not attenuation");
 Check(Near(1*16000.0/8,2000)&&Near(1*8000.0/8,1000),"wrong rate label doubles reported frequency without changing data");
 var persistent=new LabFir([0.25,0.5,0.25]); persistent.Process([1,2]);
 Check(Near(persistent.Process([3])[0],2),"periodic boundary error is discriminated by state continuity");
}
sealed class LabFir(double[] taps)
{
 readonly double[] delay=new double[taps.Length];
 public double[] Process(double[] samples)
 {
  if(taps.Length==0) throw new ArgumentException("no taps");
  var output=new double[samples.Length];
  for(int n=0;n<samples.Length;n++)
  {
   for(int i=delay.Length-1;i>0;i--) delay[i]=delay[i-1];
   delay[0]=samples[n];
   for(int i=0;i<taps.Length;i++) output[n]+=taps[i]*delay[i];
  }
  return output;
 }
}
