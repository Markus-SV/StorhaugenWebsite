window.cameraInterop = {
    stream: null,

    start: async (videoElement) => {
        try {
            // We request a very high resolution. 
            // The browser will automatically provide the best the hardware can handle (e.g., 4K or 1080p).
            const constraints = {
                audio: false,
                video: {
                    facingMode: 'environment',
                    width: { min: 1280, ideal: 4096 },
                    height: { min: 720, ideal: 2160 }
                }
            };

            const stream = await navigator.mediaDevices.getUserMedia(constraints);
            videoElement.srcObject = stream;
            window.cameraInterop.stream = stream;
        }
        catch (err) {
            console.error("Camera Error:", err);
            alert("Kunne ikke starte kamera. Sjekk tillatelser.");
        }
    },

    capture: async (videoElement) => {
        const canvas = document.createElement("canvas");

        // Match the canvas size to the ACTUAL video stream size (High Res)
        // This solves "bad quality" by using the full sensor output
        canvas.width = videoElement.videoWidth;
        canvas.height = videoElement.videoHeight;

        // Draw the current video frame to the canvas
        canvas.getContext('2d').drawImage(videoElement, 0, 0);

        // Return high quality JPEG (0.95 quality)
        return canvas.toDataURL("image/jpeg", 0.95);
    },

    stop: (videoElement) => {
        if (window.cameraInterop.stream) {
            window.cameraInterop.stream.getTracks().forEach(track => track.stop());
            window.cameraInterop.stream = null;
        }
        if (videoElement) {
            videoElement.srcObject = null;
        }
    }
};