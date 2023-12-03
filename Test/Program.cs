

using Art3xias.SoxWrapper;
using Logic;

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
    await translator.TranslateAsync("output.wav");

//File.WriteAllBytes("output.wav",output);