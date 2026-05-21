using VitalSynth.Core.FS;

namespace VitalSynth.Cli.FS;

/// <summary>
/// Same scope as VitalSynth.Console/Scope.cs, but the render signature has
/// fallen back to plain <c>double</c> — the F# units of measure don't survive
/// the boundary into C#.
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

    public void Push(ReadOnlySpan<double> samples)
    {
        foreach (var s in samples) Push(s);
    }

    public void Render(Scenario scenario, double hr, double spo2, double tempC, bool paused)
    {
        var line = new char[_buffer.Length];
        var status = paused ? "PAUSED" : ScenarioLabel(scenario);

        Console.SetCursorPosition(0, 0);
        Console.WriteLine($" VitalSynth FS | {status,-16} HR {hr,5:0}  SpO2 {spo2,5:0.0}%  T {tempC,5:0.00}C  ");
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

    // F# DUs need a little help to render nicely in C# — type-checks on each case.
    private static string ScenarioLabel(Scenario s) => s switch
    {
        _ when s.IsNormal        => "Normal",
        _ when s.IsTachycardia   => "Tachycardia",
        _ when s.IsBradycardia   => "Bradycardia",
        _ when s.IsAFib          => "AFib",
        _ when s.IsDesaturation  => "Desaturation",
        _ when s.IsAsystole      => "Asystole",
        _                        => s.ToString() ?? "?",
    };
}
