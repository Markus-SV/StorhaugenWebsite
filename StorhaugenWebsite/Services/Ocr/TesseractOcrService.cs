using Microsoft.JSInterop;

namespace StorhaugenWebsite.Services
{
    // The ": IOcrService" part tells C# that this class fulfills the IOcrService contract
    public class TesseractOcrService : IOcrService
    {
        private readonly IJSRuntime _jsRuntime;

        // We inject the JS Runtime so we can talk to the JavaScript file you added
        public TesseractOcrService(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        public async Task<string> RecognizeTextAsync(string imageSource)
        {
            // This line calls the 'window.ocrInterop.recognizeTextFromImage' function 
            // inside your wwwroot/js/ocrInterop.js file
            return await _jsRuntime.InvokeAsync<string>("ocrInterop.recognizeTextFromImage", imageSource);
        }
    }
}