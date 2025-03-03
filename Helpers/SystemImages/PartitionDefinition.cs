namespace BlackberrySystemPacker.Helpers.SystemImages
{
    public class PartitionDefinition
    {
        public int Type { get; set; }

        public long Offset { get; set; }

        public long Size { get; set; }

        public long End => Offset + Size;

        public long DefinitionPosition { get; set; }
    }
}
