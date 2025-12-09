// ==========================================
// 2. IMAGE TOOLS (Resize & Processing)
// ==========================================
window.imageTools = {
    processInputFile: async (inputId) => {
        const input = document.getElementById(inputId);
        if (!input || !input.files || input.files.length === 0) return [];

        const processedImages = [];
        for (let i = 0; i < input.files.length; i++) {
            const file = input.files[i];
            try {
                // Resize to max 1500px (increased slightly for better OCR detail)
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
            img.onerror = reject;
        };
        reader.onerror = reject;
    });
}

// ==========================================
// 3. OCR INTEROP (Text Recognition with Pre-processing)
// ==========================================
window.ocrInterop = {
    recognizeTextFromImage: async (base64Image) => {
        if (typeof Tesseract === 'undefined') {
            console.error("Tesseract.js is not loaded.");
            return "Feil: Tesseract library mangler.";
        }

        try {
            console.log("Starting OCR Pre-processing...");

            // 1. Create an image object from the base64
            const img = new Image();
            img.src = base64Image;
            await new Promise(r => img.onload = r);

            // 2. Pre-process the image (Grayscale + Binarization)
            // This turns the image into strict Black & White, removing food texture noise
            const processedBase64 = preprocessImageForOCR(img);

            console.log("Sending processed image to Tesseract...");

            // 3. Run recognition with specific tweaks
            const result = await Tesseract.recognize(
                processedBase64,
                'nor', // Norwegian
                {
                    logger: m => console.log(m),
                    // tessedit_char_whitelist: 'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyzæøåÆØÅ0123456789.,- ', // Optional: Restrict chars if needed
                }
            );

            return result.data.text;
        } catch (error) {
            console.error("OCR Error:", error);
            return null;
        }
    }
};

// Helper: Converts image to high-contrast B&W for better OCR
function preprocessImageForOCR(imgElement) {
    const canvas = document.createElement('canvas');
    canvas.width = imgElement.width;
    canvas.height = imgElement.height;
    const ctx = canvas.getContext('2d');
    ctx.drawImage(imgElement, 0, 0);

    const imageData = ctx.getImageData(0, 0, canvas.width, canvas.height);
    const data = imageData.data;

    // Loop through pixels
    for (let i = 0; i < data.length; i += 4) {
        const r = data[i];
        const g = data[i + 1];
        const b = data[i + 2];

        // Standard Grayscale formula (Luminosity)
        const gray = 0.2126 * r + 0.7152 * g + 0.0722 * b;

        // Binarization (Thresholding)
        // If the pixel is light enough, make it pure white. Otherwise, pure black.
        // Threshold of 140 usually works well for recipe cards.
        // This removes the dark food textures and keeps the white/black text contrast.
        const threshold = 140;

        // Inversion check: If text is white on dark background (like HelloFresh titles often are)
        // We actually want the TEXT to be black and background WHITE for Tesseract.
        // But simple binarization usually handles this by making light text white and dark background black.
        // Tesseract handles White-on-Black okay, but prefers Black-on-White.

        const val = (gray > threshold) ? 255 : 0;

        data[i] = val;     // R
        data[i + 1] = val; // G
        data[i + 2] = val; // B
    }

    ctx.putImageData(imageData, 0, 0);
    return canvas.toDataURL('image/jpeg');
}