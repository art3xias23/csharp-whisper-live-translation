using Art3xias.SoxWrapper;
using Whisper.net;
using Whisper.net.Ggml;
using Whisper.net.Logger;

namespace Logic
{
    public class SpeechToTextPackage
    {
        public SpeechToTextPackage()
        {
            //if (!File.Exists(modelFileName))
            //{
            //     DownloadModel(modelFileName, ggmlType);
            //}
        }
        private static WhisperProcessor _processor;
        private string input  = "input";
        private string output  = "output";
        private string ext = ".wav";
        private int counter = 0;
        public async Task<string> TranslateAsync(byte[] data)
        {

            System.Diagnostics.Debug.WriteLine(nameof(TranslateAsync));
            string inCurrentFileName = input + counter + ext;
            string outCurrentFileName = output + counter + ext;

            System.Diagnostics.Debug.WriteLine(nameof(inCurrentFileName)+": "+ inCurrentFileName);
            System.Diagnostics.Debug.WriteLine(nameof(outCurrentFileName)+": "+ outCurrentFileName);

            var ggmlType = GgmlType.Base;

            var modelFileName = "ggml-model-whisper-base.en.bin";
            await File.WriteAllBytesAsync(inCurrentFileName, data);

            counter++ ;
            //WavDetails.PrintWavDetials(null, inCurrentFileName);

            new SoxWrapperClient()
                .WithOptions()
                .WithExeLocation(
                    @"C:\Program Files (x86)\sox-14-4-2\sox.exe")
                .WithConvertCommand(inCurrentFileName, outCurrentFileName)
                .Build()
                .Execute();

            //WavDetails.PrintWavDetials(null, outCurrentFileName);
            await using var fileStream = File.OpenRead(outCurrentFileName);

            // Optional logging from the native library
            //LogProvider.Instance.OnLog += (level, message) =>
            //{
            //    System.Diagnostics.Debug.WriteLine($"{level}: {message}");
            //};

            var text = string.Empty;

            await using var processor =  WhisperFactory.FromPath(modelFileName)
                .CreateBuilder()
              .WithLanguage("en")
              .Build();
            await foreach (var result in processor.ProcessAsync(fileStream))
            {
                text += result.Text + " ";
                System.Diagnostics.Debug.WriteLine($"{result.Start}->{result.End}: {result.Text}");
            }
System.Diagnostics.Debug.WriteLine("Returning text");
            return text;
        }

        private static async Task DownloadModel(string fileName, GgmlType ggmlType)
        {
            try
            {

                Console.WriteLine($"Downloading Model {fileName}");
                using var modelStream = await WhisperGgmlDownloader.GetGgmlModelAsync(ggmlType);
                using var fileWriter = File.OpenWrite(fileName);
                await modelStream.CopyToAsync(fileWriter);
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e);
                throw;
            }
        }
    }
}
