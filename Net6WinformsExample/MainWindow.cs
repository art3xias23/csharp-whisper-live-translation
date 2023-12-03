using System;
using System.ComponentModel;
using Timer = System.Timers.Timer;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Timers;
using System.Windows.Forms;
using Art3xias.SoxWrapper;
using CSCore;
using CSCore.Codecs.WAV;
using CSCore.CoreAudioAPI;
using CSCore.SoundIn;
using CSCore.Streams;
using CSCore.Win32;
using WinformsTranslation;

namespace Recorder
{
    public partial class MainWindow : Form
    {
        private int IntervalTimeInSeconds = 5;
        private int OverlapTimeInSeconds = 2;

        //Change this to CaptureMode.Capture to capture a microphone,...
        private const CaptureMode CaptureMode = Recorder.CaptureMode.Capture;

        private MMDevice _selectedDevice;
        private WasapiCapture _soundIn;
        private IWriteable _writer;
        private MemoryStream _memStream;
        private readonly GraphVisualization _graphVisualization = new GraphVisualization();
        private IWaveSource _finalSource;

        private string _inputFileName = "input.wav";
        private string _outputFileName = "output.wav";


        private Timer _timer;

        public MMDevice SelectedDevice
        {
            get { return _selectedDevice; }
            set
            {
                _selectedDevice = value;
                //if (value != null)
                //btnStart.Enabled = true;
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

            if (_soundIn.Device == null)
                _soundIn.Device = SelectedDevice;
            _soundIn.Initialize();
            _memStream = new MemoryStream();

            var soundInSource = new SoundInSource(_soundIn);
            var singleBlockNotificationStream = new SingleBlockNotificationStream(soundInSource.ToSampleSource());
            _finalSource = singleBlockNotificationStream.ToWaveSource();
            _writer = new WaveWriter(_memStream, _finalSource.WaveFormat);

            byte[] buffer = new byte[_finalSource.WaveFormat.BytesPerSecond / 2];
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
        private async void ProcessAudio(object? sender, ElapsedEventArgs e)
        {
            await ProcessData();
        }

        private async Task ProcessData()
        {
            StopCapture();
            var copyOfMemStream = new MemoryStream(_memStream.ToArray());
            StartCapture();
            File.WriteAllBytes(_inputFileName, copyOfMemStream.ToArray());
            WavDetails.PrintWavDetials(null, _inputFileName);
            ExtractAudio();
            var outputData = ReadExtractedAudio();
            var dataResponse = await SpeechToTextApi.SpToTextAsync(outputData);
            textBox1.Invoke(new Action(() => textBox1.Text = string.Concat(textBox1.Text, dataResponse)));
        }

        private byte[] ReadExtractedAudio()
        {
            return File.ReadAllBytes(_outputFileName);
        }

        private void ExtractAudio()
        {
            new SoxWrapperClient()
                .WithOptions()
                .WithExeLocation(@"C:\Program Files (x86)\sox-14-4-2\sox.exe")
                //.WithExeLocation(@"cmd.exe")
                .WithExtractCommand(3, _inputFileName, _outputFileName)
                .Build()
                .Execute();
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
            CreateInputFileName();
            StartCapture();
            btnStart.Enabled = false;
            btnStop.Enabled = true;
        }

        private void CreateInputFileName()
        {
            //File.Create(_inputFileName).Close();
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

                //btnStop.Enabled = false;
                //btnStart.Enabled = true;
            }
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