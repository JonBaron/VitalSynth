using System.Diagnostics;
using VitalSynth.Core;

namespace VitalSynth.Cli;

public static class Program
{
    // Optional HL7 MLLP endpoint. Leave host empty to skip network I/O.
    private const string Hl7Host = "";
    private const int    Hl7Port = 2575;

    public static void Main()
    {
        var cfg  = new Cfg();
        var sink = new Hl7MllpSink(Hl7Host, Hl7Port);
        using var session = new Session(cfg, sink);

        var scope = new Scope(Math.Min(Console.WindowWidth - 1, 100));
        var samplesPerFrame = cfg.SampleRateHz / 30;
        var buffer = new double[samplesPerFrame];

        Console.CursorVisible = false;
        Console.Clear();

        var  clock       = Stopwatch.StartNew();
        long nextFrameMs = 0;
        var  paused      = false;

        while (true)
        {
            if (!paused)
            {
                session.Step(buffer);
                scope.Push(buffer);
            }

            scope.Render(cfg.Scenario, session.CurrentBpm, session.CurrentSpo2, session.CurrentTempC, paused);

            if (Console.KeyAvailable)
            {
                if (HandleKey(cfg, Console.ReadKey(true).KeyChar, ref paused) == KeyAction.Quit)
                    break;
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
