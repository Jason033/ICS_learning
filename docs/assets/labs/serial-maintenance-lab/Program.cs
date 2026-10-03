using System.Text;
// Original deterministic model. No SerialPort package, USB, COM or device access.
string mode = args.FirstOrDefault() ?? "frames";
bool fault = args.Contains("--fault");
void Check(bool value,string message) {
    if(!value) throw new InvalidOperationException(message);
    Console.WriteLine("PASS "+message);
}
switch(mode) {
case "layers": {
    byte[] tx=Encoding.ASCII.GetBytes(fault?"STATUS?":"STATUS?\r\n");
    Console.WriteLine($"COM-open=OK write={tx.Length} HEX={Convert.ToHexString(tx)}");
    Console.WriteLine(tx.TakeLast(2).SequenceEqual(new byte[]{13,10})?"Lab message complete; response=OK,READY":"UART bytes available; message missing CR LF");
    break;
}
case "wiring": {
    foreach(double voltage in new[]{-6.0,0.0,6.0})
        Console.WriteLine($"RS232 data voltage={voltage:F1}V => {(voltage<=-3?"mark=1":voltage>=3?"space=0":"undefined region")}");
    double txSignal=5, referenceShift=2;
    Console.WriteLine($"single-ended signal relative to receiver={txSignal-referenceShift}V");
    double a=4,b=1,commonShift=2;
    Console.WriteLine($"differential before={a-b}V after={(a+commonShift)-(b+commonShift)}V");
    Console.WriteLine("No pin numbers or construction instructions are implied.");
    break;
}
case "uart": {
    int baud=9600, data=8, parity=0, stop=1;
    int bits=1+data+parity+stop;
    Console.WriteLine($"8N1 bitsPerChar={bits} bitTimeUs={1_000_000.0/baud:F6} charTimeMs={bits*1000.0/baud:F6}");
    Console.WriteLine($"request9BytesMs={9*bits*1000.0/baud:F6} response10BytesMs={10*bits*1000.0/baud:F6}");
    byte ch=0x53;
    string lsbFirst=string.Concat(Enumerable.Range(0,8).Select(i=>((ch>>i)&1).ToString()));
    Console.WriteLine($"byte=53 lsbFirst={lsbFirst} evenParity={(System.Numerics.BitOperations.PopCount((uint)ch)%2)}");
    Console.WriteLine("receiver3percentSlow driftAt9.5bits=0.285bit; receiver6percentSlow=0.57bit (idealized)");
    break;
}
case "frames": {
    var parser=new LabParser();
    byte[][] chunks=fault?new[]{new byte[]{0,0xaa,0xff,0xaa,2,0x90,1,0x92},new byte[]{0xaa,2,0x90,1,0x93}}:
        new[]{new byte[]{0xaa,2,0x90},new byte[]{1,0x93,0xaa,1,0x10,0x11}};
    foreach(var c in chunks) { Console.WriteLine("RX "+Convert.ToHexString(c)); parser.Feed(c); }
    Console.WriteLine($"complete={parser.Completed} buffered={parser.Buffered}");
    Check(parser.Completed==(fault?1:2),"expected valid frame count");
    Check(parser.Buffered==0,"bounded parser drains known input");
    break;
}
case "timeout": {
    int deadline=200;
    foreach(var e in new[]{(At:10,Hex:"AA0290"),(At:210,Hex:"0193")})
        Console.WriteLine($"t={e.At} RX={e.Hex} withinDeadline={e.At<=deadline}");
    Console.WriteLine("t=200 result=INCOMPLETE 3/5; t=210 late bytes are not a new transaction success");
    break;
}
case "events": {
    int[] callbackChunks={3,0,2}; int total=0;
    foreach(int n in callbackChunks){total+=n;Console.WriteLine($"DataAvailable chunk={n} total={total} complete={total>=5}");}
    Check(total==5,"callback count is not byte or message count");
    break;
}
case "interfaces": {
    double baud=19200; int tx=20,rx=12;double work=5,turnaround=2;
    double serialTime=(tx+rx)*10*1000/baud;
    Console.WriteLine($"halfduplex txMs={tx*10*1000/baud:F3} rxMs={rx*10*1000/baud:F3} workMs={work} turnaroundMs={turnaround} totalMs={serialTime+work+turnaround:F3}");
    Console.WriteLine("one driver at a time; UART framing does not arbitrate bus ownership");
    break;
}
case "diagnose": {
    Console.WriteLine("Lab device: 9600,8,N,1; ASCII CR LF; no flow control");
    Console.WriteLine(fault?"APP TX=5354415455533F; device parser waiting for CR LF":"APP TX=5354415455533F0D0A; reply=4F4B2C52454144590D0A");
    Console.WriteLine("Next validation includes split bytes, two frames, corrupt checksum and deadline.");
    break;
}
default:throw new ArgumentException(mode);
}
sealed class LabParser {
    readonly List<byte> pending=new();
    public int Completed {get;private set;}
    public int Buffered=>pending.Count;
    // Lab frame AA LEN CMD DATA... XOR; LEN counts CMD+DATA, 1..32.
    public void Feed(byte[] bytes) {
        foreach(byte b in bytes) {
            pending.Add(b);
            while(pending.Count>0) {
                if(pending[0]!=0xaa){Console.WriteLine("skip noise "+pending[0].ToString("X2"));pending.RemoveAt(0);continue;}
                if(pending.Count<2) break;
                int len=pending[1];
                if(len<1||len>32){Console.WriteLine("invalid LEN="+len);pending.RemoveAt(0);continue;}
                int total=len+3;
                if(pending.Count<total) break;
                byte check=0;
                for(int i=1;i<total-1;i++)check^=pending[i];
                if(check!=pending[total-1]){Console.WriteLine("checksum reject");pending.RemoveAt(0);continue;}
                Console.WriteLine("FRAME "+Convert.ToHexString(pending.Take(total).ToArray()));
                Completed++;pending.RemoveRange(0,total);
            }
        }
    }
}
