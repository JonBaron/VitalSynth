using System.Diagnostics;

namespace VitalSynth.Core;

/// <summary>
/// Slow-moving signals: respiration LFO, SpO2 drift, temperature drift.
/// </summary>
public sealed class SlowSignals(Cfg cfg)
{
    private readonly Random _rng = new(0x5B02);
    private readonly Stopwatch _clock = Stopwatch.StartNew();
    private double _respPhase;
    private double _spo2Walk;
    private double _tempWalk;

    public double LastSpo2 { get; private set; } = cfg.Spo2Base;
    public double LastTempC { get; private set; } = cfg.TempBase;

    public double Respiration(double dt)
    {
        _respPhase += 2 * Math.PI * cfg.RespRpm / 60 * dt;
        return Math.Sin(_respPhase);
    }

    public double AdvanceSpo2(double dt)
    {
        _spo2Walk = Math.Clamp(_spo2Walk + (_rng.NextDouble() - 0.5) * cfg.Spo2Drift * dt, -1.5, 1.5);
        var value = cfg.Spo2Base + _spo2Walk;
        if (cfg.Scenario is Scenario.Desaturation)
            value -= 8 * (1 - Math.Exp(-_clock.Elapsed.TotalSeconds / 15));
        LastSpo2 = Math.Clamp(value, 70, 100);
        return LastSpo2;
    }

    public double AdvanceTemperature(double dt)
    {
        _tempWalk += (_rng.NextDouble() - 0.5) * cfg.TempDrift * dt;
        LastTempC = cfg.TempBase + _tempWalk;
        return LastTempC;
    }
}
