using System.Globalization;

namespace VitalSynth.Core;

/// <summary>
/// Body temperature in degrees Celsius, Ada-style subtype with domain [20, 45].
/// <para>
/// Caveat: in C#, <c>default(Celsius)</c> bypasses the constructor and produces
/// a value of 0 — outside the legal domain. We accept this as a known limitation
/// of struct-based subtypes; in this codebase the type is always initialised via
/// <see cref="Cfg"/> or <see cref="Session"/> before it is read.
/// </para>
/// </summary>
public readonly record struct Celsius : IFormattable
{
    public const double MinValue = 20;
    public const double MaxValue = 45;

    public double Value { get; }

    public Celsius(double value)
    {
        if (double.IsNaN(value) || value < MinValue || value > MaxValue)
            throw new ArgumentOutOfRangeException(
                nameof(value), value, $"Celsius must be in [{MinValue}, {MaxValue}] °C.");
        Value = value;
    }

    public static Celsius Clamp(double value) =>
        new(double.IsNaN(value) ? MinValue : Math.Clamp(value, MinValue, MaxValue));

    public override string ToString() =>
        Value.ToString(CultureInfo.InvariantCulture);

    public string ToString(string? format) =>
        Value.ToString(format, CultureInfo.InvariantCulture);

    public string ToString(string? format, IFormatProvider? formatProvider) =>
        Value.ToString(format, formatProvider);
}
