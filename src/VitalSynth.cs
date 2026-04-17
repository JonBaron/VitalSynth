// VitalSynth — a synthesizer for vital signs.
// Single-file C# 14 / .NET 10 mock. Not medical software.

using System.Diagnostics;
using System.Globalization;
using System.Net.Sockets;
using System.Text;

namespace VitalSynth;

public enum Scenario { Normal, Tachycardia, Bradycardia, AFib, Desaturation, Asystole }

public sealed class Cfg
{
    public double   Bpm           = 72;      // base heart rate
    public double   HrVar         = 3;       // ± % beat-to-beat jitter
    public double   QrsMv         = 1.2;     // R-peak amplitude (mV)
    public double   RespRpm       = 14;      // breaths per minute
    public double   RespDepth     = 0.04;    // respiratory HR modulation
    public double   Spo2Base      = 98;      // %
    public double   Spo2Drift     = 0.3;
    public double   TempBase      = 36.8;    // °C
    public double   TempDrift     = 0.05;
    public double   Noise         = 0.015;   // baseline noise (mV)
    public int      SampleRateHz  = 250;
    public Scenario Scenario      = Scenario.Normal;
    public string   Hl7Host       = "";      // empty = console only
    public int      Hl7Port       = 2575;
    public int      Hl7IntervalMs = 1000;
}


/// <summary>
/// Synthesizes an ECG P-QRS-T complex from a small Fourier series.
/// QRS is reconstructed as a 6-harmonic stack windowed by a super-Gaussian.
/// </summary>
public sealed class HeartOscillator(Cfg cfg)
{
    private const double PStart = 0.10, PEnd = 0.18;
    private const double QStart = 0.22, QEnd = 0.30;
    private const double TStart = 0.38, TEnd = 0.55;

    private readonly Random _rng = new(0xEC6);
    private double _beatPhase;
    private double _rrSec = 60.0 / cfg.Bpm;
    private double _lastBpm = cfg.Bpm;

    public int CurrentBpm => cfg.Scenario switch
    {
        Scenario.Asystole => 0,
        Scenario.AFib     => 95 + _rng.Next(-15, 25),
        _                 => (int)Math.Round(_lastBpm),
    };

    public double NextSample(double dt, double respModulation)
    {
        _lastBpm = EffectiveBpm() * (1 + respModulation * cfg.RespDepth);
        _beatPhase += dt / _rrSec;

        if (_beatPhase >= 1)
        {
            _beatPhase -= 1;
            var jitter = (_rng.NextDouble() - 0.5) * cfg.HrVar / 50;
            _rrSec = 60.0 / Math.Max(_lastBpm, 1) * (1 + jitter);
            if (cfg.Scenario is Scenario.AFib)
                _rrSec *= 1 + (_rng.NextDouble() - 0.5) * 0.35;
        }

        if (cfg.Scenario is Scenario.Asystole)
            return GaussianNoise() * cfg.Noise;

        return cfg.QrsMv * (PWave(_beatPhase) + QrsWave(_beatPhase) + TWave(_beatPhase))
             + GaussianNoise() * cfg.Noise;
    }

    private double EffectiveBpm() => cfg.Scenario switch
    {
        Scenario.Tachycardia => Math.Max(cfg.Bpm, 140),
        Scenario.Bradycardia => Math.Min(cfg.Bpm, 42),
        Scenario.AFib        => 95 + 20 * Math.Sin(Environment.TickCount / 4000.0),
        _                    => cfg.Bpm,
    };

    private double GaussianNoise()
    {
        var u1 = 1 - _rng.NextDouble();
        var u2 = 1 - _rng.NextDouble();
        return Math.Sqrt(-2 * Math.Log(u1)) * Math.Cos(2 * Math.PI * u2);
    }

    private static double PWave(double p)
    {
        if (p < PStart || p > PEnd) return 0;
        var x = (p - PStart) / (PEnd - PStart);
        return 0.12 * (0.5 - 0.5 * Math.Cos(2 * Math.PI * x));
    }

    private static double TWave(double p)
    {
        if (p < TStart || p > TEnd) return 0;
        var x = (p - TStart) / (TEnd - TStart);
        return 0.35 * Math.Sin(Math.PI * x);
    }

