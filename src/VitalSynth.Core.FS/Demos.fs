module VitalSynth.Core.FS.Demos

open System

// ════════════════════════════════════════════════════════════════════════
// Working examples of what the unit system guarantees.
//
// All declarations below COMPILE. The commented blocks show real F#
// compiler errors that would appear if you uncommented them.
// ════════════════════════════════════════════════════════════════════════


// ── Construction with units ─────────────────────────────────────────
let qrs        : float<V>    = mV 1.2          // 0.0012 V
let sampleRate : float<Hz>   = 250.0<Hz>
let resting    : float<bpm>  = 72.0<bpm>
let body       : float<degC> = 36.8<degC>


// ── Inference: 1 / Hz becomes seconds, automatically ────────────────
//   cfg.SampleRate is float<Hz>, and Hz = /s, so 1.0 / Hz = s.
//   The compiler figures this out without any cast or `.Value`.
let dt : float<s> = 1.0 / sampleRate
let samplesPerSecond : float = float sampleRate * float dt  // = 1.0


// ── Arithmetic that respects dimensions ─────────────────────────────
let louder : float<V> = qrs * 2.0
let quiet  : float<V> = -qrs
let sum    : float<V> = qrs + mV 0.5    // 1.7 mV — different prefixes, same dimension


// ── Exhaustive pattern match (no wildcard) ──────────────────────────
//   Add a new Scenario case in Scenario.fs and the compiler will
//   warn here that this match is incomplete.
let bpmFor scenario =
    match scenario with
    | Normal | Tachycardia | Bradycardia -> 72.0<bpm>
    | AFib _                             -> 95.0<bpm>
    | Desaturation _                     -> 72.0<bpm>
    | Asystole                           ->  0.0<bpm>


// ════════════════════════════════════════════════════════════════════════
// COMPILE-TIME REJECTIONS
//
// Uncomment any of these blocks and `dotnet build` will refuse it with
// the error message shown — these are real F# diagnostics, not made up.
// ════════════════════════════════════════════════════════════════════════


// ── 1. Mixing different dimensions ──────────────────────────────────
//
//   let bad1 : float<V> = resting
//
//   FS0001: Type mismatch. Expecting 'float<V>' but given 'float<bpm>'.
//           The unit of measure 'V' does not match the unit of
//           measure 'bpm'.


// ── 2. Adding mismatched dimensions ─────────────────────────────────
//
//   let bad2 = qrs + resting
//
//   FS0001: The unit of measure 'bpm' does not match the unit of
//           measure 'V'.


// ── 3. Calling a function with wrong-dimensioned argument ───────────
//
//   let bad3 = SlowSignals(Cfg.Defaults()).Respiration resting
//
//   FS0001: This expression was expected to have type 'float<s>',
//           but here has type 'float<bpm>'.


// ── 4. Squared seconds where seconds expected ───────────────────────
//
//   let bad4 : float<s> = 1.0<s> * 1.0<s>
//
//   FS0001: The unit of measure 's' does not match the unit of
//           measure 's ^ 2'.


// ── 5. Wrong shape for a discriminated union case ───────────────────
//
//   let bad5 : Scenario = Normal 5.0<bpm>
//
//   FS0003: This value is not a function and cannot be applied.
//   (F# treats nullary DU cases as values — applying them to an
//    argument is the parser's way of saying "Normal takes no payload".)


// ── 6. Forgetting a case in a pattern match (with a fresh scenario) ─
//
//   If you add `| Bigeminy` to Scenario.fs and don't update bpmFor:
//
//   FS0025: Incomplete pattern matches on this expression. For example,
//           the value 'Bigeminy' may indicate a case not covered by the
//           pattern(s).


// ── 6. Forgetting a case in a pattern match (with a fresh scenario) ─
//
//   If you add `| Bigeminy` to Scenario.fs and don't update bpmFor:
//
//   FS0025: Incomplete pattern matches on this expression. For example,
//           the value 'Bigeminy' may indicate a case not covered by the
//           pattern(s).
