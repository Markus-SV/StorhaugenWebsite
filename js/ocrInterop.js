window.ocrInterop = {
    recognizeTextFromImage: async (imageSource) => {
        console.log("Starting Smart OCR (Optimized)...");

        // Hjelpefunksjon: Roterer og gjør om til Grayscale for bedre lesbarhet
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

                    // 3. Gjør om til Grayscale (Bevarer svake bokstaver som 'f' og 'l' bedre enn svart/hvitt)
                    const imageData = ctx.getImageData(0, 0, canvas.width, canvas.height);
                    const data = imageData.data;
                    for (let i = 0; i < data.length; i += 4) {
                        // Standard formel for luminans (hvordan øyet ser lysstyrke)
                        const avg = (0.299 * data[i]) + (0.587 * data[i + 1]) + (0.114 * data[i + 2]);
                        data[i] = avg;
                        data[i + 1] = avg;
                        data[i + 2] = avg;
                    }
                    ctx.putImageData(imageData, 0, 0);

                    // Høy kvalitet (1.0) til Tesseract
                    resolve(canvas.toDataURL("image/jpeg", 1.0));
                };
                img.src = base64;
            });
        };

        try {
            const worker = await Tesseract.createWorker(['nor', 'eng']);
            await worker.setParameters({
                tessedit_pageseg_mode: '3', // Auto segmentation
                // Whitelist hjelper å unngå rare tegn
                tessedit_char_whitelist: 'ABCDEFGHIJKLMNOPQRSTUVWXYZÆØÅabcdefghijklmnopqrstuvwxyzæøå0123456789-, \n' // La til \n her også for sikkerhets skyld
            });

            let bestResult = { text: "", confidence: 0 };

            // Vi sjekker 0° (liggende) og 270° (stående Hello Fresh kort)
            const anglesToTest = [0, 270];

            for (let angle of anglesToTest) {
                console.log(`Processing angle: ${angle}°...`);

                const processedImage = await processImage(imageSource, angle);
                const ret = await worker.recognize(processedImage);

                // --- HER ER ENDRINGEN: ---
                // Vi beholder linjeskift slik at C# kan se hva som er overskrift og hva som er tekst.
                const cleanText = ret.data.text.trim();

                console.log(`Result ${angle}°: "${cleanText.substring(0, 30)}..." (Conf: ${ret.data.confidence})`);

                // Lagre hvis dette er bedre enn forrige forsøk
                if (ret.data.confidence > bestResult.confidence) {
                    bestResult = {
                        text: cleanText,
                        confidence: ret.data.confidence
                    };
                }

                // Hvis vi finner en god match og teksten har innhold
                if (ret.data.confidence > 80 && cleanText.length > 4) {
                    console.log("High confidence match found, stopping search.");
                    break;
                }
            }

            await worker.terminate();

            // Siste sikkerhetsnett i JS før vi sender til C#
            let finalText = bestResult.text;

            // Enkle erstattelser (Fjernet "Sto" herfra, håndterer det heller i C# context hvis mulig, 
            // men lar det stå hvis det funker for deg)
            finalText = finalText.replace(/\bSto\b/g, "Stekt")
                .replace(/ilet\b/g, "filet")
                .replace(/llet\b/g, "filet");

            console.log("Winner text sent to C#:", finalText);
            return finalText;

        } catch (error) {
            console.error("OCR Error:", error);
            throw error;
        }
    }
};