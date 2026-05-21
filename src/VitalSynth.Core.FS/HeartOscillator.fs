namespace VitalSynth.Core.FS

open System

/// Synthesizes an ECG P-QRS-T complex from a small Fourier series.
///
/// All pattern matches on <see cref="Scenario"/> are exhaustive — listed
/// explicitly without a <c>_</c>-catch-all so the compiler can flag any
/// missing case when a new Scenario is added.
type HeartOscillator(cfg: Cfg) =
    let rng = Random(0xEC6)

    // Internal scratch state stays as raw float — the units are checked
    // at the public boundary in `NextSample` / `CurrentHeartRate`.
    let mutable beatPhase = 0.0
    let mutable rrSec     = 60.0 / float cfg.HeartRate
    let mutable lastBpm   = float cfg.HeartRate
    let mutable rPeakArmed = true
    let mutable beatThisSample = false

    let pStart, pEnd = 0.10, 0.18
    let qStart, qEnd = 0.22, 0.30
    let tStart, tEnd = 0.38, 0.55
    let rPeakPhase   = 0.26

    let gaussianNoise () =
        let u1 = 1.0 - rng.NextDouble()
        let u2 = 1.0 - rng.NextDouble()
        sqrt (-2.0 * log u1) * cos (2.0 * Math.PI * u2)

    let pWave p =
        if p < pStart || p > pEnd then 0.0
        else
            let x = (p - pStart) / (pEnd - pStart)
            0.12 * (0.5 - 0.5 * cos (2.0 * Math.PI * x))

    let tWave p =
        if p < tStart || p > tEnd then 0.0
        else
            let x = (p - tStart) / (tEnd - tStart)
            0.35 * sin (Math.PI * x)

    let qrsWave p =
        if p < qStart || p > qEnd then 0.0
        else
            let x = (p - qStart) / (qEnd - qStart)
            let mutable harmonics = 0.0
            for k in 1 .. 6 do
                harmonics <- harmonics + sin (float k * Math.PI * x) / float k
            let env = exp (-((x - 0.5) / 0.08) ** 2.0)
            let q   = -0.15 * exp (-((x - 0.25) / 0.05) ** 2.0)
            let s   = -0.25 * exp (-((x - 0.75) / 0.06) ** 2.0)
            env * (0.6 + 0.4 * harmonics) + q + s

    /// Exhaustive — adding a new Scenario yields a compiler warning here.
    let effectiveBpm () =
        match cfg.Scenario with
        | Tachycardia       -> max (float cfg.HeartRate) 140.0
        | Bradycardia       -> min (float cfg.HeartRate)  42.0
        | AFib _            -> 95.0 + 20.0 * sin (float Environment.TickCount / 4000.0)
        | Normal
        | Desaturation _
        | Asystole          -> float cfg.HeartRate

    member _.BeatThisSample = beatThisSample

    /// Note the exhaustive match: no `_` wildcard, all six cases listed.
    member _.CurrentHeartRate : float<bpm> =
        match cfg.Scenario with
        | Asystole          -> 0.0<bpm>
        | AFib _            -> (95.0 + float (rng.Next(-15, 25))) * 1.0<bpm>
        | Normal
        | Tachycardia
        | Bradycardia
        | Desaturation _    -> Math.Round(lastBpm) * 1.0<bpm>

    /// Returned in volts — the boundary type carries the unit; callers
    /// project to millivolts via <see cref="Units.inMillivolts"/>.
    member _.NextSample (dt: float<s>, respModulation: float) : float<V> =
        beatThisSample <- false
        lastBpm <- effectiveBpm () * (1.0 + respModulation * cfg.RespDepth)
        beatPhase <- beatPhase + float dt / rrSec

        if beatPhase >= 1.0 then
            beatPhase <- beatPhase - 1.0
            rPeakArmed <- true
            let jitter = (rng.NextDouble() - 0.5) * float cfg.HrJitter / 50.0
            rrSec <- 60.0 / max lastBpm 1.0 * (1.0 + jitter)
            match cfg.Scenario with
            | AFib _ -> rrSec <- rrSec * (1.0 + (rng.NextDouble() - 0.5) * 0.35)
            | Normal | Tachycardia | Bradycardia | Desaturation _ | Asystole -> ()

        if rPeakArmed && beatPhase >= rPeakPhase then
            match cfg.Scenario with
            | Asystole -> ()
            | Normal | Tachycardia | Bradycardia | AFib _ | Desaturation _ ->
                rPeakArmed <- false
                beatThisSample <- true

        let noiseV = gaussianNoise () * float cfg.NoiseAmplitude

        match cfg.Scenario with
        | Asystole -> noiseV * 1.0<V>
        | Normal | Tachycardia | Bradycardia | AFib _ | Desaturation _ ->
            (float cfg.QrsAmplitude * (pWave beatPhase + qrsWave beatPhase + tWave beatPhase)
             + noiseV) * 1.0<V>
