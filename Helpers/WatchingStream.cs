namespace BlackberrySystemPacker.Helpers
{
    public class WatchingStream : Stream
    {

        private Stream _originalStream;

        public WatchingStream(Stream originalStream)
        {
            _originalStream = originalStream;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var read = _originalStream.Read(buffer, offset, count);
            if (read > 0)
            {
                var data = new byte[read];
                Array.Copy(buffer, offset, data, 0, read);
                //Console.WriteLine($"Read {read} bytes: {BitConverter.ToString(data)}");
            }
            return read;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            var data = new byte[count];
            Array.Copy(buffer, offset, data, 0, count);
            //Console.WriteLine($"Write {count} bytes: {BitConverter.ToString(data)}");
            _originalStream.Write(buffer, offset, count);
        }

        public override void Flush()
        {
            _originalStream.Flush();
        }

        public override bool CanRead => _originalStream.CanRead;

        public override bool CanSeek => _originalStream.CanSeek;

        public override bool CanWrite => _originalStream.CanWrite;

        public override long Length => _originalStream.Length;

        public override long Position { get => _originalStream.Position; set => _originalStream.Position = value; }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return _originalStream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            _originalStream.SetLength(value);
        }

        public override void Close()
        {
            _originalStream.Close();
        }

    }
}