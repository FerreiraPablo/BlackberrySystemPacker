namespace BlackberrySystemPacker.Helpers
{
    public class SectorOffset
    {
        public long Start;

        public long End;

        public long Offset;

        public long Size => End - Start;
        public long OffsetEnd => Offset + End;
        public long OffsetStart => Offset + Start;
    }
}