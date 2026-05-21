module VitalSynth.Core.FS.Hl7

open System
open System.Globalization

/// Formats vital signs as an HL7 v2 ORU^R01 message.
///
/// Pure function — no state, no sockets. Move the TCP transport out if needed;
/// keeping the format pure makes it trivial to snapshot-test or pipe to a file.
let format (v: Vitals) : string =
    let ts   = v.TimestampUtc.ToString("yyyyMMddHHmmss")
    let ctrl = Guid.NewGuid().ToString("N").Substring(0, 10)
    let inv  = CultureInfo.InvariantCulture
    let hr   = int (Math.Round(float v.HeartRate))
    let spo2 = (float v.Spo2).ToString("0.0", inv)
    let temp = (float v.TemperatureC).ToString("0.00", inv)
    String.Join("\r", [
        sprintf "MSH|^~\\&|VITALSYNTH|DEVROOM|EMR|HOSPITAL|%s||ORU^R01|%s|P|2.5" ts ctrl
        "PID|1||SYN-000001^^^VITALSYNTH||Doe^Synthetic||19700101|M"
        sprintf "OBR|1|%s|%s|VITALS^Vital Signs|||%s" ctrl ctrl ts
        sprintf "OBX|1|NM|HR^Heart Rate|1|%d|bpm|60-100||||F" hr
        sprintf "OBX|2|NM|SPO2^Oxygen Saturation|1|%s|%%|95-100||||F" spo2
        sprintf "OBX|3|NM|TEMP^Temperature|1|%s|Cel|36.1-37.5||||F" temp
    ]) + "\r"
