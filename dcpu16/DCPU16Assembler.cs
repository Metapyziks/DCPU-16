using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DCPU16
{
    public static class DCPU16Assembler
    {
        public class AssemblerException : Exception
        {
            public readonly ushort Line;

            public AssemblerException( ushort line, String message )
                : base( message + "\n  at Line: " + ( line + 1 ) )
            {
                Line = line;
            }
        }

        public class InvalidOpcodeException : AssemblerException
        {
            public readonly String Opcode;

            public InvalidOpcodeException( ushort line, String opcode )
                : base( line, "An invalid operator code was encountered: \"" + opcode + "\"" )
            {
                Opcode = opcode;
            }
        }

        public class InvalidValueException : AssemblerException
        {
            public readonly String Value;

            public InvalidValueException( ushort line, String value )
                : base( line, "An invalid value was encountered: \"" + value + "\"" )
            {
                Value = value;
            }
        }

        public class InvalidLabelException : AssemblerException
        {
            public readonly String Label;

            public InvalidLabelException( ushort line, String label )
                : base( line, "An invalid label was encountered: \"" + label + "\"" )
            {
                Label = label;
            }
        }

        public class InvalidLiteralException : AssemblerException
        {
            public readonly String Literal;

            public InvalidLiteralException( ushort line, String literal )
                : base( line, "An invalid literal was encountered: \"" + literal + "\"" )
            {
                Literal = literal;
            }
        }

        private abstract class Value
        {
            public ushort Line { get; private set; }
            public bool Reference { get; private set; }
            public bool Extended { get; protected set; }
            public ushort Assembled { get; protected set; }
            public ushort NextWord { get; protected set; }
            public String Disassembled { get; private set; }

            public Value( ushort line, bool reference )
            {
                Line = line;
                Reference = reference;
            }

            protected void SetDisassembled( String str )
            {
                if ( Reference )
                    str = "[" + str + "]";

                Disassembled = str;
            }

            public override String ToString()
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

            public RegisterVal( ushort line, String str, bool reference )
                : base( line, reference )
            {
                if ( str.Contains( "+" ) )
                {
                    String[] split = str.Split( '+' );
                    split[ 0 ] = split[ 0 ].Trim();
                    split[ 1 ] = split[ 1 ].Trim();

                    if( Enum.TryParse( split[ 0 ].ToUpper(), out Register ) )
                    {
                        if ( char.IsNumber( split[ 1 ][ 0 ] ) )
                            Offset = ParseNumber( Line, split[ 1 ] );
                        else
                            Label = split[ 1 ];
                    }
                    else if( Enum.TryParse( split[ 1 ].ToUpper(), out Register ) )
                    {
                        if ( char.IsNumber( split[ 0 ][ 0 ] ) )
                            Offset = ParseNumber( Line, split[ 0 ] );
                        else
                            Label = split[ 0 ];
                    }
                    else
                    {
                        if( reference )
                            str = "[" + str + "]";
                        throw new InvalidValueException( Line, str );
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

            public override void ResolveLabel( Dictionary<String, ushort> labels )
            {
                if ( Label != null )
                {
                    if ( labels.ContainsKey( Label ) )
                        Offset = labels[ Label ];
                    else
                        throw new InvalidLabelException( Line, Label );
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

            public SpecialVal( ushort line, String str, bool reference )
                : base( line, reference )
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

            public override void ResolveLabel( Dictionary<String, ushort> labels )
            {
                return;
            }
        }

        private class LiteralVal : Value
        {
            public readonly String Label;
            public ushort Literal;

            public LiteralVal( ushort line, char ch )
                : base( line, false )
            {
                Literal = (ushort) ch;
                Extended = true;
            }

            public LiteralVal( ushort line, String str, bool reference )
                : base( line, reference )
            {
                if ( str.Length == 0 )
                {
                    if ( reference )
                        str = "[" + str + "]";
                    throw new InvalidLiteralException( line, str );
                }

                if ( str[ 0 ] == '\'' )
                {
                    Literal = (ushort) str[ 1 ];
                    Extended = Literal >= 0x20;
                }
                else if ( char.IsNumber( str[ 0 ] ) )
                {
                    Literal = ParseNumber( Line, str );
                    Extended = Literal >= 0x20;
                }
                else
                {
                    Label = str;
                    Extended = true;
                }
            }

            public override void ResolveLabel( Dictionary<String, ushort> labels )
            {
                if ( Label != null )
                {
                    if ( labels.ContainsKey( Label ) )
                        Literal = labels[ Label ];
                    else
                        throw new InvalidLabelException( Line, Label );

                    Extended = true;
                }

                NextWord = Literal;

                if ( Extended )
                    Assembled = (ushort) ( 0x1e | ( Reference ? 0x0 : 0x1 ) );
                else
                    Assembled = (ushort) ( Literal | ( Reference ? 0x28 : 0x20 ) );

                SetDisassembled( "0x" + Literal.ToString( "X" ) );
            }
        }

        private class Instruction
        {
            public readonly Opcode Opcode;

            public readonly Value[] Values;

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
                Values = new Value[] { value };

                Length = 1 + ( Values[ 0 ].Extended ? 1 : 0 );
            }

            public Instruction( Opcode opcode, Value valueA, Value valueB )
                : this( opcode )
            {
                Values = new Value[] { valueA, valueB };

                Length = 1 + ( Values[ 0 ].Extended ? 1 : 0 ) + ( Values[ 1 ].Extended ? 1 : 0 );
            }

            public Instruction( LiteralVal[] values )
                : this( Opcode.Dat )
            {
                Values = values;
                Length = values.Length;
            }

            public void ResolveLabels( Dictionary<String, ushort> labels )
            {
                Assembled = new ushort[ Length ];

                if ( Opcode != Opcode.Dat )
                {
                    if ( Values.Length > 0 )
                    {
                        Values[ 0 ].ResolveLabel( labels );

                        if ( Values.Length > 1 )
                        {
                            Values[ 1 ].ResolveLabel( labels );

                            Assembled[ 0 ] = (ushort) ( (ushort) Opcode | ( Values[ 0 ].Assembled << 0x4 ) | ( Values[ 1 ].Assembled << 0xa ) );

                            if ( Values[ 0 ].Extended )
                            {
                                Assembled[ 1 ] = Values[ 0 ].NextWord;
                                if ( Values[ 1 ].Extended )
                                    Assembled[ 2 ] = Values[ 1 ].NextWord;
                            }
                            else if ( Values[ 1 ].Extended )
                                Assembled[ 1 ] = Values[ 1 ].NextWord;

                            Disassembled = Opcode.ToString().ToUpper() + " " + Values[ 0 ].ToString() + ", " + Values[ 1 ].ToString();
                        }
                        else
                        {
                            Assembled[ 0 ] = (ushort) ( (ushort) Opcode | ( Values[ 0 ].Assembled << 0xa ) );

                            if ( Values[ 0 ].Extended )
                                Assembled[ 1 ] = Values[ 0 ].NextWord;

                            Disassembled = Opcode.ToString().ToUpper() + " " + Values[ 0 ].ToString();
                        }
                    }
                }
                else
                {
                    Disassembled = Opcode.ToString().ToUpper() + " ";

                    for ( int i = 0; i < Values.Length; ++i )
                    {
                        Values[ i ].ResolveLabel( labels );
                        Assembled[ i ] = Values[ i ].NextWord;
                        Disassembled += Values[ i ].Disassembled;
                        if ( i < Values.Length - 1 )
                            Disassembled += ", ";
                    }
                }
            }
        }

        public static ushort[] Assemble( String str, ushort wordOffset = 0x0000 )
        {
            List<Instruction> instructions = new List<Instruction>();
            Dictionary<String, ushort> labels = new Dictionary<String, ushort>();
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
            String opString = null;

            while ( opString == null )
            {
                SkipWhitespace( str, ref offset );

                while ( offset < str.Length - 2 && str[ offset ] == ':' )
                {
                    String label = "";
                    while ( ++offset < str.Length && ( char.IsLetterOrDigit( str[ offset ] ) || str[ offset ] == '_' ) )
                        label += str[ offset ];

                    labels.Add( label, words );
                    SkipWhitespace( str, ref offset );
                }

                opString = "";

                while ( offset < str.Length && ( char.IsLetterOrDigit( str[ offset ] ) || str[ offset ] == '_' ) )
                    opString += str[ offset++ ];

                if ( offset >= str.Length )
                    return null;

                if ( str[ offset ] == ':' )
                {
                    ++offset;
                    labels.Add( opString, words );
                    opString = null;
                }
            }

            Opcode opcode = ParseOperator( GetLineNumber( str, offset ), opString );

            if ( opcode == Opcode.Dat )
            {
                List<LiteralVal> vals = new List<LiteralVal>();
                do
                {
                    SkipWhitespace( str, ref offset );
                    bool escaped = false;
                    if ( str[ offset ] == '"' )
                    {
                        while ( ++offset < str.Length && ( escaped || str[ offset ] != '"' ) )
                        {
                            if ( escaped )
                            {
                                switch ( str[ offset ] )
                                {
                                    case 'r':
                                        vals.Add( new LiteralVal( GetLineNumber( str, offset ), '\r' ) ); break;
                                    case '\\':
                                        vals.Add( new LiteralVal( GetLineNumber( str, offset ), '\\' ) ); break;
                                    case 'n':
                                        vals.Add( new LiteralVal( GetLineNumber( str, offset ), '\n' ) ); break;
                                    case 't':
                                        vals.Add( new LiteralVal( GetLineNumber( str, offset ), '\t' ) ); break;
                                    default:
                                        vals.Add( new LiteralVal( GetLineNumber( str, offset ), '\r' ) ); break;
                                }
                                escaped = false;
                                continue;
                            }
                            else if ( str[ offset ] == '\\' )
                            {
                                escaped = true;
                                continue;
                            }

                            vals.Add( new LiteralVal( GetLineNumber( str, offset ), str[ offset ] ) );
                        }

                        ++offset;
                    }
                    else
                        vals.Add( (LiteralVal) ReadValue( str, ref offset ) );
                }
                while ( offset < str.Length && str[ offset ] == ',' && ++offset < str.Length );

                return new Instruction( vals.ToArray() );
            }
            else
            {
                SkipWhitespace( str, ref offset );
                Value valA = ReadValue( str, ref offset );
                if ( valA == null )
                    return null;
                if ( (int) opcode < 0x10 )
                {
                    if ( str[ offset ] != ',' )
                        throw new AssemblerException( GetLineNumber( str, offset ), "Expected a \",\" between values" );
                    ++offset;
                    SkipWhitespace( str, ref offset );
                    Value valB = ReadValue( str, ref offset );
                    if ( valB == null )
                        return null;
                    return new Instruction( opcode, valA, valB );
                }
                return new Instruction( opcode, valA );
            }
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

        private static Opcode ParseOperator( ushort line, String str )
        {
            String opstr = str.ToUpper();

            switch ( opstr )
            {
                case "DAT":
                    return Opcode.Dat;
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
                    throw new InvalidOpcodeException( line, str );
            }
        }

        private static Value ReadValue( String str, ref int offset )
        {
            if ( offset >= str.Length )
                return null;

            bool reference;
            bool ischar = false;
            bool escaped = false;
            String val = "";

            reference = str[ offset ] == '[' && ( ++offset < str.Length );
            while ( offset < str.Length && ( ischar || ( !char.IsWhiteSpace( str[ offset ] ) && str[ offset ] != ',' && str[ offset ] != ']' ) ) )
            {
                if ( ischar )
                {
                    if ( escaped )
                    {
                        switch( str[ offset ] )
                        {
                            case 'r':
                                val += "\r"; break;
                            case '\\':
                                val += "\\"; break;
                            case 'n':
                                val += "\n"; break;
                            case 't':
                                val += "\t"; break;
                            default:
                                val += str[ offset ]; break;
                        }
                        escaped = false;
                        ++offset;
                        continue;
                    }
                    else if ( str[ offset ] == '\\' )
                    {
                        escaped = true;
                        ++offset;
                        continue;
                    }
                    else if ( str[ offset ] == '\'' )
                        ischar = false;
                }
                else if ( str[ offset ] == '\'' )
                    ischar = true;
                val += str[ offset++ ];
            }

            if ( offset < str.Length && str[ offset ] == ']' )
                ++offset;

            val = val.Trim();

            if ( val[ 0 ] != '\'' && val.Contains( "+" ) )
                return new RegisterVal( GetLineNumber( str, offset ), val, reference );

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
                    return new RegisterVal( GetLineNumber( str, offset ), val, reference );
                case "POP":
                case "PUSH":
                case "PEEK":
                case "SP":
                case "PC":
                case "O":
                    return new SpecialVal( GetLineNumber( str, offset ), val, reference );
                default:
                    return new LiteralVal( GetLineNumber( str, offset ), val, reference );
            }
        }

        private static ushort GetLineNumber( String str, int offset )
        {
            ushort num = 0;
            for ( int i = 0; i < Math.Min( str.Length, offset ); ++i )
                if ( str[ i ] == '\n' )
                    ++num;

            return num;
        }

        private static ushort ParseNumber( ushort line, String str )
        {
            try
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
            catch
            {
                throw new InvalidLiteralException( line, str );
            }
        }
    }
}
