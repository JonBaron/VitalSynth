// VitalSynth scope renderer — draws ECG samples to a canvas.
// Invoked from Blazor via IJSRuntime. The canvas is sized to match
// the display area at mount time; samples are pushed into a ring
// buffer and the whole trace is repainted every frame.

window.vitalScope = (() => {
    let canvas, ctx;
    let ring, cursor = 0;

    // --- Audio: patient-monitor beep + asystole alarm ---------------------
    let audioCtx = null;
    let audioOn = false;
    let alarmTimer = null;

    function ensureAudio() {
        if (!audioCtx) {
            const AC = window.AudioContext || window.webkitAudioContext;
            if (!AC) return false;
            audioCtx = new AC();
        }
        if (audioCtx.state === 'suspended') audioCtx.resume();
        return true;
    }

    function playTone(freq, durationMs, type, peakGain) {
        if (!audioOn || !audioCtx) return;
        const t = audioCtx.currentTime;
        const osc = audioCtx.createOscillator();
        const gain = audioCtx.createGain();
        osc.type = type;
        osc.frequency.value = freq;
        gain.gain.setValueAtTime(0, t);
        gain.gain.linearRampToValueAtTime(peakGain, t + 0.004);
        gain.gain.exponentialRampToValueAtTime(0.0001, t + durationMs / 1000);
        osc.connect(gain).connect(audioCtx.destination);
        osc.start(t);
        osc.stop(t + durationMs / 1000 + 0.02);
    }

    function resizeToContainer() {
        if (!canvas) return;
        const dpr = window.devicePixelRatio || 1;
        const { width, height } = canvas.getBoundingClientRect();
        const w = Math.max(64, Math.floor(width  * dpr));
        const h = Math.max(64, Math.floor(height * dpr));
        if (canvas.width !== w || canvas.height !== h) {
            canvas.width = w;
            canvas.height = h;
            ring = new Float32Array(w);
            cursor = 0;
        }
    }

    function drawGrid() {
        const w = canvas.width, h = canvas.height;
        ctx.strokeStyle = 'rgba(0, 255, 65, 0.08)';
        ctx.lineWidth = 1;
        ctx.beginPath();
        const step = Math.floor(h / 8);
        for (let y = step; y < h; y += step) { ctx.moveTo(0, y); ctx.lineTo(w, y); }
        for (let x = step; x < w; x += step) { ctx.moveTo(x, 0); ctx.lineTo(x, h); }
        ctx.stroke();
    }

    function drawTrace() {
        const w = canvas.width, h = canvas.height;
        const mid = h * 0.55;
        const gain = h * 0.28;

        ctx.strokeStyle = '#00ff41';
        ctx.lineWidth = Math.max(1.5, h * 0.006);
        ctx.shadowColor = '#00ff41';
        ctx.shadowBlur = Math.max(4, h * 0.02);
        ctx.lineJoin = 'round';
        ctx.lineCap = 'round';

        ctx.beginPath();
        for (let x = 0; x < w; x++) {
            const v = ring[(x + cursor) % w];
            const y = mid - v * gain;
            if (x === 0) ctx.moveTo(x, y); else ctx.lineTo(x, y);
        }
        ctx.stroke();
        ctx.shadowBlur = 0;
    }

    function paint() {
        const w = canvas.width, h = canvas.height;
        ctx.fillStyle = '#030a05';
        ctx.fillRect(0, 0, w, h);
        drawGrid();
        if (ring) drawTrace();
    }

    return {
        init(el) {
            canvas = el;
            ctx = canvas.getContext('2d');
            resizeToContainer();
            paint();
            window.addEventListener('resize', () => {
                resizeToContainer();
                paint();
            });
        },
        pushAndPaint(samples) {
            if (!ctx) return;
            resizeToContainer();
            const w = canvas.width;
            for (let i = 0; i < samples.length; i++) {
                ring[cursor] = samples[i];
                cursor = (cursor + 1) % w;
            }
            paint();
        },

        enableAudio() {
            if (!ensureAudio()) return false;
            audioOn = true;
            return true;
        },
        disableAudio() {
            audioOn = false;
            this.stopAlarm();
        },
        isAudioOn() { return audioOn; },

        // QRS beep — pitch rises with SpO2 (~880Hz @100%, drops ~4Hz per %)
        beep(spo2) {
            if (!audioOn) return;
            const clamped = Math.max(70, Math.min(100, spo2));
            const freq = 560 + (clamped - 80) * 16;  // 560Hz@80% .. 880Hz@100%
            playTone(freq, 90, 'sine', 0.22);
        },

        // Urgent triple beep — used for asystole
        startAlarm() {
            if (alarmTimer) return;
            const burst = () => {
                if (!audioOn) return;
                playTone(880, 120, 'square', 0.18);
                setTimeout(() => playTone(880, 120, 'square', 0.18), 180);
                setTimeout(() => playTone(880, 120, 'square', 0.18), 360);
            };
            burst();
            alarmTimer = setInterval(burst, 1500);
        },
        stopAlarm() {
            if (alarmTimer) { clearInterval(alarmTimer); alarmTimer = null; }
        }
    };
})();
