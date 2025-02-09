namespace BlackberrySystemPacker.Helpers
{
    public static class StreamExtensions
    {
        public static void TruncateStream(Stream stream, long startPosition, long length)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            if (!stream.CanSeek)
            {
                throw new ArgumentException("Stream must support seeking.", nameof(stream));
            }

            if (startPosition < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(startPosition), "Start position must be non-negative.");
            }

            if (length < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(length), "Length must be non-negative.");
            }

            if (startPosition + length > stream.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(length), "Length exceeds the stream boundary.");
            }


            if (length == 0) return; //nothing to remove


            stream.Seek(startPosition, SeekOrigin.Begin);

            long endPosition = startPosition + length - 1; // Calculate end position

            // Handle removal from the beginning
            if (startPosition == 0)
            {
                if (length == stream.Length)
                {
                    stream.SetLength(0);
                    return;
                }

                byte[] buffer = new byte[4096];
                long bytesRead;
                long currentPosition = endPosition + 1;

                stream.Position = currentPosition;

                while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    stream.Position = currentPosition - length;
                    stream.Write(buffer, 0, (int)bytesRead);
                    currentPosition += bytesRead;
                    stream.Position = currentPosition;
                }
                stream.SetLength(stream.Length - length);
                return;
            }

            // General case (middle or end removal)
            byte[] buffer2 = new byte[4096];
            long bytesRead2;
            long currentPosition2 = endPosition + 1;

            stream.Position = currentPosition2;
            while ((bytesRead2 = stream.Read(buffer2, 0, buffer2.Length)) > 0)
            {
                stream.Position = currentPosition2 - length;
                stream.Write(buffer2, 0, (int)bytesRead2);
                currentPosition2 += bytesRead2;
                stream.Position = currentPosition2;
            }
            stream.SetLength(stream.Length - length);

        }

        public static void ZeroOutStream(Stream stream, long startPosition, long length)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            if (!stream.CanSeek)
            {
                throw new ArgumentException("Stream must support seeking.", nameof(stream));
            }

            if (startPosition < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(startPosition), "Start position must be non-negative.");
            }

            if (length < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(length), "Length must be non-negative.");
            }

            if (startPosition + length > stream.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(length), "Length exceeds the stream boundary.");
            }

            if (length == 0) return; //nothing to zero out

            byte[] buffer = new byte[4096]; // Use a buffer for efficiency
            long bytesToWrite;
            long currentPosition = startPosition;

            stream.Position = currentPosition;

            while (length > 0)
            {
                bytesToWrite = Math.Min(length, buffer.Length); // How many bytes to write in this iteration

                // Zero out the buffer (important!)
                Array.Clear(buffer, 0, (int)bytesToWrite); // More efficient than looping

                stream.Write(buffer, 0, (int)bytesToWrite);

                length -= bytesToWrite;
            }

        }
    }
}