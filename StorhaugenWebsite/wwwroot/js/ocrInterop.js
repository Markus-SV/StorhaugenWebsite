// wwwroot/js/ocrInterop.js
window.ocrInterop = {
    recognizeTextFromImage: async (imageSource) => {
        console.log("Starting OCR...");
        try {
            // 1. Create a worker (assuming English text for now)
            const worker = await Tesseract.createWorker('eng');

            // 2. Recognize text. This scans the whole image.
            // (For future optimization, we could restrict this to just the top 25% of the image)
            const ret = await worker.recognize(imageSource);
            console.log("OCR result:", ret.data.text);

            // 3. Terminate worker to free memory
            await worker.terminate();

            // 4. Return the raw text found
            return ret.data.text;
        } catch (error) {
            console.error("OCR Error:", error);
            throw error;
        }
    }
};