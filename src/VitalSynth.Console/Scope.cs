using VitalSynth.Core;

namespace VitalSynth.Cli;

/// <summary>
/// Circular ECG buffer with a tiny console oscilloscope renderer.
/// Console-specific — not part of the reusable Core.
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
