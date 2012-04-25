using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace DCPU16.V11
{
    public class DASM16Assembly
    {
        public readonly ushort Offset;
        public int InstructionCount { get; private set; }
        public ushort Length { get; private set; }
        public DASM16Instruction[] Instructions { get; private set; }
        public ushort[] Words { get; private set; }

        internal DASM16Assembly( DASM16Instruction[] instructions, ushort offset )
        {
            InstructionCount = instructions.Length;
            Instructions = (DASM16Instruction[]) instructions.Clone();

            Length = 0;
            foreach ( DASM16Instruction ins in Instructions )
                Length += ins.Length;

            Words = new ushort[ Length ];
            ushort i = 0;
            foreach ( DASM16Instruction ins in Instructions )
                foreach ( ushort word in ins.Words )
                    Words[ i++ ] = word;
        }
    }
}
