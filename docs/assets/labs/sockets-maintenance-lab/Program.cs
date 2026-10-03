using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;
using System.Text;

// Independent teaching protocol, not a company API: 2-byte big-endian byte length,
// 1..1024 bytes strict UTF-8. No external hosts, services or SDKs.
var utf8 = new UTF8Encoding(false, true);
byte[] Encode(string text)
{
    byte[] body = utf8.GetBytes(text);
    if (body.Length is < 1 or > 1024) throw new InvalidDataException("length outside 1..1024");
    byte[] frame = new byte[2 + body.Length];
    BinaryPrimitives.WriteUInt16BigEndian(frame.AsSpan(0, 2), (ushort)body.Length);
    body.CopyTo(frame, 2);
    return frame;
}
void Check(bool condition, string name)
{
    if (!condition) throw new Exception("FAIL " + name);
    Console.WriteLine("PASS " + name);
}
// Deterministic segmentation, independent of how the OS divides reads.
byte[] combined = Encode("A").Concat(Encode("溫度")).ToArray();
int[] chunks = [1, 2, 3, 5];
var parser = new FrameParser();
var parsed = new List<string>();
int cursor = 0;
foreach (int size in chunks)
{
    var outputs = parser.Feed(combined.AsSpan(cursor, size));
    parsed.AddRange(outputs);
    Console.WriteLine($"feed={size} outputs={outputs.Count} retained={parser.Retained}");
    cursor += size;
}
parser.End();
Check(parsed.SequenceEqual(new[] { "A", "溫度" }), "fragmented and coalesced UTF-8 frames");
Check(Encode("溫度").Length == 8, "2 characters = 6 UTF-8 bytes + 2 header");
void ExpectFailure(byte[] bytes, bool end, string name)
{
    try { var p = new FrameParser(); p.Feed(bytes); if (end) p.End(); }
    catch (Exception ex) when (ex is InvalidDataException or DecoderFallbackException or EndOfStreamException)
    { Console.WriteLine($"PASS {name}: {ex.GetType().Name}"); return; }
    throw new Exception("FAIL expected " + name);
}
ExpectFailure([0x04, 0x01], false, "1025-byte length rejected before body allocation");
ExpectFailure([0x00], true, "EOF inside header");
ExpectFailure([0x00, 0x03, 0x41], true, "EOF inside body");
ExpectFailure([0x00, 0x01, 0xFF], false, "invalid UTF-8 rejected");

using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(10));
CancellationToken ct = deadline.Token;
using var listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
listener.Listen(4);
var endpoint = (IPEndPoint)listener.LocalEndPoint!;
Console.WriteLine($"LISTEN local={endpoint} role=listener");
Task server = Serve();
using var client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
await client.ConnectAsync(endpoint, ct);
Console.WriteLine($"CONNECT local={client.LocalEndPoint} remote={client.RemoteEndPoint} generation=1");
await SendAll(client, Encode("REQ r41 STATUS"), ct);
client.Shutdown(SocketShutdown.Send); // Outbound EOF; incoming response is still readable.
string? response = await ReadFrame(client, ct);
Check(response == "RSP r41 READY", "application response after client half-close");
Check(await ReadFrame(client, ct) is null, "EOF exactly between frames");
await server;

// UDP preserves a message boundary. A zero-length datagram is data, not TCP EOF.
using var udpReceiver = new UdpClient(new IPEndPoint(IPAddress.Loopback, 0));
using var udpSender = new UdpClient(new IPEndPoint(IPAddress.Loopback, 0));
var udpEndpoint = (IPEndPoint)udpReceiver.Client.LocalEndPoint!;
await udpSender.SendAsync(ReadOnlyMemory<byte>.Empty, udpEndpoint, ct);
var empty = await udpReceiver.ReceiveAsync(ct);
Check(empty.Buffer.Length == 0, "zero-length UDP datagram retains sender endpoint");
await udpSender.SendAsync(new byte[] { 1, 2, 3 }, udpEndpoint, ct);
var data = await udpReceiver.ReceiveAsync(ct);
Check(data.Buffer.SequenceEqual(new byte[] { 1, 2, 3 }), "next UDP datagram independently delivered");
using var cancelled = new CancellationTokenSource();
cancelled.Cancel();
try { await udpReceiver.ReceiveAsync(cancelled.Token); throw new Exception("missing cancellation"); }
catch (OperationCanceledException) { Console.WriteLine("PASS pre-cancelled pending UDP receive"); }
Console.WriteLine("PASS all deterministic parser and loopback transport checks");

async Task Serve()
{
    using Socket accepted = await listener.AcceptAsync(ct);
    Console.WriteLine($"ACCEPT local={accepted.LocalEndPoint} remote={accepted.RemoteEndPoint} role=connected");
    string? request = await ReadFrame(accepted, ct);
    Check(request == "REQ r41 STATUS", "server parsed request");
    Check(await ReadFrame(accepted, ct) is null, "client half-close observed after request");
    Console.WriteLine("PARSE request=r41 status=accepted");
    await SendAll(accepted, Encode("RSP r41 READY"), ct);
    Console.WriteLine("COMPLETE request=r41 result=READY");
    accepted.Shutdown(SocketShutdown.Send);
}
async Task SendAll(Socket socket, byte[] bytes, CancellationToken token)
{
    int offset = 0;
    while (offset < bytes.Length)
    {
        int n = await socket.SendAsync(bytes.AsMemory(offset), SocketFlags.None, token);
        if (n <= 0) throw new IOException("send made no progress");
        offset += n;
        Console.WriteLine($"SEND accepted={n} cumulative={offset}/{bytes.Length}");
    }
}
async Task<string?> ReadFrame(Socket socket, CancellationToken token)
{
    byte[] header = new byte[2];
    if (!await ReadExact(socket, header, true, token)) return null;
    int length = BinaryPrimitives.ReadUInt16BigEndian(header);
    if (length is < 1 or > 1024) throw new InvalidDataException("length outside 1..1024");
    byte[] body = new byte[length];
    await ReadExact(socket, body, false, token);
    return utf8.GetString(body);
}
async Task<bool> ReadExact(Socket socket, byte[] buffer, bool allowCleanEof, CancellationToken token)
{
    int offset = 0;
    while (offset < buffer.Length)
    {
        int n = await socket.ReceiveAsync(buffer.AsMemory(offset), SocketFlags.None, token);
        Console.WriteLine($"READ count={n} cumulative={offset+n}/{buffer.Length}");
        if (n == 0)
        {
            if (offset == 0 && allowCleanEof) return false;
            throw new EndOfStreamException($"EOF at {offset}/{buffer.Length}");
        }
        offset += n;
    }
    return true;
}

sealed class FrameParser
{
    private readonly List<byte> pending = new();
    private readonly UTF8Encoding utf8 = new(false, true);
    public int Retained => pending.Count;
    public List<string> Feed(ReadOnlySpan<byte> chunk)
    {
        foreach (byte b in chunk) pending.Add(b);
        var output = new List<string>();
        while (pending.Count >= 2)
        {
            int length = (pending[0] << 8) | pending[1];
            if (length is < 1 or > 1024) throw new InvalidDataException("length outside 1..1024");
            if (pending.Count < 2 + length) break;
            byte[] body = pending.GetRange(2, length).ToArray();
            string text = utf8.GetString(body);
            pending.RemoveRange(0, 2 + length);
            output.Add(text);
        }
        return output;
    }
    public void End()
    {
        if (pending.Count != 0) throw new EndOfStreamException($"EOF with {pending.Count} unconsumed bytes");
    }
}
