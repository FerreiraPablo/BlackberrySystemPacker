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

                var start = Sparsing.Areas.First().Item1;
                foreach (var area in Sparsing.Areas)
                {
                    long offset = (area.Item1 - start) * Sparsing.BlockSize;
                    var size = area.Item2 * Sparsing.BlockSize;

                    stream.Seek(Sparsing.Offset + offset, SeekOrigin.Begin);
                    var buffer = new byte[size];
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
