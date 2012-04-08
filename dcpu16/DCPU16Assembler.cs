using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DCPU16
{
    public class InvalidOpcodeException : Exception
    {
        public readonly String Opcode;

        public InvalidOpcodeException( String opcode )
            : base( "An invalid operator code was encountered: \"" + opcode + "\"" )
        {
            Opcode = opcode;
        }
    }

    public class InvalidValueException : Exception
    {
        public readonly String Value;

        public InvalidValueException( String value )
            : base( "An invalid value was encountered: \"" + value + "\"" )
        {
            Value = value;
        }
    }

    public class InvalidLabelException : Exception
    {
        public readonly String Label;

        public InvalidLabelException( String label )
            : base( "An invalid label was encountered: \"" + label + "\"" )
        {
            Label = label;
        }
    }

    public static class DCPU16Assembler
    {
        private abstract class Value
        {
            public bool Reference { get; private set; }
            public bool Extended { get; protected set; }
            public ushort Assembled { get; protected set; }
            public ushort NextWord { get; protected set; }
            public String Disassembled { get; private set; }

            public Value( bool reference )
            {
                Reference = reference;
            }

            protected void SetDisassembled( String str )
            {
                if ( Reference )
                    str = "[" + str + "]";

                Disassembled = str;
            }

            public override string ToString()
            {
                return Disassembled;
            }

            public abstract void ResolveLabel( Dictionary<String, ushort> labels );
        }

        private class RegisterVal : Value
        {
            public readonly Register Register;
            public readonly String Label;
            public ushort Offset;

            public RegisterVal( String str, bool reference )
                : base( reference )
            {
                if ( str.Contains( "+" ) )
                {
                    string[] split = str.Split( '+' );
                    split[ 0 ] = split[ 0 ].Trim();
                    split[ 1 ] = split[ 1 ].Trim();

                    if( Enum.TryParse( split[ 0 ], out Register ) )
                    {
                        if ( char.IsNumber( split[ 1 ][ 0 ] ) )
                            Offset = ParseNumber( split[ 1 ] );
                        else
                            Label = split[ 1 ];
                    }
                    else if( Enum.TryParse( split[ 1 ], out Register ) )
                    {
                        if ( char.IsNumber( split[ 0 ][ 0 ] ) )
                            Offset = ParseNumber( split[ 0 ] );
                        else
                            Label = split[ 0 ];
                    }
                    else
                    {
                        if( reference )
                            str = "[" + str + "]";
                        throw new InvalidValueException( str );
                    }
                    Extended = true;
                }
                else
                {
                    Register = (Register) Enum.Parse( typeof( Register ), str.ToUpper() );
                    Offset = 0x0000;
                    Extended = false;
                }
            }

            public override void ResolveLabel( Dictionary<string, ushort> labels )
            {
                if ( Label != null )
                {
                    if ( labels.ContainsKey( Label ) )
                        Offset = labels[ Label ];
                    else
                        throw new InvalidLabelException( Label );
                }

                Assembled = (ushort) ( (ushort) Register | ( Reference ? Extended ? 0x10 : 0x08 : 0x00 ) );
                NextWord = Offset;
                SetDisassembled( ( Extended ? "0x" + Offset.ToString( "X" ) + "+" : "" ) + Register.ToString() );
            }
        }

        private enum SpecType : byte
        {
            Pop, Peek, Push, SP, PC, O
        }

        private class SpecialVal : Value
        {
            public readonly SpecType Type;

            public SpecialVal( String str, bool reference )
                : base( reference )
            {
                Extended = false;
                switch ( str.ToUpper() )
                {
                    case "POP":
                        Type = SpecType.Pop;
                        Assembled = 0x18;
                        break;
                    case "PEEK":
                        Type = SpecType.Peek;
                        Assembled = 0x19;
                        break;
                    case "PUSH":
                        Type = SpecType.Push;
                        Assembled = 0x1a;
                        break;
                    case "SP":
                        Type = SpecType.SP;
                        Assembled = 0x1b;
                        break;
                    case "PC":
                        Type = SpecType.PC;
                        Assembled = 0x1c;
                        break;
                    case "O":
                        Type = SpecType.O;
                        Assembled = 0x1d;
                        break;
                }
                SetDisassembled( Type.ToString().ToUpper() );
            }

            public override void ResolveLabel( Dictionary<string, ushort> labels )
            {
                return;
            }
        }

        private class LiteralVal : Value
        {
            public readonly String Label;
            public ushort Literal;

            public LiteralVal( String str, bool reference )
                : base( reference )
            {
                if ( char.IsNumber( str[ 0 ] ) )
                {
                    Literal = ParseNumber( str );
                    Extended = Literal >= 0x20;
                }
                else
                {
                    Label = str;
                    Extended = true;
                }
            }

            public override void ResolveLabel( Dictionary<string, ushort> labels )
            {
                if ( Label != null )
                {
                    if ( labels.ContainsKey( Label ) )
                        Literal = labels[ Label ];
                    else
                        throw new InvalidLabelException( Label );

                    Extended = true;
                }

                if ( Extended )
                {
                    Assembled = (ushort) ( 0x1e | ( Reference ? 0x0 : 0x1 ) );
                    NextWord = Literal;
                }
                else
                {
                    Assembled = (ushort) ( Literal | ( Reference ? 0x28 : 0x20 ) );
                }

                SetDisassembled( "0x" + Literal.ToString( "X" ) );
            }
        }

        private class Instruction
        {
            public readonly Opcode Opcode;

            public readonly Value ValueA;
            public readonly Value ValueB;

            public int Length { get; private set; }
            public ushort[] Assembled { get; private set; }
            public String Disassembled { get; private set; }

            public Instruction( Opcode opcode )
            {
                Opcode = opcode;
            }

            public Instruction( Opcode opcode, Value value )
                : this( opcode )
            {
                ValueA = value;

                Length = 1 + ( ValueA.Extended ? 1 : 0 );
            }

            public Instruction( Opcode opcode, Value valueA, Value valueB )
                : this( opcode )
            {
                ValueA = valueA;
                ValueB = valueB;

                Length = 1 + ( ValueA.Extended ? 1 : 0 ) + ( ValueB.Extended ? 1 : 0 );
                Assembled = new ushort[ Length ];
                Assembled[ 0 ] = (ushort) ( (ushort) Opcode | ( ValueA.Assembled << 0x4 ) | ( ValueB.Assembled << 0xa ) );
                if ( ValueA.Extended )
                {
                    Assembled[ 1 ] = ValueA.NextWord;
                    if ( ValueB.Extended )
                        Assembled[ 2 ] = ValueB.NextWord;
                }
                else if ( ValueB.Extended )
                    Assembled[ 1 ] = ValueB.NextWord;
                Disassembled = Opcode.ToString().ToUpper() + " " + ValueA.ToString() + ", " + ValueB.ToString();
            }

            public void ResolveLabels( Dictionary<String, ushort> labels )
            {
                Assembled = new ushort[ Length ];

                if ( ValueA != null )
                {
                    ValueA.ResolveLabel( labels );

                    if ( ValueB != null )
                    {
                        ValueB.ResolveLabel( labels );

                        Assembled[ 0 ] = (ushort) ( (ushort) Opcode | ( ValueA.Assembled << 0x4 ) | ( ValueB.Assembled << 0xa ) );
                       
                        if ( ValueA.Extended )
                        {
                            Assembled[ 1 ] = ValueA.NextWord;
                            if ( ValueB.Extended )
                                Assembled[ 2 ] = ValueB.NextWord;
                        }
                        else if ( ValueB.Extended )
                            Assembled[ 1 ] = ValueB.NextWord;

                        Disassembled = Opcode.ToString().ToUpper() + " " + ValueA.ToString() + ", " + ValueB.ToString();
                    }
                    else
                    {
                        Assembled[ 0 ] = (ushort) ( (ushort) Opcode | ( ValueA.Assembled << 0xa ) );

                        if ( ValueA.Extended )
                            Assembled[ 1 ] = ValueA.NextWord;

                        Disassembled = Opcode.ToString().ToUpper() + " " + ValueA.ToString();
                    }
                }
            }
        }

        public static ushort[] Assemble( String str, ushort wordOffset = 0x0000 )
        {
            List<Instruction> instructions = new List<Instruction>();
            Dictionary<String, ushort> labels = new Dictionary<string, ushort>();
            Instruction next;
            int offset = 0, words = 0;
            while ( ( next = ReadInstruction( str, ref offset, (ushort) ( words + wordOffset ), labels ) ) != null )
            {
                instructions.Add( next );
                words += next.Length;
            }

            ushort[] buffer = new ushort[ words ];
            int i = 0;
            foreach ( Instruction ins in instructions )
            {
                ins.ResolveLabels( labels );
                foreach ( ushort word in ins.Assembled )
                    buffer[ i++ ] = word;
            }

            return buffer;
        }

        private static Instruction ReadInstruction( String str, ref int offset, ushort words, Dictionary<String, ushort> labels )
        {
            SkipWhitespace( str, ref offset );

            while ( offset < str.Length - 2 && str[ offset ] == ':' )
            {
                String label = "";
                while ( char.IsLetterOrDigit( str[ ++offset ] ) || str[ offset ] == '_' )
                    label += str[ offset ];

                labels.Add( label, words );
                SkipWhitespace( str, ref offset );
            }

            if ( offset >= str.Length - 3 )
                return null;

            Opcode opcode = ReadOperator( str, ref offset );
            SkipWhitespace( str, ref offset );
            Value valA = ReadValue( str, ref offset );
            if ( valA == null )
                return null;
            if ( (int) opcode < 0x10 )
            {
                SkipWhitespace( str, ref offset );
                Value valB = ReadValue( str, ref offset );
                if ( valB == null )
                    return null;
                return new Instruction( opcode, valA, valB );
            }
            return new Instruction( opcode, valA );
        }

        private static void SkipWhitespace( String str, ref int offset )
        {
            if( offset >= str.Length )
                return;

            bool comment = false;

            do
            {
                if ( str[ offset ] == ';' )
                    comment = true;
                else if ( comment && str[ offset ] == '\n' )
                    comment = false;
            }
            while ( ( comment || char.IsWhiteSpace( str[ offset ] ) ) && ( ++offset < str.Length ) );
        }

        private static Opcode ReadOperator( String str, ref int offset )
        {
            String opstr = str.Substring( offset, 3 ).ToUpper();
            offset += 3;
            SkipWhitespace( str, ref offset );

            switch ( opstr )
            {
                case "SET":
                    return Opcode.Set;
                case "ADD":
                    return Opcode.Add;
                case "SUB":
                    return Opcode.Sub;
                case "MUL":
                    return Opcode.Mul;
                case "DIV":
                    return Opcode.Div;
                case "MOD":
                    return Opcode.Mod;
                case "SHL":
                    return Opcode.ShL;
                case "SHR":
                    return Opcode.ShR;
                case "AND":
                    return Opcode.And;
                case "BOR":
                    return Opcode.BOr;
                case "XOR":
                    return Opcode.XOr;
                case "IFE":
                    return Opcode.IfE;
                case "IFN":
                    return Opcode.IfN;
                case "IFG":
                    return Opcode.IfG;
                case "IFB":
                    return Opcode.IfB;
                case "JSR":
                    return Opcode.Jsr;
                default:
                    throw new InvalidOpcodeException( opstr );
            }
        }

        private static Value ReadValue( String str, ref int offset )
        {
            if ( offset >= str.Length )
                return null;

            bool reference;
            String val = "";

            if ( str[ offset ] == '[' )
            {
                reference = true;
                while ( ++offset < str.Length && str[ offset ] != ']' )
                    val += str[ offset ];
                ++offset;
            }
            else
            {
                reference = false;
                while ( offset < str.Length && !char.IsWhiteSpace( str[ offset ] ) && str[ offset ] != ',' )
                    val += str[ offset++ ];
            }

            if ( str[ offset ] == ',' )
                ++offset;

            val = val.Trim();

            if ( val.Contains( "+" ) )
                return new RegisterVal( val, reference );

            switch ( val.ToUpper() )
            {
                case "A":
                case "B":
                case "C":
                case "X":
                case "Y":
                case "Z":
                case "I":
                case "J":
                    return new RegisterVal( val, reference );
                case "POP":
                case "PUSH":
                case "PEEK":
                case "SP":
                case "PC":
                case "O":
                    return new SpecialVal( val, reference );
                default:
                    return new LiteralVal( val, reference );
            }
        }

        private static ushort ParseNumber( String str )
        {
            if ( str.Length > 1 && char.IsLetter( str[ 1 ] ) )
            {
                switch ( str.ToLower()[ 1 ] )
                {
                    case 'b':
                        return Convert.ToUInt16( str.Substring( 2 ), 2 );
                    case 'd':
                        return Convert.ToUInt16( str.Substring( 2 ), 10 );
                    case 'x':
                        return Convert.ToUInt16( str.Substring( 2 ), 16 );
                    default:
                        return 0;
                }
            }

            return Convert.ToUInt16( str );
        }
    }
}
