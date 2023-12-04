

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
        int intervalTime = 5000; // 5 seconds in milliseconds
        int overlapTime = 1000; // 1 second in milliseconds

        while (true)
        {
            byte[] data1 = CaptureAudio(intervalTime);

            Thread.Sleep(overlapTime);

            byte[] data2 = CaptureAudio(intervalTime);

            byte[] mergedData = MergeSegments(data1, data2);

            ProcessData(mergedData);
        }
static byte[] CaptureAudio(int duration)
{
    using (var waveIn = new WaveInEvent())
    {
        var buffer = new BufferedWaveProvider(waveIn.WaveFormat);
        byte[] audioData;

        waveIn.DataAvailable += (sender, args) =>
        {
            buffer.AddSamples(args.Buffer, 0, args.BytesRecorded);
        };

        waveIn.StartRecording();
        Thread.Sleep(duration);
        waveIn.StopRecording();

        // Read directly from the BufferedWaveProvider into a byte array
        int bytesRead;
        byte[] readBuffer = new byte[buffer.BufferLength];
        bytesRead = buffer.Read(readBuffer, 0, readBuffer.Length);

        audioData = new byte[bytesRead];
        Buffer.BlockCopy(readBuffer, 0, audioData, 0, bytesRead);

        // Print some details about the captured data
        Console.WriteLine($"Captured {bytesRead} bytes of audio data.");
        Console.WriteLine($"Sample rate: {waveIn.WaveFormat.SampleRate} Hz, Channels: {waveIn.WaveFormat.Channels}, Bits per sample: {waveIn.WaveFormat.BitsPerSample}");

        return audioData;
    }
}

    static byte[] MergeSegments(byte[] segment1, byte[] segment2)
    {
        int commonSegmentLength = IdentifyCommonSegment(segment1, segment2);

        if (commonSegmentLength > 0)
        {
            return segment1.Concat(segment2.Skip(commonSegmentLength)).ToArray();
        }

        return segment1.Concat(segment2).ToArray();
    }

    static int IdentifyCommonSegment(byte[] segment1, byte[] segment2)
    {
        int maxLength = Math.Min(segment1.Length, segment2.Length);

        int commonLength = 0;
        for (int i = 1; i <= maxLength; i++)
        {
            if (segment1[segment1.Length - i] == segment2[i - 1])
            {
                commonLength = i;
            }
            else
            {
                break;
            }
        }

        return commonLength;
    }

    static void ProcessData(byte[] data)
    {
    var translator = new SpeechToTextPackage().TranslateAsync(data);
    System.Diagnostics.Debug.WriteLine(translator);
        // Implement your logic to transcribe, analyze, or perform any other processing on the audio data
    }
