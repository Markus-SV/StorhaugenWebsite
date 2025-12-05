window.ocrInterop = {
    recognizeTextFromImage: async (imageSource) => {
        console.log("Starting Robust OCR...");

        // Hjelpefunksjon: Roterer bildet OG gjør det om til svart/hvitt for bedre kontrast
        const processImage = (base64, degrees) => {
            return new Promise((resolve) => {
                const img = new Image();
                img.onload = () => {
                    const canvas = document.createElement("canvas");
                    const ctx = canvas.getContext("2d");

                    // 1. Sett riktig størrelse basert på rotasjon
                    if (degrees === 90 || degrees === 270) {
                        canvas.width = img.height;
                        canvas.height = img.width;
                    } else {
                        canvas.width = img.width;
                        canvas.height = img.height;
                    }

                    // 2. Roter Canvas
                    ctx.translate(canvas.width / 2, canvas.height / 2);
                    ctx.rotate(degrees * Math.PI / 180);
                    ctx.drawImage(img, -img.width / 2, -img.height / 2);

                    // ... inni processImage funksjonen ...

                    // 3. Gjør om til Grayscale (Bedre for tynne bokstaver enn hard kontrast)
                    const imageData = ctx.getImageData(0, 0, canvas.width, canvas.height);
                    const data = imageData.data;

                    for (let i = 0; i < data.length; i += 4) {
                        // Standard formel for å gjøre farger om til gråtoner som øyet ser dem
                        const avg = (0.299 * data[i]) + (0.587 * data[i + 1]) + (0.114 * data[i + 2]);

                        data[i] = avg; // R
                        data[i + 1] = avg; // G
                        data[i + 2] = avg; // B
                        // Vi rører ikke Alpha (gjennomsiktighet)
                    }

                    ctx.putImageData(imageData, 0, 0);

                    // Vi øker kvaliteten til 1.0 (maks) for å ikke miste detaljer i komprimering
                    resolve(canvas.toDataURL("image/jpeg", 1.0));
                };
                img.src = base64;
            });
        };

        try {
            const worker = await Tesseract.createWorker(['nor', 'eng']);
            await worker.setParameters({
                tessedit_pageseg_mode: '3', // Auto segmentation
                tessedit_char_whitelist: 'ABCDEFGHIJKLMNOPQRSTUVWXYZÆØÅabcdefghijklmnopqrstuvwxyzæøå0123456789-, ' // Filtrer bort støy-tegn
            });

            let bestResult = { text: "", confidence: 0 };

            // Vi tester disse vinklene i rekkefølge
            const anglesToTest = [0, 90, 270]; // 180 er sjeldent nødvendig, men kan legges til

            for (let angle of anglesToTest) {
                console.log(`Processing angle: ${angle}°...`);

                const processedImage = await processImage(imageSource, angle);
                const ret = await worker.recognize(processedImage);

                console.log(`Result ${angle}°: "${ret.data.text.replace(/\n/g, ' ').substring(0, 20)}..." (Conf: ${ret.data.confidence})`);

                // Vi lagrer det resultatet som har høyest selvtillit
                if (ret.data.confidence > bestResult.confidence) {
                    bestResult = {
                        text: ret.data.text,
                        confidence: ret.data.confidence
                    };
                }

                // Hvis vi treffer "jackpot" (veldig høy confidence), avslutt tidlig for å spare tid
                if (ret.data.confidence > 85 && ret.data.text.length > 5) {
                    console.log("High confidence match found, stopping search.");
                    break;
                }
            }

            await worker.terminate();

            console.log("Winner text:", bestResult.text);
            return bestResult.text;

        } catch (error) {
            console.error("OCR Error:", error);
            throw error;
        }
    }
};