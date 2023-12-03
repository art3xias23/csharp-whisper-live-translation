﻿using System.ComponentModel.DataAnnotations;
using Whisper.net;
using Whisper.net.Ggml;
using Whisper.net.Logger;
using Whisper.net.Wave;

namespace Logic
{
    public class SpeechToTextPackage
    {
        public async Task TranslateAsync(string wavFileName)
        {
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

            using var fileStream = File.OpenRead(wavFileName);

            WavDetails.PrintWavDetials(null, wavFileName);

            // This section processes the audio file and prints the results (start time, end time and text) to the console.
            await foreach (var result in processor.ProcessAsync(fileStream))
            {
                System.Diagnostics.Debug.WriteLine($"{result.Start}->{result.End}: {result.Text}");
            }
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
