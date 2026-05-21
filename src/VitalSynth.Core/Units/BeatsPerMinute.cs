using System.Globalization;

namespace VitalSynth.Core;

/// <summary>
/// Beats per minute. Strictly speaking this is a non-SI domain unit (clinical
/// vitals are documented per minute, not per second), but a free
/// <see cref="InHertz"/> projection is provided for completeness.
/// <para>
/// Domain range is <c>[0, 300]</c>: 0 covers asystole, 300 is well above
/// any clinically credible sinus rate.
/// </para>
/// </summary>
public readonly record struct BeatsPerMinute : IFormattable
{
    public const int MinValue = 0;
    public const int MaxValue = 300;

    /// <summary>The underlying value in beats per minute.</summary>
    public int Value { get; }

    public BeatsPerMinute(int value)
    {
        if (value < MinValue || value > MaxValue)
            throw new ArgumentOutOfRangeException(
                nameof(value), value, $"BeatsPerMinute must be in [{MinValue}, {MaxValue}].");
        Value = value;
    }

    public static BeatsPerMinute Clamp(int value, int min = MinValue, int max = MaxValue) =>
        new(Math.Clamp(value, min, max));

    public static BeatsPerMinute FromHertz(double hz) =>
        new((int)Math.Round(hz * 60d));

    public double InHertz => Value / 60d;

    // ── Named operations (CLS-compliant, BCL convention) ──────────
    public static BeatsPerMinute Add     (BeatsPerMinute a, int delta) => new(a.Value + delta);
    public static BeatsPerMinute Subtract(BeatsPerMinute a, int delta) => new(a.Value - delta);

    // ── Operator shortcuts (thin sugar over the methods above) ────
    public static BeatsPerMinute operator + (BeatsPerMinute a, int delta) => Add(a, delta);
    public static BeatsPerMinute operator - (BeatsPerMinute a, int delta) => Subtract(a, delta);

    public override string ToString() =>
        Value.ToString(CultureInfo.InvariantCulture);

    public string ToString(string? format) =>
        Value.ToString(format, CultureInfo.InvariantCulture);

    public string ToString(string? format, IFormatProvider? formatProvider) =>
        Value.ToString(format, formatProvider);
}
