

using Art3xias.SoxWrapper;

var inputFileName = "input.wav";
var outputFileName = "output.wav";

var byteData = File.ReadAllBytes("input.wav");
 new SoxWrapperClient()
    .WithOptions()
    .WithExeLocation(@"C:\Program Files (x86)\sox-14-4-2\sox.exe")
    //.WithExeLocation(System.Environment.GetEnvironmentVariable("sox"))
    //.WithExeLocation("cmd.exe")
    //.WithInputData(byteData)
    .WithExtractCommand(3, inputFileName, outputFileName)
    .Build()
    .Execute();

//File.WriteAllBytes("output.wav",output);