    private static double QrsWave(double p)
    {
        if (p < QStart || p > QEnd) return 0;
        var x = (p - QStart) / (QEnd - QStart);

        var harmonics = 0.0;
        for (var k = 1; k <= 6; k++)
            harmonics += Math.Sin(k * Math.PI * x) / k;

        var env = Math.Exp(-Math.Pow((x - 0.5) / 0.08, 2));
        var q   = -0.15 * Math.Exp(-Math.Pow((x - 0.25) / 0.05, 2));
        var s   = -0.25 * Math.Exp(-Math.Pow((x - 0.75) / 0.06, 2));
        return env * (0.6 + 0.4 * harmonics) + q + s;
    }
}


/// <summary>
/// Slow-moving signals: respiration LFO, SpO2 drift, temperature drift.
/// </summary>
public sealed class SlowSignals(Cfg cfg)
{
    private readonly Random _rng = new(0x5B02);
    private readonly Stopwatch _clock = Stopwatch.StartNew();
    private double _respPhase;
    private double _spo2Walk;
    private double _tempWalk;

    public double Respiration(double dt)
    {
        _respPhase += 2 * Math.PI * cfg.RespRpm / 60 * dt;
        return Math.Sin(_respPhase);
    }

    public double Spo2(double dt)
    {
        _spo2Walk = Math.Clamp(_spo2Walk + (_rng.NextDouble() - 0.5) * cfg.Spo2Drift * dt, -1.5, 1.5);
        var value = cfg.Spo2Base + _spo2Walk;
        if (cfg.Scenario is Scenario.Desaturation)
            value -= 8 * (1 - Math.Exp(-_clock.Elapsed.TotalSeconds / 15));
        return Math.Clamp(value, 70, 100);
    }

    public double Temperature(double dt)
    {
        _tempWalk += (_rng.NextDouble() - 0.5) * cfg.TempDrift * dt;
        return cfg.TempBase + _tempWalk;
    }
}


/// <summary>
/// HL7 v2 ORU^R01 formatter with MLLP framing over TCP.
/// Minimal and illustrative — not a full HL7 implementation.
/// </summary>
public sealed class Hl7Writer(string host, int port) : IDisposable
{
    private readonly TcpClient? _tcp = TryConnect(host, port);

    public void Send(int hr, double spo2, double tempC)
    {
        if (_tcp is not { Connected: true }) return;
        try { _tcp.GetStream().Write(Frame(Format(hr, spo2, tempC))); } catch { }
    }

    public void Dispose() => _tcp?.Close();

    private static string Format(int hr, double spo2, double tempC)
    {
        var ts   = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var ctrl = Guid.NewGuid().ToString("N")[..10];
        var inv  = CultureInfo.InvariantCulture;

        string[] segments =
        [
            $"MSH|^~\\&|VITALSYNTH|DEVROOM|EMR|HOSPITAL|{ts}||ORU^R01|{ctrl}|P|2.5",
            $"PID|1||SYN-000001^^^VITALSYNTH||Doe^Synthetic||19700101|M",
            $"OBR|1|{ctrl}|{ctrl}|VITALS^Vital Signs|||{ts}",
            $"OBX|1|NM|HR^Heart Rate|1|{hr}|bpm|60-100||||F",
            $"OBX|2|NM|SPO2^Oxygen Saturation|1|{spo2.ToString("0.0", inv)}|%|95-100||||F",
            $"OBX|3|NM|TEMP^Temperature|1|{tempC.ToString("0.00", inv)}|Cel|36.1-37.5||||F",
        ];
        return string.Join('\r', segments) + '\r';
    }

    private static byte[] Frame(string msg) =>
        [0x0B, .. Encoding.ASCII.GetBytes(msg), 0x1C, 0x0D];

    private static TcpClient? TryConnect(string host, int port)
    {
        if (string.IsNullOrWhiteSpace(host)) return null;
        try { return new TcpClient(host, port); } catch { return null; }
    }
}


/// <summary>
/// Circular ECG buffer with a tiny console oscilloscope renderer.
/// </summary>
public sealed class Scope(int width)
{
    private const int Height = 10;

