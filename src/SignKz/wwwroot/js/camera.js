// Thin layer: this script only opens the camera and sends hand points to C#.
// All recognition logic is written in C# (HandPose, GestureRecognizer).
window.signCamera = (function () {
    const VERSION = "0.10.14";
    const BASE = "https://cdn.jsdelivr.net/npm/@mediapipe/tasks-vision@" + VERSION;
    const MODEL = "https://storage.googleapis.com/mediapipe-models/hand_landmarker/hand_landmarker/float16/1/hand_landmarker.task";

    // Pairs of points that are connected by a line (for drawing the hand).
    const CONNECTIONS = [
        [0, 1], [1, 2], [2, 3], [3, 4],
        [0, 5], [5, 6], [6, 7], [7, 8],
        [5, 9], [9, 10], [10, 11], [11, 12],
        [9, 13], [13, 14], [14, 15], [15, 16],
        [13, 17], [17, 18], [18, 19], [19, 20], [0, 17]
    ];

    let stream = null;
    let landmarker = null;
    let running = false;
    let lastSend = 0;

    async function loadLandmarker() {
        if (landmarker) return;
        const vision = await import(BASE + "/vision_bundle.mjs");
        const files = await vision.FilesetResolver.forVisionTasks(BASE + "/wasm");
        landmarker = await vision.HandLandmarker.createFromOptions(files, {
            baseOptions: { modelAssetPath: MODEL },
            runningMode: "VIDEO",
            numHands: 1
        });
    }

    function draw(canvas, points) {
        const ctx = canvas.getContext("2d");
        ctx.clearRect(0, 0, canvas.width, canvas.height);
        if (!points) return;

        ctx.strokeStyle = "#2dd4bf";
        ctx.lineWidth = 3;
        for (const [a, b] of CONNECTIONS) {
            ctx.beginPath();
            ctx.moveTo(points[a].x * canvas.width, points[a].y * canvas.height);
            ctx.lineTo(points[b].x * canvas.width, points[b].y * canvas.height);
            ctx.stroke();
        }
        ctx.fillStyle = "#f97316";
        for (const p of points) {
            ctx.beginPath();
            ctx.arc(p.x * canvas.width, p.y * canvas.height, 4, 0, Math.PI * 2);
            ctx.fill();
        }
    }

    function loop(video, canvas, dotnetRef) {
        if (!running) return;

        const now = performance.now();
        // About 10 frames per second is enough and keeps the server calm.
        if (now - lastSend >= 100 && video.readyState >= 2) {
            lastSend = now;
            const result = landmarker.detectForVideo(video, now);

            if (result.landmarks && result.landmarks.length > 0) {
                const points = result.landmarks[0];
                draw(canvas, points);

                // Picture points first (x is multiplied by the aspect ratio, so distances are not stretched),
                // then the 3D "world" points. C# splits them again (see HandPose.FromLandmarks).
                const aspect = (video.videoWidth && video.videoHeight) ? video.videoWidth / video.videoHeight : 4 / 3;
                const flat = [];
                for (const p of points) {
                    flat.push(p.x * aspect, p.y, p.z);
                }
                if (result.worldLandmarks && result.worldLandmarks.length > 0) {
                    for (const p of result.worldLandmarks[0]) {
                        flat.push(p.x, p.y, p.z);
                    }
                }
                dotnetRef.invokeMethodAsync("OnLandmarks", flat).catch(function () { });
            } else {
                draw(canvas, null);
                dotnetRef.invokeMethodAsync("OnNoHand").catch(function () { });
            }
        }
        requestAnimationFrame(function () { loop(video, canvas, dotnetRef); });
    }

    // Returns: "ok", "denied", "nocamera", "modelfail" or "error".
    async function start(videoId, canvasId, dotnetRef) {
        const video = document.getElementById(videoId);
        const canvas = document.getElementById(canvasId);
        if (!video || !canvas) return "error";

        if (!navigator.mediaDevices || !navigator.mediaDevices.getUserMedia) {
            return "nocamera";
        }

        try {
            stream = await navigator.mediaDevices.getUserMedia({
                video: { width: 640, height: 480, facingMode: "user" },
                audio: false
            });
        } catch (e) {
            return e.name === "NotAllowedError" ? "denied" : "nocamera";
        }

        video.srcObject = stream;
        await video.play();
        canvas.width = video.videoWidth || 640;
        canvas.height = video.videoHeight || 480;

        try {
            await loadLandmarker();
        } catch (e) {
            console.error(e);
            stop();
            return "modelfail";
        }

        running = true;
        loop(video, canvas, dotnetRef);
        return "ok";
    }

    function stop() {
        running = false;
        if (stream) {
            stream.getTracks().forEach(function (t) { t.stop(); });
            stream = null;
        }
    }

    return { start: start, stop: stop };
})();
