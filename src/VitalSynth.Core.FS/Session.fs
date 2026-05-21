namespace VitalSynth.Core.FS

open System

/// A running VitalSynth session.
///
/// The line `let dt : float<s> = 1.0 / cfg.SampleRate` below is the
/// money shot: `cfg.SampleRate : float<Hz>`, `Hz` is defined as `/s`,
/// so `1.0 / float<Hz>` is automatically inferred as `float<s>`. No
/// runtime check, no manual `.Value` extraction — the compiler does it.
///
/// `VitalsPublished` is typed as `EventHandler<Vitals>` rather than F#'s
/// native `Event<Vitals>` so C# consumers get an idiomatic
/// `session.VitalsPublished += (sender, v) => …` without `FSharpHandler<T>`.
type Session(cfg: Cfg) =
    let heart = HeartOscillator(cfg)
    let slow  = SlowSignals(cfg)
    let samplesPerHl7 =
        max 1 (int (Math.Round(float cfg.SampleRate * cfg.Hl7Interval.TotalSeconds)))
    let mutable hl7Counter = 0
    let mutable pendingBeats = 0
    let vitalsPublished = Event<EventHandler<Vitals>, Vitals>()

    do
        slow.AdvanceSpo2 0.0<s> |> ignore
        slow.AdvanceTemperature 0.0<s> |> ignore

    member _.Cfg = cfg
    member _.CurrentHeartRate = heart.CurrentHeartRate
    member _.CurrentSpo2      = slow.LastSpo2
    member _.CurrentTempC     = slow.LastTemperature
    member _.PendingBeats     = pendingBeats

    [<CLIEvent>]
    member _.VitalsPublished = vitalsPublished.Publish

    member _.ConsumePendingBeats () =
        let n = pendingBeats
        pendingBeats <- 0
        n

    /// Fills <paramref name="buffer"/> with ECG samples in millivolts.
    member this.Step (buffer: float[]) =
        let dt : float<s> = 1.0 / cfg.SampleRate   // <— Hz⁻¹ inferred as s
        for i in 0 .. buffer.Length - 1 do
            let resp = slow.Respiration dt
            let sample = heart.NextSample(dt, resp)
            buffer.[i] <- inMillivolts sample
            if heart.BeatThisSample then pendingBeats <- pendingBeats + 1
            slow.AdvanceSpo2 dt |> ignore
            slow.AdvanceTemperature dt |> ignore

            hl7Counter <- hl7Counter + 1
            if hl7Counter >= samplesPerHl7 then
                hl7Counter <- 0
                let v : Vitals = {
                    HeartRate    = heart.CurrentHeartRate
                    Spo2         = slow.LastSpo2
                    TemperatureC = slow.LastTemperature
                    Scenario     = cfg.Scenario
                    TimestampUtc = DateTime.UtcNow
                }
                vitalsPublished.Trigger(this, v)

    interface IDisposable with
        member _.Dispose () = ()
