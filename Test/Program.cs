

using Art3xias.SoxWrapper;
using Logic;
using NAudio.Wave;

async Task NaudioTest()
{
    var waveIn = new WaveInEvent();
    var sptt = new SpeechToTextPackage();
    waveIn.DataAvailable += async (sender, e) =>
    {
        // e.Buffer contains the audio data

        // Send the audio data to Whisper for transcription
        var transcription = await sptt.TranslateAsync(e.Buffer);

        // Handle the transcription (update UI, etc.)
        System.Diagnostics.Debug.WriteLine("Transcription: " + transcription);
    };

    waveIn.StartRecording();

    Console.WriteLine("Press Enter to stop recording...");
    Console.ReadLine();

    waveIn.StopRecording();
    waveIn.Dispose();
}

NaudioTest();

async Task SoxClientTest()
{
    var inputFileName = "input.wav";
    var outputFileName = "output.wav";

//var byteData = File.ReadAllBytes("input.wav");
    new SoxWrapperClient()
        .WithOptions()
        .WithExeLocation(@"C:\Program Files (x86)\sox-14-4-2\sox.exe")
        //.WithExeLocation(System.Environment.GetEnvironmentVariable("sox"))
        //.WithExeLocation("cmd.exe")
        //.WithInputData(byteData)
        .WithConvertCommand(3, inputFileName, outputFileName)
        .Build()
        .Execute();

    var translator = new SpeechToTextPackage();
    //await translator.TranslateAsync("output.wav");
}

await SoxClientTest();

//File.WriteAllBytes("output.wav",output);