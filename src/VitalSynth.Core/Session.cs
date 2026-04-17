namespace VitalSynth.Core;

/// <summary>
/// A running VitalSynth session. Owns the oscillators, slow modulators
/// and the HL7 tick cadence. UI-agnostic — a console, Blazor WASM or
/// WPF client all consume the same session by calling <see cref="Step"/>
/// and reading the exposed vitals.
///
/// The engine is <b>passive</b>: the client chooses when to step and
/// how many samples to produce, which keeps threading, animation and
/// pause semantics simple across platforms.
/// </summary>
public sealed class Session : IDisposable
{
    private readonly HeartOscillator _heart;
    private readonly SlowSignals _slow;
    private readonly IVitalsSink? _sink;
    private readonly int _samplesPerHl7;
    private int _hl7Counter;

    public Cfg Cfg { get; }

    public int    CurrentBpm   => _heart.CurrentBpm;
    public double CurrentSpo2  => _slow.LastSpo2;
    public double CurrentTempC => _slow.LastTempC;

    /// <summary>Raised on every HL7 tick, after the sink has been published to.</summary>
    public event Action<Vitals>? VitalsPublished;

    public Session(Cfg cfg, IVitalsSink? sink = null)
    {
        Cfg = cfg;
        _heart = new HeartOscillator(cfg);
        _slow  = new SlowSignals(cfg);
        _sink  = sink;
        _samplesPerHl7 = Math.Max(1, cfg.SampleRateHz * cfg.Hl7IntervalMs / 1000);

        _slow.AdvanceSpo2(0);
        _slow.AdvanceTemperature(0);
    }

    /// <summary>
    /// Advance the engine by <paramref name="samples"/> samples at the
    /// configured sample rate, filling <paramref name="buffer"/> with
    /// ECG voltages in mV. Triggers HL7 ticks when due.
    /// </summary>
    public void Step(Span<double> buffer)
    {
        var dt = 1.0 / Cfg.SampleRateHz;
        for (var i = 0; i < buffer.Length; i++)
        {
            var resp = _slow.Respiration(dt);
            buffer[i] = _heart.NextSample(dt, resp);
            _slow.AdvanceSpo2(dt);
            _slow.AdvanceTemperature(dt);

            if (++_hl7Counter >= _samplesPerHl7)
            {
                _hl7Counter = 0;
                var v = new Vitals(
                    HeartRate:    _heart.CurrentBpm,
                    Spo2:         _slow.LastSpo2,
                    TemperatureC: _slow.LastTempC,
                    Scenario:     Cfg.Scenario,
                    TimestampUtc: DateTime.UtcNow);
                _sink?.Publish(v);
                VitalsPublished?.Invoke(v);
            }
        }
    }

    public void Dispose()
    {
        if (_sink is IDisposable d) d.Dispose();
    }
}
