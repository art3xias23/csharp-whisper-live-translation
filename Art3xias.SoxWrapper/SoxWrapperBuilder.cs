using System.Diagnostics;
using System.IO.Pipes;

namespace Art3xias.SoxWrapper
{
    public class SoxWrapperClient
    {
        private SoxWrapperOptions _builderOptions;
        public SoxWrapperOptionsBuilder WithOptions() => new();


        public class SoxWrapperOptionsBuilder
        {
            private SoxWrapperOptions _options { get; }

            public SoxWrapperOptionsBuilder()
            {
                _options = new SoxWrapperOptions();
            }

            public SoxWrapperOptionsBuilder WithExeLocation(string exeLocation)
            {
                _options.ExeLocation = exeLocation;
                return this;
            }

            public SoxWrapperOptionsBuilder WithExtractCommand(int secondsToExtract, string inputFileName, string outputFileName)
            {
                _options.Command = " input.wav output.wav trim -3";
                return this;
            }
            public SoxWrapperOptionsBuilder WithConvertCommand(string inputFileName, string outputFileName)
            {
                _options.Command = $"{inputFileName} -r 16000 -e signed-integer {outputFileName}";
                return this;
            }

            public SoxWrapperOptionsBuilder WithInputData(byte[] inputData)
            {
                _options.InputData = inputData;
                return this;
            }

            public SoxWrapperOptions Build()
            {
                return _options;
            }
        }

        public class SoxWrapperOptions
        {
            public string ExeLocation { get; set; }
            public string Command { get; set; }
            public byte[] InputData { get; set; }

            public void Execute()
            {
                System.Diagnostics.Debug.WriteLine("ExecuteSox");
                try
                {
                    using (var process = new Process())
                    {
                        process.StartInfo = new()
                        {
                            FileName = ExeLocation,
                            Arguments = Command,
                            UseShellExecute = false,
                            CreateNoWindow = true,
                            RedirectStandardInput = true,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            WorkingDirectory = ""
                        };

                        process.Start();

                        string errorOutput = process.StandardError.ReadToEnd();
                        System.Diagnostics.Debug.WriteLine($"Error output from sox:\n{errorOutput}");

                        process.WaitForExit();
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error executing Sox: {ex.Message}");
                    throw; // Rethrow the exception after logging
                }

            }
        }
    }
}
