namespace VitalSynth.Core;

/// <summary>
/// A snapshot of vital signs at a point in time.
/// Produced by the session on every HL7 tick.
/// </summary>
public readonly record struct Vitals(
    int      HeartRate,
    double   Spo2,
    double   TemperatureC,
    Scenario Scenario,
    DateTime TimestampUtc
);
