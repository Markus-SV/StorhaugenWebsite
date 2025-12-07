window.cameraInterop = {
stream: null,

    start: async (videoElement) => {
        try
        {
            // Request the rear camera (environment)
            const stream = await navigator.mediaDevices.getUserMedia({
            video: { facingMode: 'environment' }
            });
            videoElement.srcObject = stream;
            window.cameraInterop.stream = stream;
        }
        catch (err)
        {
            console.error("Camera Error:", err);
            alert("Kunne ikke starte kamera. Sjekk tillatelser.");
        }
    },

    capture: async (videoElement) => {
        const canvas = document.createElement("canvas");
        // Capture at the resolution of the video feed
        canvas.width = videoElement.videoWidth;
        canvas.height = videoElement.videoHeight;

        // Draw the current video frame to the canvas
        canvas.getContext('2d').drawImage(videoElement, 0, 0);

        // Convert to high-quality JPEG
        return canvas.toDataURL("image/jpeg", 0.9);
    },

    stop: (videoElement) => {
        if (window.cameraInterop.stream)
        {
            window.cameraInterop.stream.getTracks().forEach(track => track.stop());
            window.cameraInterop.stream = null;
        }
        if (videoElement)
        {
            videoElement.srcObject = null;
        }
    }
}
;