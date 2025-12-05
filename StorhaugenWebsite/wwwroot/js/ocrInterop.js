window.ocrInterop = {
    recognizeTextFromImage: async (imageSource) => {
        console.log("Starting OCR...");
        try {
            // Endring 1: Vi legger til 'nor' for norsk språkstøtte
            // Dette laster ned litt mer data første gang, men gir mye bedre resultat på norske matvarer.
            const worker = await Tesseract.createWorker(['eng', 'nor']);

            // Endring 2: Vi setter parametere for å prøve å finne tekst selv om det er litt rotete
            // PSM 1 betyr "Automatic page segmentation with OSD" (Orientation detection)
            // Dette KAN hjelpe på roterte bilder, men fungerer ikke alltid perfekt i nettleseren.
            await worker.setParameters({
                tessedit_pageseg_mode: '1', 
            });

            const ret = await worker.recognize(imageSource);
            console.log("OCR result:", ret.data.text);

            await worker.terminate();
            return ret.data.text;
        } catch (error) {
            console.error("OCR Error:", error);
            throw error;
        }
    }
};