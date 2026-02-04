namespace BlackberrySystemPacker.Helpers.SystemImages
{
    public class PartitionDefinition
    {
        public int Type { get; set; }

        public long Offset { get; set; }

        public long Size { get; set; }

        public long End => Offset + Size;

        public long DefinitionPosition { get; set; }

        public SparsingDefinition Sparsing { get; set; } = null;

        public Stream Extract(Stream stream)
        {
            var targetStream = new MemoryStream();
            ExtractInto(stream, targetStream);
            return targetStream;
        }

        public void ExtractInto(Stream stream, Stream targetStream)
        {
            if(Sparsing != null) { 
                var initialPosition = stream.Position;
                targetStream.Seek(0, SeekOrigin.Begin);

                foreach (var area in Sparsing.Areas) { 
                
                    var gapBlocks = (long)area.StartBlock;
                    long offset = ((long)gapBlocks * Sparsing.BlockSize);
                    var size = (long)area.BlockCount * Sparsing.BlockSize;
                    var buffer = new byte[size];
                    stream.Seek(offset, SeekOrigin.Begin);
                    stream.Read(buffer, 0, buffer.Length);
                    targetStream.Write(buffer, 0, buffer.Length);
                }
                targetStream.Seek(0, SeekOrigin.Begin);
                stream.Seek(initialPosition, SeekOrigin.Begin);
            } else
            {
                stream.Seek(Offset, SeekOrigin.Begin);
                var buffer = new byte[Size];
                stream.Read(buffer, 0, buffer.Length);
                targetStream.Write(buffer, 0, buffer.Length);
                targetStream.Seek(0, SeekOrigin.Begin);
            }
        }
    }
}
