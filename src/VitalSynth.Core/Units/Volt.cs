using System.Globalization;

namespace VitalSynth.Core;

/// <summary>
/// Electric potential in volts (SI base unit). The value is always stored
/// in volts; sub-/superunits are expressed through factory methods and
/// projection properties (<see cref="FromMillivolts"/>, <see cref="InMillivolts"/>, …).
/// <para>
/// This makes <c>Volt.FromMillivolts(1) + new Volt(1)</c> well-defined:
/// 0.001 V + 1.000 V = 1.001 V, regardless of how the operands were typed in.
/// </para>
/// </summary>
public readonly record struct Volt : IFormattable
{
    /// <summary>The underlying value in volts.</summary>
    public double Value { get; }

    public Volt(double volts)
    {
        if (!double.IsFinite(volts))
            throw new ArgumentOutOfRangeException(
                nameof(volts), volts, "Volt must be a finite number.");
        Value = volts;
    }

    public static Volt FromMillivolts(double mv) => new(mv / 1_000d);
    public static Volt FromKilovolts (double kv) => new(kv * 1_000d);

    public double InMillivolts => Value * 1_000d;
    public double InKilovolts  => Value / 1_000d;

    // ── Named operations (CLS-compliant, BCL convention) ──────────
    public static Volt Add     (Volt a, Volt b)        => new(a.Value + b.Value);
    public static Volt Subtract(Volt a, Volt b)        => new(a.Value - b.Value);
    public static Volt Negate  (Volt v)                => new(-v.Value);
    public static Volt Scale   (Volt v, double scalar) => new(v.Value * scalar);

    // ── Operator shortcuts (thin sugar over the methods above) ────
    public static Volt operator + (Volt a, Volt b)        => Add(a, b);
    public static Volt operator - (Volt a, Volt b)        => Subtract(a, b);
    public static Volt operator - (Volt v)                => Negate(v);
    public static Volt operator * (Volt v,   double s)    => Scale(v, s);
    public static Volt operator * (double s, Volt v)      => Scale(v, s);

    public override string ToString() =>
        Value.ToString(CultureInfo.InvariantCulture);

    public string ToString(string? format) =>
        Value.ToString(format, CultureInfo.InvariantCulture);

    public string ToString(string? format, IFormatProvider? formatProvider) =>
        Value.ToString(format, formatProvider);
}
