// ==========================================
// 1. CAMERA INTEROP (Video Stream & Capture)
// ==========================================
window.cameraInterop = {
    stream: null,

    start: async (videoElement) => {
        // 1. FORCE STOP existing streams first to prevent "Permission" errors
        if (window.cameraInterop.stream) {
            window.cameraInterop.stream.getTracks().forEach(track => track.stop());
        }

        try {
            const constraints = {
                audio: false,
                video: {
                    facingMode: 'environment',
                    width: { ideal: 1920 },
                    height: { ideal: 1080 }
                }
            };
            const stream = await navigator.mediaDevices.getUserMedia(constraints);
            videoElement.srcObject = stream;
            window.cameraInterop.stream = stream;
        }
        catch (err) {
            console.error("Camera Error:", err);
            // Don't show alert here to avoid spamming user, UI handles the error state
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
                // Resize to max 1500px for speed/quality balance
                const base64 = await resizeImage(file, 1500, 1500);
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
// 3. OCR INTEROP (Rotation & Binarization)
// ==========================================
window.ocrInterop = {
    recognizeTextFromImage: async (base64Image) => {
        if (typeof Tesseract === 'undefined') return "Error: Tesseract missing";

        try {
            console.log("Starting OCR...");

            // 1. Create Image Object
            const img = new Image();
            img.src = base64Image;
            await new Promise(r => img.onload = r);

            // 2. ATTEMPT 1: Normal Orientation (Pre-processed)
            console.log("Attempt 1: Normal orientation");
            let processedImage = preprocessImageForOCR(img, 0); // 0 degrees
            let result = await runTesseract(processedImage);

            // 3. CHECK: If result is garbage (short or low confidence), TRY ROTATING
            if (isResultPoor(result.data.text)) {
                console.log("Result poor. Attempt 2: Rotating 90 degrees (Landscape mode)...");

                // Rotate 90 degrees (simulates landscape card in portrait phone)
                processedImage = preprocessImageForOCR(img, 90);
                const resultRotated = await runTesseract(processedImage);

                // If rotated result is longer/better, use that instead
                if (resultRotated.data.text.length > result.data.text.length) {
                    console.log("Rotation improved result!");
                    result = resultRotated;
                }
            }

            return result.data.text;

        } catch (error) {
            console.error("OCR Error:", error);
            return null;
        }
    }
};

// Helper: Run Tesseract
async function runTesseract(imageData) {
    return await Tesseract.recognize(imageData, 'nor', {
        logger: m => console.log(m)
    });
}

// Helper: Check if text looks like garbage
function isResultPoor(text) {
    // If text is empty, very short, or just symbols like "i | å Bn: (/"
    if (!text || text.length < 5) return true;
    const clean = text.replace(/[^a-zA-ZæøåÆØÅ]/g, '');
    return clean.length < 3;
}

// Helper: Rotates AND Binarizes (Black/White) in one step
function preprocessImageForOCR(imgElement, rotateDegrees) {
    const canvas = document.createElement('canvas');
    const ctx = canvas.getContext('2d');

    // Handle Dimensions swapping for 90/270 degree rotation
    if (rotateDegrees === 90 || rotateDegrees === 270) {
        canvas.width = imgElement.height;
        canvas.height = imgElement.width;
    } else {
        canvas.width = imgElement.width;
        canvas.height = imgElement.height;
    }

    // 1. ROTATION
    ctx.translate(canvas.width / 2, canvas.height / 2);
    ctx.rotate(rotateDegrees * Math.PI / 180);
    ctx.drawImage(imgElement, -imgElement.width / 2, -imgElement.height / 2);

    // 2. BINARIZATION (Make it Black & White)
    // We access the pixels of the rotated image
    const imageData = ctx.getImageData(0, 0, canvas.width, canvas.height);
    const data = imageData.data;

    for (let i = 0; i < data.length; i += 4) {
        const r = data[i];
        const g = data[i + 1];
        const b = data[i + 2];
        const gray = 0.2126 * r + 0.7152 * g + 0.0722 * b;

        // High contrast threshold
        const val = (gray > 130) ? 255 : 0;

        data[i] = val;
        data[i + 1] = val;
        data[i + 2] = val;
    }
    ctx.putImageData(imageData, 0, 0);

    return canvas.toDataURL('image/jpeg');
}