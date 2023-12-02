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

            public SoxWrapperOptionsBuilder WithExtractCommand(int secondsToExtract)
            {
                _options.Command = @$"input.wav output.wav trim -{secondsToExtract}";
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
                try
                {
                    using (var process = new Process())
                    {
                        process.StartInfo = new()
                        {
                            FileName = ExeLocation,
                            Arguments = Command,
                            UseShellExecute = false,
                            RedirectStandardInput = true,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                        };

                            process.Start();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error executing Sox: {ex.Message}");
                    throw; // Rethrow the exception after logging
                }

            }
        }
    }
}
