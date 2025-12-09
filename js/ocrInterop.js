window.cameraInterop = {
    stream: null,

    start: async (videoElement) => {
        try {
            // Prøv å låse skjermretning hvis mulig (Android/PWA)
            if (screen.orientation && screen.orientation.lock) {
                try {
                    await screen.orientation.lock("portrait");
                } catch (e) {
                    // Ignorer feil her, støttes ikke av alle
                }
            }

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
        canvas.width = videoElement.videoWidth;
        canvas.height = videoElement.videoHeight;
        canvas.getContext('2d').drawImage(videoElement, 0, 0);
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

window.imageTools = {
    processInputFile: async (inputId) => {
        const input = document.getElementById(inputId);
        if (!input || !input.files || input.files.length === 0) {
            return [];
        }

        const processedImages = [];

        for (let i = 0; i < input.files.length; i++) {
            const file = input.files[i];
            try {
                // Resize til max 1200px
                const base64 = await resizeImage(file, 1200, 1200);
                processedImages.push({
                    name: file.name,
                    data: base64
                });
            } catch (err) {
                console.error("Feil i imageTools:", err);
            }
        }

        // Nullstill input for å kunne velge samme fil igjen
        input.value = '';
        return processedImages;
    }
};

// Hjelpefunksjon
function resizeImage(file, maxWidth, maxHeight) {
    return new Promise((resolve, reject) => {
        const reader = new FileReader();
        reader.readAsDataURL(file);
        reader.onload = (event) => {
            const img = new Image();
            img.src = event.target.result;
            img.onload = () => {
                const canvas = document.createElement('canvas');
                let width = img.width;
                let height = img.height;

                if (width > height) {
                    if (width > maxWidth) {
                        height *= maxWidth / width;
                        width = maxWidth;
                    }
                } else {
                    if (height > maxHeight) {
                        width *= maxHeight / height;
                        height = maxHeight;
                    }
                }

                canvas.width = width;
                canvas.height = height;
                const ctx = canvas.getContext('2d');
                ctx.drawImage(img, 0, 0, width, height);

                // Returner ren JPEG base64
                resolve(canvas.toDataURL('image/jpeg', 0.8));
            };
            img.onerror = reject;
        };
        reader.onerror = reject;
    });
}