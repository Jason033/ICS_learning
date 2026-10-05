using System.Text;

string mode = args.FirstOrDefault() ?? "frames";
bool showFault = args.Contains("--fault");

switch (mode)
{
    case "layers":
        RunLayers(showFault);
        break;
    case "uart":
        RunUart();
        break;
    case "frames":
        RunFrames(showFault);
        break;
    case "timeout":
        RunTimeout();
        break;
    case "events":
        RunEvents();
        break;
    case "interfaces":
        RunInterfaces();
        break;
    case "diagnose":
        RunDiagnosis(showFault);
        break;
    default:
        throw new ArgumentException($"Unknown mode: {mode}");
}

void RunLayers(bool omitTerminator)
{
    string command = omitTerminator ? "STATUS?" : "STATUS?\r\n";
    byte[] transmittedBytes = Encoding.ASCII.GetBytes(command);

    Console.WriteLine($"COM-open=OK write={transmittedBytes.Length} HEX={Convert.ToHexString(transmittedBytes)}");

    bool hasLineTerminator = transmittedBytes.TakeLast(2).SequenceEqual(new byte[] { 13, 10 });
    Console.WriteLine(hasLineTerminator
        ? "Lab message complete; response=OK,READY"
        : "UART bytes available; message missing CR LF");
}

void RunUart()
{
    const int baud = 9600;
    const int dataBits = 8;
    const int parityBits = 0;
    const int stopBits = 1;

    int bitsPerCharacter = 1 + dataBits + parityBits + stopBits;
    double bitTimeMicroseconds = 1_000_000.0 / baud;
    double characterTimeMilliseconds = bitsPerCharacter * 1000.0 / baud;

    Console.WriteLine(
        $"8N1 bitsPerChar={bitsPerCharacter} bitTimeUs={bitTimeMicroseconds:F6} charTimeMs={characterTimeMilliseconds:F6}");
    Console.WriteLine(
        $"request9BytesMs={WireTimeMilliseconds(9, bitsPerCharacter, baud):F6} " +
        $"response10BytesMs={WireTimeMilliseconds(10, bitsPerCharacter, baud):F6}");

    byte sampleByte = 0x53;
    string leastSignificantBitFirst = string.Concat(
        Enumerable.Range(0, 8).Select(bitIndex => ((sampleByte >> bitIndex) & 1).ToString()));
    int evenParityBit = System.Numerics.BitOperations.PopCount((uint)sampleByte) % 2;

    Console.WriteLine($"byte=53 lsbFirst={leastSignificantBitFirst} evenParity={evenParityBit}");
}

void RunFrames(bool introduceFault)
{
    var parser = new LabParser();
    byte[][] chunks = introduceFault
        ? new[]
        {
            new byte[] { 0, 0xaa, 0xff, 0xaa, 2, 0x90, 1, 0x92 },
            new byte[] { 0xaa, 2, 0x90, 1, 0x93 }
        }
        : new[]
        {
            new byte[] { 0xaa, 2, 0x90 },
            new byte[] { 1, 0x93, 0xaa, 1, 0x10, 0x11 }
        };

    foreach (byte[] chunk in chunks)
    {
        Console.WriteLine("RX " + Convert.ToHexString(chunk));
        parser.Feed(chunk);
    }

    Console.WriteLine($"complete={parser.Completed} buffered={parser.Buffered}");
    Check(parser.Completed == (introduceFault ? 1 : 2), "expected valid frame count");
    Check(parser.Buffered == 0, "bounded parser drains known input");
}

void RunTimeout()
{
    const int deadlineMilliseconds = 200;
    var receivedChunks = new[]
    {
        (AtMilliseconds: 10, Hex: "AA0290"),
        (AtMilliseconds: 210, Hex: "0193")
    };

    foreach (var chunk in receivedChunks)
    {
        bool arrivedBeforeDeadline = chunk.AtMilliseconds <= deadlineMilliseconds;
        Console.WriteLine(
            $"t={chunk.AtMilliseconds} RX={chunk.Hex} withinDeadline={arrivedBeforeDeadline}");
    }

    Console.WriteLine(
        "t=200 result=INCOMPLETE 3/5; t=210 late bytes are not a new transaction success");
}

