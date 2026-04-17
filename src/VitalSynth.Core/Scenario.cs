namespace VitalSynth.Core;

/// <summary>
/// Clinical pattern the session is currently playing back.
/// Think of this as the synth's "preset".
/// </summary>
public enum Scenario
{
    Normal,
    Tachycardia,
    Bradycardia,
    AFib,
    Desaturation,
    Asystole,
}
