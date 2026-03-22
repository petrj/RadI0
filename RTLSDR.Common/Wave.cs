using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RTLSDR.Common
{
    /// <summary>
    /// A class for creating and writing WAV audio files.
    /// </summary>
    public class Wave
    {
        private AudioDataDescription _dataDesc = new AudioDataDescription();

        private FileStream? _fileStream;
        private BinaryWriter? _writer;

        private long? _dataChunkSizePosition;
        private uint? _dataChunkSize;

        /// <summary>
        /// Initializes a new instance of the <see cref="Wave"/> class.
        /// </summary>
        public Wave()
        {
        }

        /// <summary>
        /// Creates a new WAV file with the specified audio description.
        /// </summary>
        /// <param name="filePath">The path to the WAV file to create.</param>
        /// <param name="audioDescription">The audio data description.</param>
        public void CreateWaveFile(string filePath, AudioDataDescription audioDescription)
        {
            _fileStream = new FileStream(filePath, FileMode.Create);
            _writer = new BinaryWriter(_fileStream);

            _dataDesc = audioDescription;

            _dataChunkSize = 0;

            WriteWaveHeader();
        }

        /// <summary>
        /// Writes the RIFF WAV header to the file.
        /// </summary>
        private void WriteWaveHeader()
        {
            if (_writer == null)
            {
                throw new InvalidOperationException("Wave file not initialized. Call CreateWaveFile first.");
            }

            // RIFF header
            _writer.Write(new char[4] { 'R', 'I', 'F', 'F' });
            _writer.Write((uint)0); // Placeholder for file size
            _writer.Write(new char[4] { 'W', 'A', 'V', 'E' });

            // fmt subchunk
            _writer.Write(new char[4] { 'f', 'm', 't', ' ' });
            _writer.Write((uint)16); // Subchunk1Size for PCM
            _writer.Write((short)1); // AudioFormat (1 for PCM)
            _writer.Write(_dataDesc.Channels); // NumChannels
            _writer.Write(_dataDesc.SampleRate); // SampleRate
            _writer.Write(_dataDesc.SampleRate * _dataDesc.Channels * _dataDesc.BitsPerSample / 8); // ByteRate
            _writer.Write((short)(_dataDesc.Channels * _dataDesc.BitsPerSample / 8)); // BlockAlign
            _writer.Write(_dataDesc.BitsPerSample); // BitsPerSample

            // data subchunk
            _writer.Write(new char[4] { 'd', 'a', 't', 'a' });
            _dataChunkSizePosition = _fileStream?.Position;
            _writer.Write((uint)0); // Placeholder for data chunk size
        }

        /// <summary>
        /// Writes sample data to the WAV file.
        /// </summary>
        /// <param name="data">The audio sample data.</param>
        public void WriteSampleData(byte[] data)
        {
            _writer?.Write(data);
            _dataChunkSize += (uint)data.Length;
        }

        /// <summary>
        /// Closes the WAV file and updates the header with final sizes.
        /// </summary>
        public void CloseWaveFile()
        {
            if (_writer == null ||
                _fileStream == null ||
                 _dataChunkSizePosition == null || 
                 _dataChunkSize == null)
            {
                throw new InvalidOperationException("Wave file not initialized.");
            }

            _writer.Flush();
            _fileStream.Flush();

            // Update file size
            _writer.Seek(4, SeekOrigin.Begin);
            _writer.Write((uint)(_fileStream.Length - 8));

            // Update data chunk size
            _writer.Seek((int)_dataChunkSizePosition, SeekOrigin.Begin);
            _writer.Write((uint)_dataChunkSize);

            _writer.Close();
            _fileStream.Close();

            _writer.Dispose();
            _fileStream.Dispose();
        }

    }
}
