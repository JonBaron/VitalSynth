namespace VitalSynth.Core.FS

open System

/// Snapshot of vital signs at a point in time.
///
/// F# records are immutable by default — there is no way to mutate a field
/// after creation. The equivalent C# `readonly record struct` requires the
/// `readonly` keyword and explicit init-only properties.
type Vitals = {
    HeartRate    : float<bpm>
    Spo2         : float<pct>
    TemperatureC : float<degC>
    Scenario     : Scenario
    TimestampUtc : DateTime
}
