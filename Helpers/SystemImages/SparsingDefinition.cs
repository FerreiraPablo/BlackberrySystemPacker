using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlackberrySystemPacker.Helpers.SystemImages
{
    public class SparsingArea
    {

        public int StartBlock { get; set; }

        public int BlockCount { get; set; }

        public SparsingArea(int startBlock, int blockCount)
        {
            StartBlock = startBlock;
            BlockCount = blockCount;
        }
    }

    public class SparsingDefinition
    {
        public List<SparsingArea> Areas { get; set; } = [];
        
        public int Size { get => BlockCount * BlockSize; }
        
        public int BlockCount { get => Areas.Sum(x => x.BlockCount); }
        
        public int BlockSize { get; set; } = 65536;
        
        public int Offset { get; set; }

        public int StartBlock { get; set; }

        public int End { get => Offset + Size; }
    }
}
