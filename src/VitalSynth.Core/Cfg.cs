namespace VitalSynth.Core;

/// <summary>
/// Mutable configuration shared across the session. Treat it as the
/// synth's "front panel" — knobs a client is free to wiggle at runtime.
/// </summary>
public sealed class Cfg
{
    public double   Bpm           = 72;      // base heart rate
    public double   HrVar         = 3;       // ± % beat-to-beat jitter
    public double   QrsMv         = 1.2;     // R-peak amplitude (mV)
    public double   RespRpm       = 14;      // breaths per minute
    public double   RespDepth     = 0.04;    // respiratory HR modulation
    public double   Spo2Base      = 98;      // %
    public double   Spo2Drift     = 0.3;
    public double   TempBase      = 36.8;    // °C
    public double   TempDrift     = 0.05;
    public double   Noise         = 0.015;   // baseline noise (mV)
    public int      SampleRateHz  = 250;
    public Scenario Scenario      = Scenario.Normal;
    public int      Hl7IntervalMs = 1000;
}
