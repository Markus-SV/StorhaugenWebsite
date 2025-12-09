// ==========================================
// 1. CAMERA INTEROP (Video Stream & Capture)
// ==========================================
window.cameraInterop = {
    stream: null,

    start: async (videoElement) => {
        try {
            // Attempt to lock orientation to portrait on mobile
            if (screen.orientation && screen.orientation.lock) {
                try {
                    await screen.orientation.lock("portrait");
                } catch (e) {
                    // Ignore if not supported
                }
            }

            const constraints = {
                audio: false,
                video: {
                    facingMode: 'environment', // Use back camera
                    width: { min: 1280, ideal: 1920 }, // High resolution for OCR
                    height: { min: 720, ideal: 1080 }
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

// ==========================================
// 2. IMAGE TOOLS (Resize & Processing)
// ==========================================
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
                // Resize to max 1200px for consistency
                const base64 = await resizeImage(file, 1200, 1200);
                processedImages.push({
                    name: file.name,
                    data: base64
                });
            } catch (err) {
                console.error("Feil i imageTools:", err);
            }
        }

        // Reset input
        input.value = '';
        return processedImages;
    }
};

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

                resolve(canvas.toDataURL('image/jpeg', 0.8));
            };
            img.onerror = reject;
        };
        reader.onerror = reject;
    });
}

// ==========================================
// 3. OCR INTEROP (Text Recognition)
// This was missing and caused your error!
// ==========================================
window.ocrInterop = {
    recognizeTextFromImage: async (base64Image) => {
        // Ensure Tesseract is loaded in your index.html
        if (typeof Tesseract === 'undefined') {
            console.error("Tesseract.js is not loaded.");
            return "Feil: Tesseract library mangler i index.html.";
        }

        try {
            console.log("Starting OCR...");
            // Run recognition (using 'nor' for Norwegian, fallback to 'eng')
            const result = await Tesseract.recognize(
                base64Image,
                'nor',
                { logger: m => console.log(m) }
            );

            return result.data.text;
        } catch (error) {
            console.error("OCR Error:", error);
            return null;
        }
    }
};