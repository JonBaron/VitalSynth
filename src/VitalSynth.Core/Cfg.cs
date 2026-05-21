namespace VitalSynth.Core;

/// <summary>
/// Mutable configuration shared across the session. Treat it as the
/// synth's "front panel" — knobs a client is free to wiggle at runtime.
/// <para>
/// Numeric knobs are typed with their physical SI unit where one exists
/// (<see cref="Volt"/>, <see cref="Hertz"/>, <see cref="TimeSpan"/>),
/// otherwise with their clinical domain unit (<see cref="BeatsPerMinute"/>,
/// <see cref="Percent"/>, <see cref="Celsius"/>). A few raw <see cref="double"/>s
/// remain for dimensionless tuning constants where wrapping them would only
/// add noise.
/// </para>
/// </summary>
public sealed class Cfg
{
    public BeatsPerMinute HeartRate       = new(72);
    public Percent        HrJitter        = new(3);
    public Volt           QrsAmplitude    = Volt.FromMillivolts(1.2);
    public BeatsPerMinute RespiratoryRate = new(14);
    public double         RespDepth       = 0.04;        // dimensionless 0..1 ratio
    public Percent        Spo2Base        = new(98);
    public double         Spo2Drift       = 0.3;         // dimensionless drift constant
    public Celsius        TempBase        = new(36.8);
    public double         TempDrift       = 0.05;        // dimensionless drift constant
    public Volt           NoiseAmplitude  = Volt.FromMillivolts(0.015);
    public Hertz          SampleRate      = new(250);
    public Scenario       Scenario        = Scenario.Normal;
    public TimeSpan       Hl7Interval     = TimeSpan.FromSeconds(1);
}
