using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DCPU16.V11
{
    public class DASM16Instruction
    {
        public readonly DCPU16Opcode Opcode;

        public readonly DASM16Value[] Values;

        public ushort Length { get; private set; }
        public ushort[] Words { get; private set; }
        public String Disassembled { get; private set; }

        public DASM16Instruction( DCPU16Opcode opcode )
        {
            Opcode = opcode;
        }

        public DASM16Instruction( DCPU16Opcode opcode, DASM16Value value )
            : this( opcode )
        {
            Values = new DASM16Value[] { value };

            Length = (ushort) ( 1 + ( Values[ 0 ].Extended ? 1 : 0 ) );
        }

        public DASM16Instruction( DCPU16Opcode opcode, DASM16Value valueA, DASM16Value valueB )
            : this( opcode )
        {
            Values = new DASM16Value[] { valueA, valueB };

            Length = (ushort) ( 1 + ( Values[ 0 ].Extended ? 1 : 0 ) + ( Values[ 1 ].Extended ? 1 : 0 ) );
        }

        public DASM16Instruction( DASM16LiteralVal[] values )
            : this( DCPU16Opcode.Dat )
        {
            Values = values;
            Length = (ushort) ( values.Length );
        }

        public void ResolveConstants( Dictionary<String, ushort> consts )
        {
            Words = new ushort[ Length ];

            if ( Opcode != DCPU16Opcode.Dat )
            {
                if ( Values.Length > 0 )
                {
                    Values[ 0 ].ResolveConstant( consts );

                    if ( Values.Length > 1 )
                    {
                        Values[ 1 ].ResolveConstant( consts );

                        Words[ 0 ] = (ushort) ( (ushort) Opcode | ( Values[ 0 ].Assembled << 0x4 ) | ( Values[ 1 ].Assembled << 0xa ) );

                        if ( Values[ 0 ].Extended )
                        {
                            Words[ 1 ] = Values[ 0 ].NextWord;
                            if ( Values[ 1 ].Extended )
                                Words[ 2 ] = Values[ 1 ].NextWord;
                        }
                        else if ( Values[ 1 ].Extended )
                            Words[ 1 ] = Values[ 1 ].NextWord;

                        Disassembled = Opcode.ToString().ToUpper() + " " + Values[ 0 ].ToString() + ", " + Values[ 1 ].ToString();
                    }
                    else
                    {
                        Words[ 0 ] = (ushort) ( (ushort) Opcode | ( Values[ 0 ].Assembled << 0xa ) );

                        if ( Values[ 0 ].Extended )
                            Words[ 1 ] = Values[ 0 ].NextWord;

                        Disassembled = Opcode.ToString().ToUpper() + " " + Values[ 0 ].ToString();
                    }
                }
            }
            else
            {
                Disassembled = Opcode.ToString().ToUpper() + " ";

                for ( int i = 0; i < Values.Length; ++i )
                {
                    Values[ i ].ResolveConstant( consts );
                    Words[ i ] = Values[ i ].NextWord;
                    Disassembled += Values[ i ].Disassembled;
                    if ( i < Values.Length - 1 )
                        Disassembled += ", ";
                }
            }
        }
    }
}