    private readonly double[] _buffer = new double[width];
    private int _cursor;

    public void Push(double mv)
    {
        _buffer[_cursor] = mv;
        _cursor = (_cursor + 1) % _buffer.Length;
    }

    public void Render(Scenario scenario, int hr, double spo2, double tempC, bool paused)
    {
        var line = new char[_buffer.Length];
        var status = paused ? "PAUSED" : scenario.ToString();

        Console.SetCursorPosition(0, 0);
        Console.WriteLine($" VitalSynth | {status,-16} HR {hr,3}  SpO2 {spo2,5:0.0}%  T {tempC,5:0.00}C  ");
        Console.WriteLine(new string('─', _buffer.Length));

        for (var y = 0; y < Height; y++)
        {
            Array.Fill(line, ' ');
            for (var x = 0; x < _buffer.Length; x++)
            {
                var v  = _buffer[(x + _cursor) % _buffer.Length];
                var py = (int)Math.Round((Height - 1) * (1 - Math.Clamp((v + 0.5) / 2, 0, 1)));
                if (py == y) line[x] = '*';
            }
            Console.WriteLine(new string(line));
        }

        Console.WriteLine(new string('─', _buffer.Length));
        Console.WriteLine(" [1] Normal [2] Tachy [3] Brady [4] AFib [5] Desat [6] Asystole  [±] BPM  [space] pause  [q] quit ");
    }
}


public static class Program
{
    public static void Main()
    {
        var cfg   = new Cfg();
        var heart = new HeartOscillator(cfg);
        var slow  = new SlowSignals(cfg);
        var scope = new Scope(Math.Min(Console.WindowWidth - 1, 100));
        using var hl7 = new Hl7Writer(cfg.Hl7Host, cfg.Hl7Port);

        var dt              = 1.0 / cfg.SampleRateHz;
        var samplesPerFrame = cfg.SampleRateHz / 30;
        var samplesPerHl7   = cfg.SampleRateHz * cfg.Hl7IntervalMs / 1000;

        Console.CursorVisible = false;
        Console.Clear();

        var  clock       = Stopwatch.StartNew();
        long nextFrameMs = 0;
        var  hl7Counter  = 0;
        var  paused      = false;

        while (true)
        {
            if (!paused)
            {
                for (var i = 0; i < samplesPerFrame; i++)
                {
                    scope.Push(heart.NextSample(dt, slow.Respiration(dt)));

                    if (++hl7Counter >= samplesPerHl7)
                    {
                        hl7Counter = 0;
                        hl7.Send(heart.CurrentBpm, slow.Spo2(1), slow.Temperature(1));
                    }
                }
            }

            scope.Render(cfg.Scenario, heart.CurrentBpm, slow.Spo2(0), slow.Temperature(0), paused);

            if (Console.KeyAvailable)
            {
                var action = HandleKey(cfg, Console.ReadKey(true).KeyChar, ref paused);
                if (action == KeyAction.Quit) break;
            }

            nextFrameMs += 1000 / 30;
            var sleep = (int)(nextFrameMs - clock.ElapsedMilliseconds);
            if (sleep > 0) Thread.Sleep(sleep);
        }

        Console.CursorVisible = true;
    }

    private enum KeyAction { None, Quit }

    private static KeyAction HandleKey(Cfg cfg, char key, ref bool paused)
    {
        switch (key)
        {
            case '1': cfg.Scenario = Scenario.Normal;       break;
            case '2': cfg.Scenario = Scenario.Tachycardia;  break;
            case '3': cfg.Scenario = Scenario.Bradycardia;  break;
            case '4': cfg.Scenario = Scenario.AFib;         break;
            case '5': cfg.Scenario = Scenario.Desaturation; break;
            case '6': cfg.Scenario = Scenario.Asystole;     break;
            case '+': cfg.Bpm = Math.Min(220, cfg.Bpm + 5); break;
            case '-': cfg.Bpm = Math.Max( 25, cfg.Bpm - 5); break;
            case ' ': paused = !paused;                     break;
            case 'q' or 'Q': return KeyAction.Quit;
        }
        return KeyAction.None;
    }
}
