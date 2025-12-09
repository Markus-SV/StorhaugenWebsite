// ==========================================
// 1. CAMERA INTEROP (Video Stream & Capture)
// ==========================================
window.cameraInterop = {
    stream: null,

    start: async (videoElement) => {
        // FORCE STOP: Kill any existing stream to fix "Permission" errors
        if (window.cameraInterop.stream) {
            window.cameraInterop.stream.getTracks().forEach(track => track.stop());
            window.cameraInterop.stream = null;
        }

        try {
            const constraints = {
                audio: false,
                video: {
                    facingMode: 'environment', // Back camera
                    width: { ideal: 1920 },    // 1080p is best for OCR
                    height: { ideal: 1080 }
                }
            };
            const stream = await navigator.mediaDevices.getUserMedia(constraints);
            videoElement.srcObject = stream;
            window.cameraInterop.stream = stream;
        }
        catch (err) {
            console.error("Camera Error:", err);
            // Alert removed to prevent spam, UI will show error state if needed
        }
    },

    capture: async (videoElement) => {
        const canvas = document.createElement("canvas");
        canvas.width = videoElement.videoWidth;
        canvas.height = videoElement.videoHeight;
        canvas.getContext('2d').drawImage(videoElement, 0, 0);
        return canvas.toDataURL("image/jpeg", 0.9);
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
                // Resize to max 1800px for better clarity on ingredients
                const base64 = await resizeImage(file, 1800, 1800);
                processedImages.push({ name: file.name, data: base64 });
            } catch (err) {
                console.error("Feil i imageTools:", err);
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

                // Keep aspect ratio
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
// 3. OCR INTEROP (With Rotation Logic)
// ==========================================
window.ocrInterop = {
    recognizeTextFromImage: async (base64Image) => {
        if (typeof Tesseract === 'undefined') return "Error: Tesseract missing";

        try {
            console.log("OCR Start...");
            const img = new Image();
            img.src = base64Image;
            await new Promise(r => img.onload = r);

            // --- ATTEMPT 1: Standard (0 degrees) ---
            console.log("Attempt 1: 0 deg");
            let processed = preprocessImageForOCR(img, 0);
            let result = await Tesseract.recognize(processed, 'nor');
            let text = result.data.text;

            // --- ATTEMPT 2: Rotate 90 deg (Landscape) ---
            // If text is garbage (short or mostly symbols), try rotating
            if (isTextGarbage(text)) {
                console.log("Text garbage. Attempt 2: 90 deg rotation...");
                processed = preprocessImageForOCR(img, 90);
                const result90 = await Tesseract.recognize(processed, 'nor');

                // If rotated result is better/longer, keep it
                if (result90.data.text.length > text.length) {
                    text = result90.data.text;
                }
            }

            return text;

        } catch (error) {
            console.error("OCR Error:", error);
            return null;
        }
    }
};

// Check if OCR result is "Garbage" (like your "; LL 4 Hi PE" error)
function isTextGarbage(text) {
    if (!text || text.length < 5) return true;
    // Count actual letters
    const letters = text.replace(/[^a-zA-ZæøåÆØÅ]/g, '').length;
    // If less than 40% of the result is letters, it's garbage (symbols/noise)
    return (letters / text.length) < 0.40;
}

// Process Image: Rotate -> Grayscale -> High Contrast
function preprocessImageForOCR(imgElement, rotateDegrees) {
    const canvas = document.createElement('canvas');
    const ctx = canvas.getContext('2d');

    // Swap width/height if rotating 90 degrees
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

    // 2. HIGH CONTRAST FILTER (Binarization)
    const imageData = ctx.getImageData(0, 0, canvas.width, canvas.height);
    const data = imageData.data;

    for (let i = 0; i < data.length; i += 4) {
        const r = data[i];
        const g = data[i + 1];
        const b = data[i + 2];

        // Luminosity
        const gray = 0.2126 * r + 0.7152 * g + 0.0722 * b;

        // Threshold: 135 is a good balance for HelloFresh cards
        // Anything lighter than 135 becomes pure white, darker becomes pure black.
        // This hides the food textures and colored circles.
        const val = (gray > 135) ? 255 : 0;

        data[i] = val;
        data[i + 1] = val;
        data[i + 2] = val;
    }
    ctx.putImageData(imageData, 0, 0);

    return canvas.toDataURL('image/jpeg');
}