void RunEvents()
{
    int[] callbackChunkSizes = { 3, 0, 2 };
    int totalBytesRead = 0;

    foreach (int bytesRead in callbackChunkSizes)
    {
        totalBytesRead += bytesRead;
        Console.WriteLine(
            $"DataAvailable chunk={bytesRead} total={totalBytesRead} complete={totalBytesRead >= 5}");
    }

    Check(totalBytesRead == 5, "callback count is not byte or message count");
}

void RunInterfaces()
{
    const int baud = 19200;
    const int bitsPerCharacter = 10;
    const int transmittedBytes = 20;
    const int receivedBytes = 12;
    const double deviceProcessingMilliseconds = 5;
    const double directionChangeMilliseconds = 2;

    double transmitMilliseconds = WireTimeMilliseconds(transmittedBytes, bitsPerCharacter, baud);
    double receiveMilliseconds = WireTimeMilliseconds(receivedBytes, bitsPerCharacter, baud);
    double totalMilliseconds = transmitMilliseconds + receiveMilliseconds
        + deviceProcessingMilliseconds + directionChangeMilliseconds;

    Console.WriteLine(
        $"halfduplex txMs={transmitMilliseconds:F3} rxMs={receiveMilliseconds:F3} " +
        $"workMs={deviceProcessingMilliseconds} turnaroundMs={directionChangeMilliseconds} totalMs={totalMilliseconds:F3}");
    Console.WriteLine("one driver at a time; UART framing does not arbitrate bus ownership");
}

void RunDiagnosis(bool omitTerminator)
{
    Console.WriteLine("Lab device: 9600,8,N,1; ASCII CR LF; no flow control");
    Console.WriteLine(omitTerminator
        ? "APP TX=5354415455533F; device parser waiting for CR LF"
        : "APP TX=5354415455533F0D0A; reply=4F4B2C52454144590D0A");
    Console.WriteLine("Next validation includes split bytes, two frames, corrupt checksum and deadline.");
}

double WireTimeMilliseconds(int bytes, int bitsPerCharacter, int bitsPerSecond)
{
    return bytes * bitsPerCharacter * 1000.0 / bitsPerSecond;
}

void Check(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }

    Console.WriteLine("PASS " + message);
}

sealed class LabParser
{
    private readonly List<byte> _pending = new();

    public int Completed { get; private set; }
    public int Buffered => _pending.Count;

    // Teaching frame: AA, LEN, CMD, DATA..., XOR. LEN counts CMD and DATA only (1..32).
    public void Feed(byte[] receivedBytes)
    {
        foreach (byte receivedByte in receivedBytes)
        {
            _pending.Add(receivedByte);

            while (_pending.Count > 0)
            {
                if (_pending[0] != 0xaa)
                {
                    Console.WriteLine("skip noise " + _pending[0].ToString("X2"));
                    _pending.RemoveAt(0);
                    continue;
                }

                if (_pending.Count < 2)
                {
                    break;
                }

                int payloadLength = _pending[1];
                if (payloadLength < 1 || payloadLength > 32)
                {
                    Console.WriteLine("invalid LEN=" + payloadLength);
                    _pending.RemoveAt(0);
                    continue;
                }

                int frameLength = payloadLength + 3;
                if (_pending.Count < frameLength)
                {
                    break;
                }

                byte expectedChecksum = 0;
                for (int index = 1; index < frameLength - 1; index++)
                {
                    expectedChecksum ^= _pending[index];
                }

                if (expectedChecksum != _pending[frameLength - 1])
                {
                    Console.WriteLine("checksum reject");
                    _pending.RemoveAt(0);
                    continue;
                }

                byte[] frame = _pending.Take(frameLength).ToArray();
                Console.WriteLine("FRAME " + Convert.ToHexString(frame));
                Completed++;
                _pending.RemoveRange(0, frameLength);
            }
        }
    }
}
