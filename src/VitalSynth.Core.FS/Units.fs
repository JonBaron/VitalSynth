namespace VitalSynth.Core.FS

/// Compile-time units of measure.
///
/// Unlike the C# wrapper-structs in `VitalSynth.Core/Units/`, these survive
/// arithmetic: <c>(V × s) / s</c> stays <c>V</c>, <c>1 / Hz</c> automatically
/// becomes <c>s</c>, and any mismatch is a compile error — not a runtime
/// check. Six declarations replace ~200 lines of hand-written C# wrappers.
///
/// Notice how `Hz` is *defined* as `/s` (inverse seconds). That single line
/// lets the compiler reason about <c>1.0 / sampleRate</c> automatically.
[<AutoOpen>]
module Units =
    [<Measure>] type V          // volt
    [<Measure>] type s          // second
    [<Measure>] type Hz = /s    // hertz, defined as 1/s
    [<Measure>] type bpm        // beats per minute (clinical domain unit)
    [<Measure>] type degC       // degrees Celsius
    [<Measure>] type pct        // percent (0..100)

    /// 1 mV = 0.001 V — used only at boundaries.
    let inline mV (x: float) : float<V> = x * 0.001<V>

    /// Project back to millivolts for ECG wire format / scope rendering.
    let inline inMillivolts (v: float<V>) : float = float v * 1000.0
