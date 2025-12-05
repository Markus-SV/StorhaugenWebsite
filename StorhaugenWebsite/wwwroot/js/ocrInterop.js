window.ocrInterop = {
    recognizeTextFromImage: async (imageSource) => {
        console.log("Starting Smart OCR...");

        // Hjelpefunksjon for å rotere bilde (bruker Canvas)
        const rotateBase64 = (base64, degrees) => {
            return new Promise((resolve) => {
                const img = new Image();
                img.onload = () => {
                    const canvas = document.createElement("canvas");
                    const ctx = canvas.getContext("2d");

                    // Bytt høyde/bredde hvis vi roterer 90 grader
                    if (degrees === 90 || degrees === 270) {
                        canvas.width = img.height;
                        canvas.height = img.width;
                    } else {
                        canvas.width = img.width;
                        canvas.height = img.height;
                    }

                    // Flytt origo til midten og roter
                    ctx.translate(canvas.width / 2, canvas.height / 2);
                    ctx.rotate(degrees * Math.PI / 180);
                    ctx.drawImage(img, -img.width / 2, -img.height / 2);

                    resolve(canvas.toDataURL("image/jpeg"));
                };
                img.src = base64;
            });
        };

        try {
            const worker = await Tesseract.createWorker(['nor', 'eng']);

            // PSM 3 er standard og tryggest for blandet innhold
            await worker.setParameters({
                tessedit_pageseg_mode: '3',
            });

            // --- FORSØK 1: Original retning ---
            let ret = await worker.recognize(imageSource);
            console.log("Attempt 1 (Original):", ret.data.text.substring(0, 20) + "...", "Confidence:", ret.data.confidence);

            // Sjekk om resultatet er dårlig (Lav confidence eller veldig kort tekst)
            // Hello Fresh titler er vanligvis tydelige, så confidence bør være over 70.
            if (ret.data.confidence < 75 || ret.data.text.length < 5) {

                console.log("Low confidence. Rotating image 90 degrees and retrying...");

                // Roter bildet 90 grader (med klokka)
                const rotatedImage = await rotateBase64(imageSource, 90);

                // --- FORSØK 2: Rotert 90 grader ---
                const ret2 = await worker.recognize(rotatedImage);
                console.log("Attempt 2 (Rotated 90):", ret2.data.text.substring(0, 20) + "...", "Confidence:", ret2.data.confidence);

                // Hvis dette resultatet er bedre, bruk det
                if (ret2.data.confidence > ret.data.confidence) {
                    ret = ret2;
                }

                // (Valgfritt: Man kan legge til en sjekk for 270 grader her også hvis man ofte holder telefonen "feil" vei)
            }

            await worker.terminate();
            return ret.data.text;

        } catch (error) {
            console.error("OCR Error:", error);
            throw error;
        }
    }
};