using System.Buffers.Binary;
using System.Globalization;
using System.Net;
using System.Text;

// Original teaching model. No network traffic, administrator rights or devices.
// Arguments: flow endpoints ethernet routes tcp services multicast mtu metrics diagnose.
var mode = args.FirstOrDefault() ?? "flow";
bool fault = args.Contains("--fault");
uint IPv4(string text) {
    byte[] bytes = IPAddress.Parse(text).GetAddressBytes();
    if (bytes.Length != 4) throw new ArgumentException("IPv4 only");
    return BinaryPrimitives.ReadUInt32BigEndian(bytes);
}
uint Mask(int prefix) => prefix switch {
    0 => 0, >= 1 and <= 32 => uint.MaxValue << (32-prefix),
    _ => throw new ArgumentOutOfRangeException(nameof(prefix))
};
string Address(uint value) {
    byte[] bytes = new byte[4];
    BinaryPrimitives.WriteUInt32BigEndian(bytes, value);
    return new IPAddress(bytes).ToString();
}
bool Matches(string destination, string network, int prefix) =>
    (IPv4(destination) & Mask(prefix)) == (IPv4(network) & Mask(prefix));
void Check(bool condition, string name) {
    if (!condition) throw new InvalidOperationException(name);
    Console.WriteLine($"PASS {name}");
}
switch (mode) {
case "flow": {
    byte[] request = Encoding.ASCII.GetBytes(fault ? "STATUS?" : "STATUS?\n");
    Console.WriteLine($"id=Q17 connect-complete=20ms bytes={request.Length} hex={Convert.ToHexString(request)}");
    Console.WriteLine("id=Q17 TCP-ack=35ms");
    if (request[^1] == 10) {
        Console.WriteLine("id=Q17 parse-complete=40ms reply=OK,READY at=65ms");
        Check(request.Length == 8, "newline-framed request");
    } else Console.WriteLine("id=Q17 parser-waiting-for-LF deadline=500ms result=UNKNOWN");
    break;
}
case "endpoints": {
    var rows = new[] {
        new { Local="127.0.0.1", Port=7000, Remote="*", State="Listen", Pid=4100 },
        new { Local="192.0.2.10", Port=54000, Remote="198.51.100.20:7000", State="Established", Pid=4200 },
        new { Local="192.0.2.10", Port=54001, Remote="198.51.100.20:7000", State="Established", Pid=4200 },
    };
    foreach (var r in rows) Console.WriteLine($"TCP local={r.Local}:{r.Port} remote={r.Remote} state={r.State} pid={r.Pid}");
    Console.WriteLine("IPv6 endpoint=" + new IPEndPoint(IPAddress.Parse("2001:db8::10"),7000));
    Check(rows.Count(r => r.State == "Established") == 2, "two separate connections");
    break;
}
case "ethernet": {
    int accessVlan = fault ? 20 : 10;
    Console.WriteLine($"A 192.0.2.10/24 access-vlan={accessVlan} ARP who-has 192.0.2.1");
    Console.WriteLine("Gateway 192.0.2.1 access-vlan=10");
    Console.WriteLine(accessVlan == 10 ? "ARP reply mac=02:00:00:00:10:01; frame A-MAC->GW-MAC; IP A->B" : "ARP no reply: different broadcast domains");
    if (accessVlan == 10) Console.WriteLine("Router output: frame GW2-MAC->B-MAC; IP A->B; TTL 64->63");
    Check(Encoding.ASCII.GetByteCount("STATUS?\n")+20+20 == 48, "IPv4+TCP+payload=48 bytes");
    break;
}
case "routes": {
    string destination = args.Skip(1).FirstOrDefault(a=>!a.StartsWith("--")) ?? "10.24.6.77";
    var routes = new[] {
        new Route("0.0.0.0",0,"192.0.2.1",10),
        new Route("10.24.0.0",16,"192.0.2.2",5),
        new Route("10.24.6.0",24,"192.0.2.3",50),
        new Route("10.24.6.64",26,"192.0.2.4",100),
    };
    var candidates = routes.Where(r=>Matches(destination,r.Network,r.Prefix));
    foreach(var r in candidates) Console.WriteLine($"MATCH {r.Network}/{r.Prefix} via={r.NextHop} metric={r.Metric}");
    var best = candidates.OrderByDescending(r=>r.Prefix).ThenBy(r=>r.Metric).First();
    Console.WriteLine($"SELECT {best.Network}/{best.Prefix} via={best.NextHop}");
    uint network = IPv4(destination) & Mask(26);
    Console.WriteLine($"/26 network={Address(network)} broadcast={Address(network | ~Mask(26))}");
    Check(Matches("10.24.6.77","10.24.6.64",26),"/26 includes .77");
    Check(!Matches("10.24.6.130","10.24.6.64",26),"/26 excludes .130");
    break;
}
case "tcp": {
    uint initial = 1000; int payload = 8;
    uint next = initial + 1 + (uint)payload;
    Console.WriteLine($"SYN seq={initial}; first-data={initial+1}; bytes={payload}; expected-ack={next}");
    uint ack=1009, window=4096, sentNext=2009;
    Console.WriteLine($"ack={ack} window={window} in-flight={sentNext-ack} may-send={ack+window-sentNext}");
    var chunks = new[] { "STA", "TUS?\n" };
    var pending = new StringBuilder();
    foreach(var chunk in chunks) {
        pending.Append(chunk);
        Console.WriteLine($"read={chunk.Length}; buffered={pending.Length}; message-complete={pending.ToString().EndsWith('\n')}");
    }
    Check(next==1009,"SYN consumes one sequence number");
    Check(pending.ToString()=="STATUS?\n","stream reassembly");
    break;
}
case "services": {
    Console.WriteLine("DNS name=lab.example A=198.51.100.20 ttl=120 obtained=0s");
    foreach(int elapsed in new[]{30,120,150}) Console.WriteLine($"elapsed={elapsed}s cache-valid={elapsed<120}");
    var original = new Flow("TCP","10.0.0.10",54000,"198.51.100.20",7000);
    var translated = original with { Src="203.0.113.5", SrcPort=61000 };
    Console.WriteLine("INSIDE "+original); Console.WriteLine("OUTSIDE "+translated);
    Console.WriteLine("REPLY dst=203.0.113.5:61000 -> translated dst=10.0.0.10:54000");
    Check(original.Dst==translated.Dst,"source NAT keeps destination in this model");
    break;
}
case "multicast": {
    string group=args.Skip(1).FirstOrDefault(a=>!a.StartsWith("--")) ?? "239.1.2.3";
    uint ip=IPv4(group);
    if((ip & 0xf0000000)!=0xe0000000) throw new ArgumentException("Not IPv4 multicast");
    Console.WriteLine($"group={group} ethernet=01:00:5e:{(ip>>16)&0x7f:x2}:{(ip>>8)&0xff:x2}:{ip&0xff:x2}");
    Console.WriteLine("membership iface=12 group=239.1.2.3 receiver-port=5004");
    Console.WriteLine(fault?"incoming iface=18; membership mismatch; app received=0":"incoming iface=12; app received=50 datagrams/s");
    Check((IPv4("239.1.2.3") & 0x7fffff)==(IPv4("239.129.2.3") & 0x7fffff),"two groups share MAC mapping");
    break;
}
case "mtu": {
    int mtu = args.Skip(1).FirstOrDefault(a=>!a.StartsWith("--")) is string x ? int.Parse(x) : 1500;
    Console.WriteLine($"IPv4-no-options mtu={mtu} TCP-payload-max={mtu-40} UDP-payload-max={mtu-28} ping-data-max={mtu-28}");
    int payload=2000, fragmentData=((mtu-20)/8)*8;
    if (fragmentData<=0) throw new ArgumentException("MTU too small");
    for(int offset=0;offset<payload;offset+=fragmentData) {
        int data=Math.Min(fragmentData,payload-offset);
        Console.WriteLine($"fragment offset-units={offset/8} data={data} ip-total={data+20} MF={offset+data<payload}");
    }
    Console.WriteLine("serialization 1500 bytes at 10Mbit/s = 1.2ms (IP length only)");
    break;
}
case "metrics": {
    double[] rtt={20,21,19,22,200};
    double[] sorted=rtt.Order().ToArray();
    Console.WriteLine($"rtt-mean={rtt.Average():F1}ms median={sorted[2]}ms maximum={sorted[^1]}ms");
    Console.WriteLine("received=980 of sent=1000; loss=2.00% (matching IDs assumed)");
    Console.WriteLine($"goodput={8*8.0/10:F2}Mbit/s (8MB completed in 10 seconds)");
    Console.WriteLine("BDP=20Mbit/s*0.05s/8=125000 bytes");
    Console.WriteLine("window-limit=65536*8/0.05=10.48576Mbit/s");
    Check(Math.Abs(rtt.Average()-56.4)<0.001,"mean includes tail event");
    break;
}
case "diagnose": {
    Console.WriteLine("Q31 connect=OK TX=8 bytes peer-ACK=YES application=TIMEOUT");
    Console.WriteLine(fault?"SERVER read=3 parser-rejected-before-message-complete":"SERVER read=3+5 parser=STATUS? LF reply=OK,READY");
    Console.WriteLine(fault?"NEXT: repair incremental parser; recheck both fragmented and joined input":"VERIFY: request-id matched, application result READY, repeated 20 times");
    break;
}
default: throw new ArgumentException("Unknown mode "+mode);
}
record Route(string Network,int Prefix,string NextHop,int Metric);
record Flow(string Protocol,string Src,int SrcPort,string Dst,int DstPort);
