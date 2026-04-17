namespace VitalSynth.Core;

/// <summary>
/// Synthesizes an ECG P-QRS-T complex from a small Fourier series.
/// QRS is reconstructed as a 6-harmonic stack windowed by a super-Gaussian.
/// </summary>
public sealed class HeartOscillator(Cfg cfg)
{
    private const double PStart = 0.10, PEnd = 0.18;
    private const double QStart = 0.22, QEnd = 0.30;
    private const double TStart = 0.38, TEnd = 0.55;

    private const double RPeakPhase = 0.26;

    private readonly Random _rng = new(0xEC6);
    private double _beatPhase;
    private double _rrSec = 60.0 / cfg.Bpm;
    private double _lastBpm = cfg.Bpm;
    private bool _rPeakArmed = true;

    /// <summary>True for exactly one sample when the R-peak of a beat passes.</summary>
    public bool BeatThisSample { get; private set; }

    public int CurrentBpm => cfg.Scenario switch
    {
        Scenario.Asystole => 0,
        Scenario.AFib     => 95 + _rng.Next(-15, 25),
        _                 => (int)Math.Round(_lastBpm),
    };

    public double NextSample(double dt, double respModulation)
    {
        BeatThisSample = false;
        _lastBpm = EffectiveBpm() * (1 + respModulation * cfg.RespDepth);
        _beatPhase += dt / _rrSec;

        if (_beatPhase >= 1)
        {
            _beatPhase -= 1;
            _rPeakArmed = true;
            var jitter = (_rng.NextDouble() - 0.5) * cfg.HrVar / 50;
            _rrSec = 60.0 / Math.Max(_lastBpm, 1) * (1 + jitter);
            if (cfg.Scenario is Scenario.AFib)
                _rrSec *= 1 + (_rng.NextDouble() - 0.5) * 0.35;
        }

        if (_rPeakArmed && _beatPhase >= RPeakPhase && cfg.Scenario is not Scenario.Asystole)
        {
            _rPeakArmed = false;
            BeatThisSample = true;
        }

        if (cfg.Scenario is Scenario.Asystole)
            return GaussianNoise() * cfg.Noise;

        return cfg.QrsMv * (PWave(_beatPhase) + QrsWave(_beatPhase) + TWave(_beatPhase))
             + GaussianNoise() * cfg.Noise;
    }

    private double EffectiveBpm() => cfg.Scenario switch
    {
        Scenario.Tachycardia => Math.Max(cfg.Bpm, 140),
        Scenario.Bradycardia => Math.Min(cfg.Bpm, 42),
        Scenario.AFib        => 95 + 20 * Math.Sin(Environment.TickCount / 4000.0),
        _                    => cfg.Bpm,
    };

    private double GaussianNoise()
    {
        var u1 = 1 - _rng.NextDouble();
        var u2 = 1 - _rng.NextDouble();
        return Math.Sqrt(-2 * Math.Log(u1)) * Math.Cos(2 * Math.PI * u2);
    }

    private static double PWave(double p)
    {
        if (p < PStart || p > PEnd) return 0;
        var x = (p - PStart) / (PEnd - PStart);
        return 0.12 * (0.5 - 0.5 * Math.Cos(2 * Math.PI * x));
    }

    private static double TWave(double p)
    {
        if (p < TStart || p > TEnd) return 0;
        var x = (p - TStart) / (TEnd - TStart);
        return 0.35 * Math.Sin(Math.PI * x);
    }

    private static double QrsWave(double p)
    {
        if (p < QStart || p > QEnd) return 0;
        var x = (p - QStart) / (QEnd - QStart);

        var harmonics = 0.0;
        for (var k = 1; k <= 6; k++)
            harmonics += Math.Sin(k * Math.PI * x) / k;

        var env = Math.Exp(-Math.Pow((x - 0.5) / 0.08, 2));
        var q   = -0.15 * Math.Exp(-Math.Pow((x - 0.25) / 0.05, 2));
        var s   = -0.25 * Math.Exp(-Math.Pow((x - 0.75) / 0.06, 2));
        return env * (0.6 + 0.4 * harmonics) + q + s;
    }
}
