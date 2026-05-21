namespace VitalSynth.Core;

/// <summary>
/// A snapshot of vital signs at a point in time.
/// Produced by the session on every HL7 tick.
/// </summary>
public readonly record struct Vitals(
    BeatsPerMinute HeartRate,
    Percent        Spo2,
    Celsius        TemperatureC,
    Scenario       Scenario,
    DateTime       TimestampUtc
);
