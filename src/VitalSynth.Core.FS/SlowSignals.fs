namespace VitalSynth.Core.FS

open System
open System.Diagnostics

/// Slow-moving signals: respiration LFO, SpO2 drift, temperature drift.
///
/// Boundary types carry units of measure; internal scratch variables
/// (drift walks etc.) stay raw <c>float</c> — wrapping every line would
/// only add noise. This is the same pragmatic split used in the C# version.
type SlowSignals(cfg: Cfg) =
    let rng = Random(0x5B02)
    let clock = Stopwatch.StartNew()
    let mutable respPhase = 0.0
    let mutable spo2Walk = 0.0
    let mutable tempWalk = 0.0
    let mutable lastSpo2 = cfg.Spo2Base
    let mutable lastTemp = cfg.TempBase

    member _.LastSpo2 = lastSpo2
    member _.LastTemperature = lastTemp

    member _.Respiration (dt: float<s>) : float =
        respPhase <- respPhase + 2.0 * Math.PI * float cfg.RespiratoryRate / 60.0 * float dt
        sin respPhase

    member _.AdvanceSpo2 (dt: float<s>) : float<pct> =
        spo2Walk <-
            Math.Clamp(
                spo2Walk + (rng.NextDouble() - 0.5) * cfg.Spo2Drift * float dt,
                -1.5, 1.5)
        let mutable value = float cfg.Spo2Base + spo2Walk
        match cfg.Scenario with
        | Desaturation _ ->
            value <- value - 8.0 * (1.0 - Math.Exp(-clock.Elapsed.TotalSeconds / 15.0))
        | Normal | Tachycardia | Bradycardia | AFib _ | Asystole ->
            ()
        lastSpo2 <- Math.Clamp(value, 70.0, 100.0) * 1.0<pct>
        lastSpo2

    member _.AdvanceTemperature (dt: float<s>) : float<degC> =
        tempWalk <- tempWalk + (rng.NextDouble() - 0.5) * cfg.TempDrift * float dt
        lastTemp <- (float cfg.TempBase + tempWalk) * 1.0<degC>
        lastTemp
