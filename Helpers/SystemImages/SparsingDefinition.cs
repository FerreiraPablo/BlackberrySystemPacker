using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlackberrySystemPacker.Helpers.SystemImages
{
    public class SparsingDefinition
    {
        public List<Tuple<int, int>> Areas { get; set; } = [];
        
        public int Size { get => BlockCount * BlockSize; }
        
        public int BlockCount { get => Areas.Sum(x => x.Item2); }
        
        public int BlockSize { get; set; } = 65536;
        
        public int Offset { get; set; }

        public int End { get => Offset + Size; }
    }
}
