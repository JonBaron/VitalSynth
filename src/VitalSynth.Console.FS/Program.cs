using System.Diagnostics;
using VitalSynth.Core.FS;

namespace VitalSynth.Cli.FS;

// Side-by-side counterpart of VitalSynth.Console, but consuming the F# core.
// Compare with src/VitalSynth.Console/Program.cs to see the friction at the
// C# ⇄ F# boundary:
//   - F# units of measure are a compile-time fiction — `cfg.HeartRate` is just
//     a `double` here. Type safety is lost crossing back into C#.
//   - F# discriminated union cases with payloads come through as factory
//     methods (`Scenario.NewAFib(jitter)`, `Scenario.NewDesaturation(onset)`).
//   - Cases without payload are static properties (`Scenario.Normal`).
//   - F# records are usable as ordinary C# records.
public static class Program
{
    public static void Main()
    {
        var cfg = Cfg.Defaults();
        using var session = new Session(cfg);

        // Subscribe to HL7 publication. Hl7.format is a pure F# function.
        session.VitalsPublished += (sender, v) =>
        {
            _ = Hl7.format(v); // wire-up point — could write to TCP, file, …
        };

        var scope = new Scope(Math.Min(Console.WindowWidth - 1, 100));
        var samplesPerFrame = (int)(cfg.SampleRate / 30);
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

            scope.Render(cfg.Scenario,
                         session.CurrentHeartRate,
                         session.CurrentSpo2,
                         session.CurrentTempC,
                         paused);

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
            // DU cases without payload look like static properties to C#.
            case '1': cfg.Scenario = Scenario.Normal;                        break;
            case '2': cfg.Scenario = Scenario.Tachycardia;                   break;
            case '3': cfg.Scenario = Scenario.Bradycardia;                   break;
            // DU cases with payload come through as `NewCase(payload)` factories.
            case '4': cfg.Scenario = Scenario.NewAFib(5.0);                  break;
            case '5': cfg.Scenario = Scenario.NewDesaturation(0.0);          break;
            case '6': cfg.Scenario = Scenario.Asystole;                      break;
            // Units of measure are gone — back to plain double clamping.
            case '+': cfg.HeartRate = Math.Min(220, cfg.HeartRate + 5);      break;
            case '-': cfg.HeartRate = Math.Max( 25, cfg.HeartRate - 5);      break;
            case ' ': paused = !paused;                                      break;
            case 'q' or 'Q': return KeyAction.Quit;
        }
        return KeyAction.None;
    }
}
