namespace VitalSynth.Core;

/// <summary>
/// A destination for vital-sign snapshots. The core library publishes
/// to this from the session loop; transports (HL7/MLLP TCP, HTTP,
/// WebSocket, in-memory, …) implement their own delivery.
/// </summary>
public interface IVitalsSink
{
    void Publish(Vitals vitals);
}
