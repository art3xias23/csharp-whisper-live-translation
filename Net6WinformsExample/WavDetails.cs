using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WinformsTranslation
{
    internal class WavDetails
    {
        public static void PrintWavDetials(byte[] wavBytes, string inputFile)
        {
            if (wavBytes == null || wavBytes.Length == 0)
            {
                wavBytes = File.ReadAllBytes(inputFile);
            }


            WavHeader wavHeader = new WavHeader();
            wavHeader.Parse(wavBytes);

            System.Diagnostics.Debug.WriteLine($"Chunk ID: {wavHeader.ChunkID}");
            System.Diagnostics.Debug.WriteLine($"Chunk Size: {wavHeader.ChunkSize} bytes");
            System.Diagnostics.Debug.WriteLine($"Format: {wavHeader.Format}");
            System.Diagnostics.Debug.WriteLine($"Subchunk1 ID: {wavHeader.Subchunk1ID}");
            System.Diagnostics.Debug.WriteLine($"Subchunk1 Size: {wavHeader.Subchunk1Size} bytes");
            System.Diagnostics.Debug.WriteLine($"Audio Format: {wavHeader.AudioFormat}");
            System.Diagnostics.Debug.WriteLine($"Number of Channels: {wavHeader.NumChannels}");
            System.Diagnostics.Debug.WriteLine($"Sample Rate: {wavHeader.SampleRate} Hz");
            System.Diagnostics.Debug.WriteLine($"Byte Rate: {wavHeader.ByteRate} bytes per second");
            System.Diagnostics.Debug.WriteLine($"Block Align: {wavHeader.BlockAlign} bytes");
            System.Diagnostics.Debug.WriteLine($"Bits Per Sample: {wavHeader.BitsPerSample} bits");
            System.Diagnostics.Debug.WriteLine($"Subchunk2Id: {wavHeader.SubChunk2Id} bytes");
            System.Diagnostics.Debug.WriteLine($"Subchunk2Size: {wavHeader.SubChunk2Size} bytes");
            System.Diagnostics.Debug.WriteLine("====================");
            System.Diagnostics.Debug.WriteLine("====================");
        }

        class WavHeader
        {
            //Riff descriptor
            public string ChunkID { get; set; }
            public int ChunkSize { get; set; }
            public string Format { get; set; }

            public string Subchunk1ID { get; set; }
            public int Subchunk1Size { get; set; }
            public short AudioFormat { get; set; }
            public short NumChannels { get; set; }
            public int SampleRate { get; set; }
            public int ByteRate { get; set; }
            public short BlockAlign { get; set; }
            public short BitsPerSample { get; set; }

            public string SubChunk2Id { get; set; }
            public int SubChunk2Size { get; set; }
            public string data { get; set; }


            public void Parse(byte[] headerBytes)
            {
                ChunkID = Encoding.ASCII.GetString(headerBytes, 0, 4);
                ChunkSize = BitConverter.ToInt32(headerBytes, 4) - 8;
                Format = Encoding.ASCII.GetString(headerBytes, 8, 4);

                Subchunk1ID = Encoding.ASCII.GetString(headerBytes, 12, 4);
                Subchunk1Size = BitConverter.ToInt32(headerBytes, 16);
                AudioFormat = BitConverter.ToInt16(headerBytes, 20);
                NumChannels = BitConverter.ToInt16(headerBytes, 22);
                SampleRate = BitConverter.ToInt32(headerBytes, 24);
                ByteRate = BitConverter.ToInt32(headerBytes, 28);
                BlockAlign = BitConverter.ToInt16(headerBytes, 32);
                BitsPerSample = BitConverter.ToInt16(headerBytes, 34);

                SubChunk2Id = System.Text.Encoding.ASCII.GetString(headerBytes, 36, 4);
                SubChunk2Size = BitConverter.ToInt32(headerBytes, 40);
            }
        }

    }
}
