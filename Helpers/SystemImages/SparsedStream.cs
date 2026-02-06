namespace BlackberrySystemPacker.Helpers.SystemImages
{
    public class SparsedStream : Stream
    {
        private readonly Stream _stream;
        private readonly PartitionDefinition _definition;
        private long _position;
        private readonly List<VirtualChunk> _chunks;
        private readonly long _length;

        private struct VirtualChunk
        {
            public long VirtualStart;
            public long VirtualEnd; // Exclusive
            public long PhysicalStart;
            public long Length;
        }

        public SparsedStream(PartitionDefinition definition, Stream baseStream)
        {
            _stream = baseStream ?? throw new ArgumentNullException(nameof(baseStream));
            _definition = definition ?? throw new ArgumentNullException(nameof(definition));

            if (_definition.Sparsing == null)
            {
                throw new ArgumentException("PartitionDefinition must have a Sparsing definition.", nameof(definition));
            }

            _chunks = new List<VirtualChunk>();
            long currentVirtualPos = 0;
            
            // Calculate chunks based on Sparsing.Areas
            // Logic derived from PartitionDefinition.ExtractInto:
            // start = Sparsing.Areas.First().Item1
            // gapBlocks = area.Item1 - start
            // physicalOffset = (long)gapBlocks * BlockSize
            // size = (long)area.Item2 * BlockSize
            // PhysicalStart = Sparsing.Offset + physicalOffset

            if (_definition.Sparsing.Areas != null && _definition.Sparsing.Areas.Any())
            {
                var startBlockIndex = _definition.Sparsing.Areas.First().StartBlock;
                long blockSize = _definition.Sparsing.BlockSize;
                long basePhysicalOffset = _definition.Sparsing.Offset;

                foreach (var area in _definition.Sparsing.Areas)
                {
                    int areaBlockIndex = area.StartBlock;
                    int areaBlockCount = area.BlockCount;

                    long gapBlocks = areaBlockIndex - startBlockIndex;
                    long chunkPhysicalOffset = gapBlocks * blockSize;
                    long chunkLength = areaBlockCount * blockSize;

                    var chunk = new VirtualChunk
                    {
                        VirtualStart = currentVirtualPos,
                        VirtualEnd = currentVirtualPos + chunkLength,
                        Length = chunkLength,
                        PhysicalStart = basePhysicalOffset + chunkPhysicalOffset
                    };

                    _chunks.Add(chunk);
                    currentVirtualPos += chunkLength;
                }
            }

            _length = currentVirtualPos;
            _position = 0;
        }

        public override bool CanRead => _stream.CanRead;

        public override bool CanSeek => _stream.CanSeek;

        public override bool CanWrite => _stream.CanWrite;

        public override long Length => _length;

        public override long Position
        {
            get => _position;
            set => Seek(value, SeekOrigin.Begin);
        }

        public override void Flush()
        {
            _stream.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (_position >= _length) return 0;

            int totalRead = 0;
            // Cap count to remaining length
            long remaining = _length - _position;
            if (count > remaining) count = (int)remaining;

            while (count > 0)
            {
                var chunk = GetChunkForPosition(_position);
                if (chunk == null) break; // Should not happen given bounds check

                long relativeOffset = _position - chunk.Value.VirtualStart;
                long hiddenChunkRemaining = chunk.Value.Length - relativeOffset;
                int toRead = (int)Math.Min(count, hiddenChunkRemaining);

                _stream.Seek(chunk.Value.PhysicalStart + relativeOffset, SeekOrigin.Begin);
                int read = _stream.Read(buffer, offset, toRead);

                if (read == 0) break; // End of physical stream or unexpected error

                _position += read;
                offset += read;
                count -= read;
                totalRead += read;
            }

            return totalRead;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            long newPos = _position;
            switch (origin)
            {
                case SeekOrigin.Begin:
                    newPos = offset;
                    break;
                case SeekOrigin.Current:
                    newPos = _position + offset;
                    break;
                case SeekOrigin.End:
                    newPos = _length + offset;
                    break;
            }

            if (newPos < 0) throw new IOException("Seek before beginning of stream.");
            // We allow seeking past end of stream, standard Stream behavior, but Read/Write will limit.
            _position = newPos;
            return _position;
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException("Resizing a SparsedStream is not supported as the sparse definition is fixed.");
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (_position >= _length) throw new IOException("Cannot write past the end of the predefined sparse stream.");
            
            // Similar to Read, split across chunks
             while (count > 0)
            {
                var chunk = GetChunkForPosition(_position);
                if (chunk == null)
                {
                     // If we are out of bounds of defined chunks but within Length (which shouldn't happen if Length is sum of chunks),
                     // or if we are at EOF.
                     if (_position >= _length) throw new IOException("Cannot write past the end of the predefined sparse stream.");
                     break; 
                }

                long relativeOffset = _position - chunk.Value.VirtualStart;
                long hiddenChunkRemaining = chunk.Value.Length - relativeOffset;
                int toWrite = (int)Math.Min(count, hiddenChunkRemaining);

                _stream.Seek(chunk.Value.PhysicalStart + relativeOffset, SeekOrigin.Begin);
                _stream.Write(buffer, offset, toWrite);

                _position += toWrite;
                offset += toWrite;
                count -= toWrite;
            }
        }

        private VirtualChunk? GetChunkForPosition(long virtualPos)
        {
            // Binary search could be efficient, but linear is fine for typically small number of chunks
            // optimization: Check last used chunk first? For now simple.
            foreach (var chunk in _chunks)
            {
                if (virtualPos >= chunk.VirtualStart && virtualPos < chunk.VirtualEnd)
                {
                    return chunk;
                }
            }
            return null;
        }
    }
}
