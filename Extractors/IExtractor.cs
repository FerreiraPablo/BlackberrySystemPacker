using BlackberrySystemPacker.Nodes;

namespace BlackberrySystemPacker.Extractors
{
    public interface IExtractor
    {
        List<FileSystemNode> GetNodes(BinaryReader reader, long startPosition);
    }
}