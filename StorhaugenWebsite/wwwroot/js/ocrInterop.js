// ==========================================
// 1. CAMERA INTEROP (Video Stream & Capture)
// ==========================================
window.cameraInterop = {
    stream: null,

    start: async (videoElement) => {
        if (window.cameraInterop.stream) {
            window.cameraInterop.stream.getTracks().forEach(track => track.stop());
            window.cameraInterop.stream = null;
        }

        try {
            const constraints = {
                audio: false,
                video: {
                    facingMode: 'environment',
                    width: { ideal: 1920 },
                    height: { ideal: 1080 },
                    focusMode: 'continuous' // Try to force focus
                }
            };
            const stream = await navigator.mediaDevices.getUserMedia(constraints);
            videoElement.srcObject = stream;
            window.cameraInterop.stream = stream;

            // Wait a moment for camera to adjust focus/exposure
            const track = stream.getVideoTracks()[0];
            if (track.getCapabilities && track.getCapabilities().focusMode) {
                try { await track.applyConstraints({ advanced: [{ focusMode: "continuous" }] }); } catch (e) { }
            }
        }
        catch (err) {
            console.error("Camera Error:", err);
        }
    },

    capture: async (videoElement) => {
        const canvas = document.createElement("canvas");
        canvas.width = videoElement.videoWidth;
        canvas.height = videoElement.videoHeight;
        // Draw image
        canvas.getContext('2d').drawImage(videoElement, 0, 0);
        return canvas.toDataURL("image/jpeg", 0.95); // High quality for OCR
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
// 2. IMAGE TOOLS
// ==========================================
window.imageTools = {
    processInputFile: async (inputId) => {
        const input = document.getElementById(inputId);
        if (!input || !input.files || input.files.length === 0) return [];

        const processedImages = [];
        for (let i = 0; i < input.files.length; i++) {
            const file = input.files[i];
            try {
                // Resize to max 2000px - bigger is often better for text detection
                const base64 = await resizeImage(file, 2000, 2000);
                processedImages.push({ name: file.name, data: base64 });
            } catch (err) {
                console.error("Error processing file:", err);
            }
        }
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
                resolve(canvas.toDataURL('image/jpeg', 0.9));
            };
        };
    });
}

// ==========================================
// 3. OCR INTEROP (Robust Rotation Logic)
// ==========================================
window.ocrInterop = {
    recognizeTextFromImage: async (base64Image) => {
        if (typeof Tesseract === 'undefined') {
            console.error("Tesseract.js not loaded.");
            return "Error: Tesseract missing";
        }

        try {
            console.log("OCR Start...");
            const img = new Image();
            img.src = base64Image;
            await new Promise(r => img.onload = r);

            // Create worker with better config
            const worker = await Tesseract.createWorker('nor');
            await worker.setParameters({
                tessedit_pageseg_mode: Tesseract.PSM.AUTO,
                preserve_interword_spaces: '1',
            });

            // --- STRATEGY: Try 0 -> 90 -> 270 (270 is needed for your Title image) ---

            // 1. Try Normal (0 deg)
            let bestText = await tryRecognize(worker, img, 0);

            // 2. If result looks bad, try Landscape (90 deg)
            if (isTextGarbage(bestText)) {
                console.log("Text garbage at 0deg. Trying 90deg...");
                const text90 = await tryRecognize(worker, img, 90);
                if (countLetters(text90) > countLetters(bestText)) bestText = text90;
            }

            // 3. If result still looks bad, try Inverse Landscape (270 deg)
            // This is CRITICAL for the "Sprø katsu" image you provided
            if (isTextGarbage(bestText)) {
                console.log("Text garbage. Trying 270deg (bottom-to-top text)...");
                const text270 = await tryRecognize(worker, img, 270);
                if (countLetters(text270) > countLetters(bestText)) bestText = text270;
            }

            await worker.terminate();
            return bestText;

        } catch (error) {
            console.error("OCR Error:", error);
            return null;
        }
    }
};

async function tryRecognize(worker, imgElement, rotation) {
    // Preprocess: Rotate -> Grayscale -> Contrast
    const processedBase64 = preprocessImageForOCR(imgElement, rotation);
    const result = await worker.recognize(processedBase64);
    console.log(`Result at ${rotation}deg:`, result.data.text.substring(0, 50) + "...");
    return result.data.text;
}

function countLetters(text) {
    if (!text) return 0;
    return text.replace(/[^a-zA-ZæøåÆØÅ]/g, '').length;
}

function isTextGarbage(text) {
    if (!text || text.length < 5) return true;
    const letterCount = countLetters(text);
    // If less than 35% of the content is actual letters, it's likely noise/garbage
    return (letterCount / text.length) < 0.35;
}

// Advanced Preprocessing: High Contrast Grayscale (Better than hard threshold)
function preprocessImageForOCR(imgElement, rotateDegrees) {
    const canvas = document.createElement('canvas');
    const ctx = canvas.getContext('2d');

    // Swap dimensions for rotation
    if (rotateDegrees === 90 || rotateDegrees === 270) {
        canvas.width = imgElement.height;
        canvas.height = imgElement.width;
    } else {
        canvas.width = imgElement.width;
        canvas.height = imgElement.height;
    }

    // 1. ROTATE
    ctx.translate(canvas.width / 2, canvas.height / 2);
    ctx.rotate(rotateDegrees * Math.PI / 180);
    ctx.drawImage(imgElement, -imgElement.width / 2, -imgElement.height / 2);

    // 2. CONTRAST & GRAYSCALE
    const imageData = ctx.getImageData(0, 0, canvas.width, canvas.height);
    const data = imageData.data;

    const contrast = 1.5; // Increase contrast by 50%
    const intercept = 128 * (1 - contrast);

    for (let i = 0; i < data.length; i += 4) {
        const r = data[i];
        const g = data[i + 1];
        const b = data[i + 2];

        // Standard Grayscale
        let gray = 0.2126 * r + 0.7152 * g + 0.0722 * b;

        // Apply High Contrast
        gray = (gray * contrast) + intercept;

        // Clamp 0-255
        gray = Math.max(0, Math.min(255, gray));

        data[i] = gray;
        data[i + 1] = gray;
        data[i + 2] = gray;
    }
    ctx.putImageData(imageData, 0, 0);

    return canvas.toDataURL('image/jpeg', 1.0);
}