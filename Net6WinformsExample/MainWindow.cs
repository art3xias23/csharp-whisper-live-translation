using System;
using System.ComponentModel;
using Timer = System.Timers.Timer;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Timers;
using CSCore;
using CSCore.Codecs.WAV;
using CSCore.CoreAudioAPI;
using System.Windows.Forms;
using CSCore.SoundIn;
using CSCore.Streams;
using CSCore.Win32;
using WhisperLiveTranslation;
using WinformsTranslation;

namespace Recorder
{
    public partial class MainWindow : Form
    {
        private byte[] Result1 = Array.Empty<byte>();
        private MemoryStream Result1Mem = new MemoryStream();
        private byte[] Result2 = Array.Empty<byte>();
        private byte[] OverlapBuffer = Array.Empty<byte>();
        private MemoryStream OverlapBufferMem = new MemoryStream();

        private int IntervalTimeInSeconds = 5;
        private int OverlapTimeInSeconds = 2;

        private int counter = 0;
        //Change this to CaptureMode.Capture to capture a microphone,...
        private const CaptureMode CaptureMode = Recorder.CaptureMode.Capture;

        private MMDevice _selectedDevice;
        private WasapiCapture _soundIn;
        private IWriteable _writer;
        private MemoryStream _memStream;
        private readonly GraphVisualization _graphVisualization = new GraphVisualization();
        private IWaveSource _finalSource;

        private Timer _timer;

        public MMDevice SelectedDevice
        {
            get { return _selectedDevice; }
            set
            {
                _selectedDevice = value;
                if (value != null)
                    btnStart.Enabled = true;
            }
        }

        public string TextData { get; set; }

        public MainWindow()
        {
            InitializeComponent();

            textBox1.DataBindings.Add("Text", this, "TextData");

        }

        private void RefreshDevices()
        {
            deviceList.Items.Clear();

            using (var deviceEnumerator = new MMDeviceEnumerator())
            using (var deviceCollection = deviceEnumerator.EnumAudioEndpoints(
                CaptureMode == CaptureMode.Capture ? DataFlow.Capture : DataFlow.Render, DeviceState.Active))
            {
                foreach (var device in deviceCollection)
                {
                    var deviceFormat = WaveFormatFromBlob(device.PropertyStore[
                        new PropertyKey(new Guid(0xf19f064d, 0x82c, 0x4e27, 0xbc, 0x73, 0x68, 0x82, 0xa1, 0xbb, 0x8e, 0x4c), 0)].BlobValue);

                    var item = new ListViewItem(device.FriendlyName) { Tag = device };
                    item.SubItems.Add(deviceFormat.Channels.ToString(CultureInfo.InvariantCulture));

                    deviceList.Items.Add(item);
                }
            }
        }

        private void StartCapture()
        {
            if (SelectedDevice == null)
                return;

            if (CaptureMode == CaptureMode.Capture)
                _soundIn = new WasapiCapture();
            else
                _soundIn = new WasapiLoopbackCapture();

            _soundIn.Device = SelectedDevice;
            _soundIn.Initialize();

            var soundInSource = new SoundInSource(_soundIn);
            var singleBlockNotificationStream = new SingleBlockNotificationStream(soundInSource.ToSampleSource());
            _finalSource = singleBlockNotificationStream.ToWaveSource();
            _memStream = new MemoryStream();
            _writer = new WaveWriter(_memStream, _finalSource.WaveFormat);

            var buffer = new byte[_finalSource.WaveFormat.BytesPerSecond / 16];
            soundInSource.DataAvailable += (s, e) =>
            {
                int read;
                while ((read = _finalSource.Read(buffer, 0, buffer.Length)) > 0)
                    _writer.Write(buffer, 0, read);
            };

            singleBlockNotificationStream.SingleBlockRead += SingleBlockNotificationStreamOnSingleBlockRead;

            _soundIn.Start();

            _timer = new Timer(); 
            _timer.Interval = IntervalTimeInSeconds * 1000; // Convert to milliseconds
            _timer.Elapsed += ProcessAudio;
            _timer.Start();

        }
        private void ProcessSimpleAudio(object? sender, ElapsedEventArgs e)
        {

            _timer.Stop();
            _soundIn.Stop();
            var mem = _memStream.ToArray();
 
            Task.Run(async () => await ProcessData(mem));

            _memStream = new MemoryStream(); 
            _soundIn.Start();

            _timer.Start();
        }
        private void ProcessAudio(object? sender, ElapsedEventArgs e)
        {

            _timer.Stop();
 
            var memArray = _memStream.ToArray();
            var fileName = $"File{counter}.wav";
            File.WriteAllBytes(fileName, memArray);
            System.Diagnostics.Debug.WriteLine($"FileName: {fileName}");
            WavDetails.PrintWavDetials(memArray);

            counter++;
            var duplicateStream = new MemoryStream(memArray);
            Task.Run(async () => await ProcessData(_memStream.ToArray()));

            int secondsToRemove = 3;
            byte[] trimmedAudioBytes = RemoveDurationFromAudio(memArray, secondsToRemove, _finalSource.WaveFormat.SampleRate, _finalSource.WaveFormat.BytesPerSecond);
            _memStream = new MemoryStream(trimmedAudioBytes);

            _timer.Start();
        }

