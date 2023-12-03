using Newtonsoft.Json;

namespace WinformsTranslation
{
    public class SpeechToTextApi
    {
        public static async Task<string> SpToTextAsync(byte[] audioBytes)
        {
            string whisperUrl = "https://api.openai.com/v1/audio/transcriptions";
            string apiKey = Environment.GetEnvironmentVariable("WhisperApiKey");
            HttpClient http = new HttpClient();
            http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
            // add the audio bytes to the form data
            using var content = new MultipartFormDataContent
            {
                { new ByteArrayContent(audioBytes), "file", "audio.wav" },
                {new StringContent("whisper-1"), "model" }
            };
            HttpResponseMessage response = await http.PostAsync(whisperUrl, content);
            string responseText = await response.Content.ReadAsStringAsync();
            System.Diagnostics.Debug.WriteLine(responseText);
            dynamic responseJson = JsonConvert.DeserializeObject<dynamic>(responseText);
            return responseJson.text;
        }
    }
}
