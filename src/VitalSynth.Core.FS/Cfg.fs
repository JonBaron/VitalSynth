namespace VitalSynth.Core.FS

open System

/// Mutable configuration. F# records are normally immutable; the explicit
/// <c>mutable</c> keyword signals "this is a knob that may be wiggled at
/// runtime". An "accidental" mutation is impossible — every mutable field
/// is opt-in and visible.
///
/// `Defaults` is attached as a static member rather than a same-named module,
/// so C# consumers can write the natural `Cfg.Defaults()` instead of the
/// F#-compiled `CfgModule.defaults()`.
type Cfg = {
    mutable HeartRate       : float<bpm>
    mutable HrJitter        : float<pct>
    mutable QrsAmplitude    : float<V>
    mutable RespiratoryRate : float<bpm>
    mutable RespDepth       : float
    mutable Spo2Base        : float<pct>
    mutable Spo2Drift       : float
    mutable TempBase        : float<degC>
    mutable TempDrift       : float
    mutable NoiseAmplitude  : float<V>
    mutable SampleRate      : float<Hz>
    mutable Scenario        : Scenario
    mutable Hl7Interval     : TimeSpan
}
with
    static member Defaults () : Cfg = {
        HeartRate       = 72.0<bpm>
        HrJitter        = 3.0<pct>
        QrsAmplitude    = mV 1.2
        RespiratoryRate = 14.0<bpm>
        RespDepth       = 0.04
        Spo2Base        = 98.0<pct>
        Spo2Drift       = 0.3
        TempBase        = 36.8<degC>
        TempDrift       = 0.05
        NoiseAmplitude  = mV 0.015
        SampleRate      = 250.0<Hz>
        Scenario        = Normal
        Hl7Interval     = TimeSpan.FromSeconds(1.0)
    }