        static byte[] RemoveDurationFromAudio(byte[] audioBytes, int durationInSecondsToRemove, int sampleRate, int bytesPerSecond)
        {
            // Calculate the number of bytes to remove
            //int bytesPerSecond = 44100 * 2; // Assuming 44.1 kHz sample rate and 16-bit audio
            int bytesToRemove = durationInSecondsToRemove * bytesPerSecond;

            // Check if the specified duration exceeds the length of the audio
            if (bytesToRemove >= audioBytes.Length)
            {
                // Handle the case where the specified duration is longer than the audio
                throw new ArgumentException("Duration to remove exceeds the length of the audio.");
            }

            // Create a new byte array for the trimmed audio
            byte[] trimmedAudioBytes = new byte[audioBytes.Length - bytesToRemove];

            // Copy the audio data after the specified duration
            Array.Copy(audioBytes, bytesToRemove, trimmedAudioBytes, 0, trimmedAudioBytes.Length);

            return trimmedAudioBytes;
        }

        private void ProcessAudioBackUp(object? sender, ElapsedEventArgs e)
        {
            _timer.Stop();
            var memArray = _memStream.ToArray();
            Result1 = CombineAudio(memArray, OverlapBuffer);
            OverlapBuffer = ExtractOverlap(memArray, _finalSource.WaveFormat.SampleRate,
                _finalSource.WaveFormat.BytesPerSample, OverlapTimeInSeconds);
            var duplicateStream = new MemoryStream(memArray);

            Task.Run(async () => await ProcessData(duplicateStream.ToArray()));
            _memStream.SetLength(0); 
            _timer.Start();
        }

        private byte[] ExtractOverlap(byte[] audioBytes, int sampleRate, int bytesPerSample, int durationInSeconds)
        {
            var startingPoint = 0;
            var byteCount = durationInSeconds * sampleRate * bytesPerSample;
            if (byteCount > audioBytes.Length)
            {
                throw new ArgumentException("Invalid extraction parameters: The specified duration exceeds the length of the audio array");
            }

            byte[] extractedAudio = new byte[byteCount];
            Array.Copy(audioBytes, startingPoint, extractedAudio, 0, byteCount);

            return extractedAudio;

        }

        private byte[] CombineAudio(byte[] audio1Bytes, byte[] audio2Bytes)
        {
            byte[] concatenatedArray = new byte[audio1Bytes.Length + audio2Bytes.Length];
            Buffer.BlockCopy(audio1Bytes, 0, concatenatedArray, 0, audio1Bytes.Length);
            Buffer.BlockCopy(audio2Bytes, 0, concatenatedArray, audio1Bytes.Length, audio2Bytes.Length);
            return concatenatedArray;
        }

        private async Task ProcessData(byte[] data)
        {
            var dataResponse = await SpeechToText.SpToText(data);
            textBox1.Invoke(new Action(() => textBox1.Text = string.Concat(textBox1.Text, dataResponse)));
        }


        private void SingleBlockNotificationStreamOnSingleBlockRead(object sender, SingleBlockReadEventArgs e)
        {
            _graphVisualization.AddSamples(e.Left, e.Right);
        }

        private static WaveFormat WaveFormatFromBlob(Blob blob)
        {
            if (blob.Length == 40)
                return (WaveFormat)Marshal.PtrToStructure(blob.Data, typeof(WaveFormatExtensible));
            return (WaveFormat)Marshal.PtrToStructure(blob.Data, typeof(WaveFormat));
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            
                StartCapture();
                btnStart.Enabled = false;
                btnStop.Enabled = true;
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            StopCapture();
        }

        private async void StopCapture()
        {
            if (_soundIn != null)
            {
                _soundIn.Stop();
                _soundIn.Dispose();
                _soundIn = null;
                _finalSource.Dispose();

                if (_writer is IDisposable)
                    ((IDisposable)_writer).Dispose();

                btnStop.Enabled = false;
                btnStart.Enabled = true;
            }
            await Task.Delay(3000);
            var fileBytesLocation = @"C:\Users\kmilchev\Documents\1.wav";
            var fileBytes = await File.ReadAllBytesAsync(fileBytesLocation);
            await SpeechToText.SpToText(fileBytes);
        }

        private void deviceList_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (deviceList.SelectedItems.Count > 0)
            {
                SelectedDevice = (MMDevice)deviceList.SelectedItems[0].Tag;
            }
            else
            {
                SelectedDevice = null;
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            var image = pictureBox1.Image;
            pictureBox1.Image = _graphVisualization.Draw(pictureBox1.Width, pictureBox1.Height);
            if (image != null)
                image.Dispose();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            StopCapture();
        }

        private void MainWindow_Load(object sender, EventArgs e)
        {
            RefreshDevices();
        }

        private void timer2_Tick(object sender, EventArgs e)
        {
            var second = 1;
            System.Diagnostics.Debug.WriteLine("1");
            second += 1;
        }
    }

    public enum CaptureMode
    {
        Capture,
        LoopbackCapture
    }
}