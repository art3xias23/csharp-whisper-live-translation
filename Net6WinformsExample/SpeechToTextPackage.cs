using Art3xias.SoxWrapper;
using Whisper.net;
using Whisper.net.Ggml;
using Whisper.net.Logger;

namespace WinformsTranslation
{
    public class SpeechToTextPackage
    {
        public static async Task<string> TranslateAsync(string fileName)
        {
            ConvertBytesToFile();
            var ggmlType = GgmlType.Base;
            var modelFileName = "ggml-model-whisper-base.en.bin";

            // This section detects whether the "ggml-base.bin" file exists in our project disk. If it doesn't, it downloads it from the internet
            if (!File.Exists(modelFileName))
            {
                await DownloadModel(modelFileName, ggmlType);
            }

            // Optional logging from the native library
            LogProvider.Instance.OnLog += (level, message) =>
            {
                System.Diagnostics.Debug.WriteLine($"{level}: {message}");
            };

            // This section creates the whisperFactory object which is used to create the processor object.
            using var whisperFactory = WhisperFactory.FromPath(modelFileName);

            // This section creates the processor object which is used to process the audio file, it uses language `auto` to detect the language of the audio file.
            using var processor = whisperFactory.CreateBuilder()
                .WithLanguage("auto")
                .Build();

            using var fileStream = File.OpenRead("output.wav");

            WavDetails.PrintWavDetials(await File.ReadAllBytesAsync("output.wav"),"");

            // This section processes the audio file and prints the results (start time, end time and text) to the console.
            var text = string.Empty;
            await foreach (var result in processor.ProcessAsync(fileStream))
            {
                text = text + " " + result.Text;
                Console.WriteLine($"{result.Start}->{result.End}: {result.Text}");
            }
System.Diagnostics.Debug.WriteLine("Text: ", text);
            return text;
        }

        private static async Task DownloadModel(string fileName, GgmlType ggmlType)
        {
            Console.WriteLine($"Downloading Model {fileName}");
            using var modelStream = await WhisperGgmlDownloader.GetGgmlModelAsync(ggmlType);
            using var fileWriter = File.OpenWrite(fileName);
            await modelStream.CopyToAsync(fileWriter);
        }

       private static void ConvertBytesToFile()
        {
            new SoxWrapperClient()
                .WithOptions()
                .WithExeLocation(@"C:\Program Files (x86)\sox-14-4-2\sox.exe")
                //.WithExeLocation(System.Environment.GetEnvironmentVariable("sox"))
                //.WithExeLocation("cmd.exe")
                //.WithInputData(byteData)
                .WithConvertCommand(3, "","")
                .Build()
                .Execute();
        }
    }
}
