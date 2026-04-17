using System.Globalization;
using System.Net.Sockets;
using System.Text;

namespace VitalSynth.Core;

/// <summary>
/// Formats vital signs as HL7 v2 ORU^R01 messages and sends them over
/// MLLP/TCP. Not wire-compatible with Blazor WebAssembly — a web client
/// should plug in its own <see cref="IVitalsSink"/> (HTTP, WebSocket,
/// SignalR, …) instead.
/// </summary>
public sealed class Hl7MllpSink : IVitalsSink, IDisposable
{
    private readonly TcpClient? _tcp;

    public Hl7MllpSink(string host, int port)
    {
        _tcp = TryConnect(host, port);
    }

    public bool IsConnected => _tcp is { Connected: true };

    public void Publish(Vitals v)
    {
        if (!IsConnected) return;
        try { _tcp!.GetStream().Write(Frame(Format(v))); } catch { /* best-effort */ }
    }

    public void Dispose() => _tcp?.Close();

    public static string Format(Vitals v)
    {
        var ts   = v.TimestampUtc.ToString("yyyyMMddHHmmss");
        var ctrl = Guid.NewGuid().ToString("N")[..10];
        var inv  = CultureInfo.InvariantCulture;

        string[] segments =
        [
            $"MSH|^~\\&|VITALSYNTH|DEVROOM|EMR|HOSPITAL|{ts}||ORU^R01|{ctrl}|P|2.5",
            $"PID|1||SYN-000001^^^VITALSYNTH||Doe^Synthetic||19700101|M",
            $"OBR|1|{ctrl}|{ctrl}|VITALS^Vital Signs|||{ts}",
            $"OBX|1|NM|HR^Heart Rate|1|{v.HeartRate}|bpm|60-100||||F",
            $"OBX|2|NM|SPO2^Oxygen Saturation|1|{v.Spo2.ToString("0.0", inv)}|%|95-100||||F",
            $"OBX|3|NM|TEMP^Temperature|1|{v.TemperatureC.ToString("0.00", inv)}|Cel|36.1-37.5||||F",
        ];
        return string.Join('\r', segments) + '\r';
    }

    public static byte[] Frame(string msg) =>
        [0x0B, .. Encoding.ASCII.GetBytes(msg), 0x1C, 0x0D];

    private static TcpClient? TryConnect(string host, int port)
    {
        if (string.IsNullOrWhiteSpace(host)) return null;
        try { return new TcpClient(host, port); } catch { return null; }
    }
}
