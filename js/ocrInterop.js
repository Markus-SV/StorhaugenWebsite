window.ocrInterop = {
    recognizeTextFromImage: async (imageSource) => {
        console.log("Starting OCR...");
        try {
            // Last ned språkfiler
            const worker = await Tesseract.createWorker(['nor', 'eng']);

            // Endre parametere:
            // PSM 3 er standard og fungerer oftest best for blandet innhold.
            // PSM 6 er bra hvis du cropper bildet til kun tekst, men 3 er tryggest her.
            await worker.setParameters({
                tessedit_pageseg_mode: '3',
            });

            const ret = await worker.recognize(imageSource);
            console.log("OCR result:", ret.data.text);

            // Logg konfidensnivået for debugging
            console.log("Confidence:", ret.data.confidence);

            await worker.terminate();
            return ret.data.text;
        } catch (error) {
            console.error("OCR Error:", error);
            throw error;
        }
    }
};