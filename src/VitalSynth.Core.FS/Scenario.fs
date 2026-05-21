namespace VitalSynth.Core.FS

/// Clinical pattern the session is currently playing back.
///
/// Discriminated union — the killer F# feature over C# enums:
/// each case may carry its own typed data, and any <c>match</c>
/// expression is checked for completeness at compile time.
///
/// Adding a new case (e.g. <c>Bigeminy</c>) here triggers a warning
/// in every <c>match</c> that doesn't handle it.
type Scenario =
    | Normal
    | Tachycardia
    | Bradycardia
    /// Atrial fibrillation. Carries the jitter amplitude as part of the case.
    | AFib of jitterAmplitude: float<bpm>
    /// Carries how long ago the desaturation started.
    | Desaturation of onset: float<s>
    | Asystole
