window.ocrInterop = {
    recognizeTextFromImage: async (imageSource) => {
        console.log("Starting Smart OCR (Optimized)...");

        // Hjelpefunksjon: Endrer størrelse, roterer og gjør om til Grayscale
        const processImage = (base64, degrees) => {
            return new Promise((resolve) => {
                const img = new Image();
                img.onload = () => {
                    const canvas = document.createElement("canvas");
                    const ctx = canvas.getContext("2d");

                    // 1. Beregn skalering (Max 1800px for å spare minne på iPhone)
                    let width = img.width;
                    let height = img.height;
                    const maxDim = 1800;

                    if (width > maxDim || height > maxDim) {
                        const ratio = Math.min(maxDim / width, maxDim / height);
                        width = Math.floor(width * ratio);
                        height = Math.floor(height * ratio);
                    }

                    // 2. Sett Canvas-størrelse basert på rotasjon
                    // Hvis vi roterer 90/270 grader, bytter vi bredde og høyde på canvaset
                    if (degrees === 90 || degrees === 270) {
                        canvas.width = height;
                        canvas.height = width;
                    } else {
                        canvas.width = width;
                        canvas.height = height;
                    }

                    // 3. Roter og tegn bildet
                    ctx.translate(canvas.width / 2, canvas.height / 2);
                    ctx.rotate(degrees * Math.PI / 180);
                    // Tegn bildet sentrert (bruk width/height fra skaleringen over)
                    ctx.drawImage(img, -width / 2, -height / 2, width, height);

                    // 4. Gjør om til Grayscale (Bedre kontrast for tekst)
                    const imageData = ctx.getImageData(0, 0, canvas.width, canvas.height);
                    const data = imageData.data;
                    for (let i = 0; i < data.length; i += 4) {
                        // Luminans-formel
                        const avg = (0.299 * data[i]) + (0.587 * data[i + 1]) + (0.114 * data[i + 2]);
                        data[i] = avg;     // R
                        data[i + 1] = avg; // G
                        data[i + 2] = avg; // B
                    }
                    ctx.putImageData(imageData, 0, 0);

                    // Returner høykvalitets JPEG (1.0) til Tesseract
                    resolve(canvas.toDataURL("image/jpeg", 1.0));
                };
                img.src = base64;
            });
        };

        try {
            // Initialiser Tesseract Worker
            const worker = await Tesseract.createWorker(['nor', 'eng']);

            await worker.setParameters({
                tessedit_pageseg_mode: '3', // Auto segmentation
                // Whitelist sikrer at vi ikke får rare symboler, men tillater norske tegn og tall
                tessedit_char_whitelist: 'ABCDEFGHIJKLMNOPQRSTUVWXYZÆØÅabcdefghijklmnopqrstuvwxyzæøå0123456789-,. \n'
            });

            let bestResult = { text: "", confidence: 0 };

            // Vi tester to vinkler:
            // 0°   = Hvis brukeren holder telefonen likt som kortet
            // 270° = Standard for Hello Fresh (Liggende kort, stående telefon)
            const anglesToTest = [0, 270];

            for (let angle of anglesToTest) {
                console.log(`Processing angle: ${angle}°...`);

                // Prosesser bildet (nedskalering + rotasjon + grayscale)
                const processedImage = await processImage(imageSource, angle);

                // Kjør selve OCR-jobben
                const ret = await worker.recognize(processedImage);

                const cleanText = ret.data.text.trim();
                console.log(`Result ${angle}° (Conf: ${Math.round(ret.data.confidence)}%): "${cleanText.substring(0, 20)}..."`);

                // Lagre resultatet hvis det er bedre enn forrige forsøk
                if (ret.data.confidence > bestResult.confidence) {
                    bestResult = {
                        text: cleanText,
                        confidence: ret.data.confidence
                    };
                }

                // "Early exit": Hvis vi er veldig sikre (>80%), stopp her for å spare tid
                if (ret.data.confidence > 80 && cleanText.length > 5) {
                    console.log("High confidence match found, stopping loop.");
                    break;
                }
            }

            // Rydd opp worker for å frigjøre minne
            await worker.terminate();

            let finalText = bestResult.text;

            // Enkel "Clean up" av vanlige OCR-feil
            if (finalText) {
                finalText = finalText
                    .replace(/\bSto\b/g, "Stekt")
                    .replace(/ilet\b/g, "filet")
                    .replace(/llet\b/g, "filet")
                    .replace(/\|/g, "I"); // Noen ganger tolkes 'I' som pipe '|'
            }

            console.log("Sending text to C#:", finalText ? finalText.substring(0, 50) + "..." : "Empty");
            return finalText;

        } catch (error) {
            console.error("OCR Error:", error);
            // Ikke krasj appen, returner heller tom streng
            return "";
        }
    }
};