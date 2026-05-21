using System.Globalization;

namespace VitalSynth.Core;

/// <summary>
/// Frequency in hertz (SI). The value is always stored in Hz; multipliers
/// (kHz, MHz) are factory methods and projection properties.
/// </summary>
public readonly record struct Hertz : IFormattable
{
    public const double MaxValue = 1_000_000_000d;   // 1 GHz, generous upper bound

    /// <summary>The underlying value in Hz.</summary>
    public double Value { get; }

    public Hertz(double hz)
    {
        if (!double.IsFinite(hz) || hz <= 0 || hz > MaxValue)
            throw new ArgumentOutOfRangeException(
                nameof(hz), hz, $"Hertz must be in (0, {MaxValue}].");
        Value = hz;
    }

    public static Hertz FromKilohertz(double khz) => new(khz * 1_000d);
    public static Hertz FromMegahertz(double mhz) => new(mhz * 1_000_000d);

    public double InKilohertz => Value / 1_000d;
    public double InMegahertz => Value / 1_000_000d;

    public override string ToString() =>
        Value.ToString(CultureInfo.InvariantCulture);

    public string ToString(string? format) =>
        Value.ToString(format, CultureInfo.InvariantCulture);

    public string ToString(string? format, IFormatProvider? formatProvider) =>
        Value.ToString(format, formatProvider);
}
