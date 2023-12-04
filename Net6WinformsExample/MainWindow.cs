using System;
using System.ComponentModel;
using System.Diagnostics.Metrics;
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
        private bool _manualStop = false;

        private string _inputFileName = "input.wav";
        private string _outputFileName = "output.wav";
        private int conter = 0;


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
            if (HandleSoundIn()) return;
            _soundIn = new WasapiCapture();
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
                {
                    if (_manualStop)
                    {
                        _memStream = new MemoryStream();
                        _writer = new WaveWriter(_memStream, _finalSource.WaveFormat);
                        _manualStop = false;
                    }
                    _writer.Write(buffer, 0, read);
                }
            };

            singleBlockNotificationStream.SingleBlockRead += SingleBlockNotificationStreamOnSingleBlockRead;

            _soundIn.Start();

            _timer = new Timer();
            _timer.Interval = IntervalTimeInSeconds * 1000; // Convert to milliseconds
            _timer.Elapsed += ProcessAudio;
            _timer.Start();

        }
        private async Task StartNotificationCapture()
        {
            if (HandleSoundIn()) return;
            //1. init capture
            _soundIn = new WasapiCapture();
            _soundIn.Device = SelectedDevice;
            _soundIn.Initialize();

//2. wrap capture to an audio stream
            var soundInSource = new SoundInSource(_soundIn);

//3. wrap audio stream within an NotificationSource in order to intercept samples
            var notificationSource = new NotificationSource(soundInSource.ToSampleSource());
            notificationSource.Interval = 5000; //5 seconds
            notificationSource.BlockRead += async(s, e) =>
            {
                var fileName = "input.wav";

                using(var writer = new WaveWriter(fileName, notificationSource.WaveFormat))
                    writer.WriteSamples(e.Data, 0, e.Length);


                var result = await SpeechToTextPackage.TranslateAsync(fileName);
                textBox1.Invoke(new Action(() => textBox1.Text = string.Concat(textBox1.Text, result)));
            };

//4. convert whole chain back to WaveSource to write wave to the file
            var waveSource = notificationSource.ToWaveSource();

//5. initialize the wave writer
            var writer = new WaveWriter("out.wav", waveSource.WaveFormat);

//buffer for reading from the wavesource
            byte[] buffer = new byte[waveSource.WaveFormat.BytesPerSecond / 2];

//6. if capture serves new data to the audio stream chain, read from the last chain element (wavesource) and write it back to the file
          //  this will process audio data from capture to notificationSource to wavesource and will also trigger the blockread event of the notificationSource
            soundInSource.DataAvailable += (s, e) =>
            {
                int read;
                while((read = waveSource.Read(buffer, 0, buffer.Length)) > 0)
                    writer.Write(buffer, 0, read);
            };

            var singleBlockNotificationStream = new SingleBlockNotificationStream(soundInSource.ToSampleSource());

            singleBlockNotificationStream.SingleBlockRead += SingleBlockNotificationStreamOnSingleBlockRead;

            _soundIn.Start();

        }

        private bool HandleSoundIn()
        {
            if (SelectedDevice == null)
                return true;
            return false;
        }

        private async void ProcessAudio(object? sender, ElapsedEventArgs e)
        {
            //Every 5 seconds
            _manualStop = true;
            await ProcessData();
            _timer.Stop();
            _timer.Start();
        }

        private async Task ProcessData()
        {
            var copyOfMemStream = new MemoryStream(_memStream.ToArray());
            _inputFileName = conter + _inputFileName;
            File.WriteAllBytes(_inputFileName, copyOfMemStream.ToArray());
            WavDetails.PrintWavDetials(null, _inputFileName);
            //ExtractAudio();
            //var outputData = ReadExtractedAudio();
            //var dataResponse = await SpeechToTextApi.SpToTextAsync(outputData);
            //textBox1.Invoke(new Action(() => textBox1.Text = string.Concat(textBox1.Text, dataResponse)));
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

        private async void btnStart_Click(object sender, EventArgs e)
        {
            CreateInputFileName();
            //StartCapture();
            await StartNotificationCapture();
            //btnStart.Enabled = false;
            //btnStop.Enabled = true;
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