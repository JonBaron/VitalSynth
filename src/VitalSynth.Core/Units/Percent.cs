using System.Globalization;

namespace VitalSynth.Core;

/// <summary>
/// Ada-style subtype: <c>subtype Percent is Float range 0.0 .. 100.0</c>.
/// Construction throws <see cref="ArgumentOutOfRangeException"/> on out-of-range
/// or NaN input; arithmetic operators inherit the same check. Use
/// <see cref="Clamp"/> when you explicitly want lossy "best effort" conversion.
/// </summary>
public readonly record struct Percent : IFormattable
{
    public const double MinValue = 0;
    public const double MaxValue = 100;

    public double Value { get; }

    public Percent(double value)
    {
        if (double.IsNaN(value) || value < MinValue || value > MaxValue)
            throw new ArgumentOutOfRangeException(
                nameof(value), value, $"Percent must be in [{MinValue}, {MaxValue}].");
        Value = value;
    }

    /// <summary>Truncates any double to the legal range. NaN becomes 0.</summary>
    public static Percent Clamp(double value) =>
        new(double.IsNaN(value) ? MinValue : Math.Clamp(value, MinValue, MaxValue));

    // ── Named operations (CLS-compliant, BCL convention) ──────────
    public static Percent Add     (Percent a, Percent b) => new(a.Value + b.Value);
    public static Percent Subtract(Percent a, Percent b) => new(a.Value - b.Value);

    // ── Operator shortcuts (thin sugar over the methods above) ────
    public static Percent operator + (Percent a, Percent b) => Add(a, b);
    public static Percent operator - (Percent a, Percent b) => Subtract(a, b);

    public override string ToString() =>
        Value.ToString(CultureInfo.InvariantCulture);

    public string ToString(string? format) =>
        Value.ToString(format, CultureInfo.InvariantCulture);

    public string ToString(string? format, IFormatProvider? formatProvider) =>
        Value.ToString(format, formatProvider);
}
