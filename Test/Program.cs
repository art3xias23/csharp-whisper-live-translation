

using Art3xias.SoxWrapper;

var byteData = File.ReadAllBytes("input.wav");
 new SoxWrapperClient()
    .WithOptions()
    .WithExeLocation(@"C:\Program Files (x86)\sox-14-4-2\sox.exe")
    //.WithExeLocation("cmd.exe")
    .WithInputData(byteData)
    .WithExtractCommand(3)
    .Build()
    .Execute();

//File.WriteAllBytes("output.wav",output);