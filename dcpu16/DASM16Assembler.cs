using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;

namespace DCPU16
{
    public class AssemblerException : Exception
    {
        public readonly int Line;
        public readonly int Column;

        public AssemblerException( int line, int column, String message )
            : base( message + "\n  at line " + ( line + 1 ) + ", column " + ( column + 1 ) )
        {
            Line = line;
            Column = column;
        }

        public AssemblerException( String str, int offset, String message )
            : this( DASM16Assembler.GetLineNumber( str, offset ), DASM16Assembler.GetColumnNumber( str, offset ), message )
        {

        }
    }

    public class InvalidOpcodeException : AssemblerException
    {
        public readonly String Opcode;

        public InvalidOpcodeException( int line, int column, String opcode )
            : base( line, column, "An invalid operator code was encountered: \"" + opcode + "\"" )
        {
            Opcode = opcode;
        }

        public InvalidOpcodeException( String str, int offset, String opcode )
            : base( str, offset, "An invalid operator code was encountered: \"" + opcode + "\"" )
        {
            Opcode = opcode;
        }
    }

    public class InvalidValueException : AssemblerException
    {
        public readonly String Value;

        public InvalidValueException( int line, int column, String value )
            : base( line, column, "An invalid value was encountered: \"" + value + "\"" )
        {
            Value = value;
        }

        public InvalidValueException( String str, int offset, String value )
            : base( str, offset, "An invalid value was encountered: \"" + value + "\"" )
        {
            Value = value;
        }
    }

    public class InvalidCommandException : AssemblerException
    {
        public readonly String Command;

        public InvalidCommandException( int line, int column, String command )
            : base( line, column, "An invalid command was encountered: \"." + command + "\"" )
        {
            Command = command;
        }

        public InvalidCommandException( String str, int offset, String command )
            : base( str, offset, "An invalid command was encountered: \"." + command + "\"" )
        {
            Command = command;
        }
    }

    public class InvalidLabelException : AssemblerException
    {
        public readonly String Label;

        public InvalidLabelException( int line, int column, String label )
            : base( line, column, "An invalid label was encountered: \"" + label + "\"" )
        {
            Label = label;
        }

        public InvalidLabelException( String str, int offset, String label )
            : base( str, offset, "An invalid label was encountered: \"" + label + "\"" )
        {
            Label = label;
        }
    }

    public class InvalidLiteralException : AssemblerException
    {
        public readonly String Literal;

        public InvalidLiteralException( int line, int column, String literal )
            : base( line, column, "An invalid literal was encountered: \"" + literal + "\"" )
        {
            Literal = literal;
        }

        public InvalidLiteralException( String str, int offset, String literal )
            : base( str, offset, "An invalid literal was encountered: \"" + literal + "\"" )
        {
            Literal = literal;
        }
    }

    public static class DASM16Assembler
    {
        public static DASM16Assembly AssembleString( String src, ushort offset = 0x0000 )
        {
            return Parse( src ).Assemble( offset );
        }

        public static DASM16Assembly AssembleFile( String filePath, ushort offset = 0x0000 )
        {
            if ( !File.Exists( filePath ) )
                throw new FileNotFoundException( "Cannot assemble file - file not found", filePath );

            return Parse( File.ReadAllText( filePath ) ).Assemble( offset );
        }

        private static DASM16Builder Parse( String str )
        {
            DASM16Builder builder = new DASM16Builder();

            int offset = 0;
            while ( offset < str.Length )
            {
                SkipWhitespace( str, ref offset );
                if ( IsNextCommand( str, offset ) )
                {
                    int startOffset = offset;
                    DASM16CommandInfo cmd = ReadCommand( str, ref offset );
                    switch ( cmd.Command )
                    {
                        case DASM16Command.Define:
                        case DASM16Command.LDefine:
                            ushort val;
                            try
                            {
                                val = ParseLiteral( cmd.Args[ 1 ] );
                            }
                            catch ( AssemblerException )
                            {
                                throw new AssemblerException( str, startOffset, "Invalid second argument for definition, expected literal value" );
                            }
                            builder.AddConstant( cmd.Args[ 0 ], val, cmd.Command == DASM16Command.LDefine );
                            break;
                        case DASM16Command.Include:
                            String path = cmd.Args[ 0 ];
                            if ( !File.Exists( path ) )
                                throw new AssemblerException( str, startOffset, "Cannot include file at \"" + path + "\", file does not exist" );
                            String code = File.ReadAllText( path );
                            try
                            {
                                builder.AddChild( Parse( code ) );
                            }
                            catch ( AssemblerException e )
                            {
                                throw new AssemblerException( str, startOffset, e.Message + "\n  in included file \"" + path + "\"" );
                            }
                            break;
                    }
                    continue;
                }
                if ( IsNextLabelDefinition( str, offset ) )
                {
                    int startOffset = offset;
                    if ( ++offset >= str.Length )
                        throw new AssemblerException( str, offset, "Unexpected end of file" );

                    bool local = str[ offset ] == ':';
                    if ( local )
                        ++offset;
                    SkipWhitespace( str, ref offset );

                    if ( offset >= str.Length )
                        throw new AssemblerException( str, offset, "Unexpected end of file" );

                    String label = ReadLabel( str, ref offset );

                    if ( label.Length == 0 )
                        throw new AssemblerException( str, offset, "Label identifier expected" );

                    builder.AddLabel( label, local );
                    continue;
                }
                if ( IsNextOpcode( str, offset ) )
                {
                    builder.AddInstruction( ReadInstruction( str, ref offset ) );
                    continue;
                }
                if ( offset < str.Length )
                    throw new AssemblerException( str, offset, "Expected .command, :label or opcode" );
            }
            return builder;
        }
        
        private static DASM16Instruction ReadInstruction( String str, ref int offset )
        {
            Opcode opcode = ReadOpcode( str, ref offset );

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
                                        vals.Add( new LiteralVal( str, offset, '\r' ) ); break;
                                    case '\\':
                                        vals.Add( new LiteralVal( str, offset, '\\' ) ); break;
                                    case 'n':
                                        vals.Add( new LiteralVal( str, offset, '\n' ) ); break;
                                    case 't':
                                        vals.Add( new LiteralVal( str, offset, '\t' ) ); break;
                                    default:
                                        vals.Add( new LiteralVal( str, offset, '\r' ) ); break;
                                }
                                escaped = false;
                                continue;
                            }
                            else if ( str[ offset ] == '\\' )
                            {
                                escaped = true;
                                continue;
                            }

                            vals.Add( new LiteralVal( str, offset, str[ offset ] ) );
                        }

                        ++offset;
                    }
                    else
                        vals.Add( (LiteralVal) ReadValue( str, ref offset ) );
                }
                while ( offset < str.Length && str[ offset ] == ',' && ++offset < str.Length );

                return new DASM16Instruction( vals.ToArray() );
            }
            else
            {
                SkipWhitespace( str, ref offset );
                DASM16Value valA = ReadValue( str, ref offset );
                if ( (int) opcode < 0x10 )
                {
                    if ( str[ offset ] != ',' )
                        throw new AssemblerException( str, offset, "Expected a , between values" );
                    ++offset;
                    SkipWhitespace( str, ref offset );
                    DASM16Value valB = ReadValue( str, ref offset );
                    return new DASM16Instruction( opcode, valA, valB );
                }
                return new DASM16Instruction( opcode, valA );
            }
        }

        internal static void SkipWhitespace( String str, ref int offset )
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

        internal static bool IsNextCommand( String str, int offset )
        {
            if ( offset < str.Length && str[ offset ] == '.' )
                return true;

            return false;
        }

        internal static bool IsNextLabelDefinition( String str, int offset )
        {
            if ( offset < str.Length && str[ offset ] == ':' )
                return true;

            return false;
        }

        private static bool IsNextOpcode( String str, int offset )
        {
            if ( offset + 2 >= str.Length )
                return false;

            String sub = str.Substring( offset, 3 );
            foreach ( Opcode op in Enum.GetValues( typeof( Opcode ) ) )
            {
                String name = op.ToString();
                if ( name.ToUpper() == sub || name.ToLower() == sub )
                    return true;
            }
            return false;
        }

        private static bool IsNextLiteral( String str, int offset )
        {
            if ( offset >= str.Length )
                return false;
            char ch = str[ offset ];
            return ch == '\'' || char.IsDigit( ch );
        }

        private static bool IsNextRegister( String str, int offset )
        {
            if ( offset >= str.Length )
                return false;
            char ch = char.ToUpper( str[ offset++ ] );
            if ( char.IsLetter( ch ) && ( offset >= str.Length || !( char.IsLetterOrDigit( str[ offset ] ) || str[ offset ] == '_' ) ) )
            {
                Register temp;
                return Enum.TryParse<Register>( ch.ToString(), out temp );
            }
            return false;
        }

        private static bool IsNextSpecial( String str, int offset )
        {
            if ( offset >= str.Length )
                return false;
            String sub = "";
            while ( offset < str.Length && char.IsLetter( str[ offset ] ) )
                sub += char.ToUpper( str[ offset++ ] );
            if ( sub.Length == 0 || sub.Length > 4 )
                return false;
            SpecialRegister temp;
            return Enum.TryParse<SpecialRegister>( sub, out temp );
        }

        private static bool IsNextLabel( String str, int offset )
        {
            if ( offset >= str.Length )
                return false;
            String sub = "";
            while ( offset < str.Length && ( char.IsLetterOrDigit( str[ offset ] ) || str[ offset ] == '_' ) )
                sub += char.ToUpper( str[ offset++ ] );
            return sub.Length > 0;
        }

        private static DASM16CommandInfo ReadCommand( String str, ref int offset )
        {
            int startOffset = ++offset;
            String cmd = "";
            while ( offset < str.Length && char.IsLetterOrDigit( str[ offset ] ) )
                cmd += str[ offset++ ];
            foreach ( String name in Enum.GetNames( typeof( DASM16Command ) ) )
            {
                if ( name.ToLower() == cmd )
                {
                    DASM16Command command = (DASM16Command) Enum.Parse( typeof( DASM16Command ), name );
                    int argCount = 0;
                    switch ( command )
                    {
                        case DASM16Command.Define:
                        case DASM16Command.LDefine:
                            argCount = 2; break;
                        case DASM16Command.Include:
                            argCount = 1; break;
                    }
                    String[] args = new String[ argCount ];
                    int i = 0;
                    while ( i < argCount )
                    {
                        SkipWhitespace( str, ref offset );
                        String arg = "";
                        bool isString = false;
                        bool escaped = false;
                        while ( offset < str.Length )
                        {
                            if ( !isString )
                            {
                                if ( str[ offset ] == '"' )
                                    isString = true;
                                else if ( char.IsWhiteSpace( str[ offset ] ) )
                                    break;
                                else
                                    arg += str[ offset ];
                            }
                            else if ( !escaped )
                            {
                                if ( str[ offset ] == '\\' )
                                    escaped = true;
                                else if ( str[ offset ] == '"' )
                                {
                                    isString = false;
                                    ++offset;
                                    break;
                                }
                                else
                                    arg += str[ offset ];
                            }
                            else
                            {
                                switch ( str[ offset ] )
                                {
                                    case '\r':
                                        arg += '\r'; break;
                                    case '\n':
                                        arg += '\n'; break;
                                    case '\t':
                                        arg += '\t'; break;
                                    default:
                                        arg += str[ offset ]; break;
                                }
                            }
                            ++offset;
                        }
                        args[ i++ ] = arg;
                    }
                    return new DASM16CommandInfo( command, args );
                }
            }
            throw new InvalidCommandException( str, startOffset, cmd );
        }

        private static Opcode ReadOpcode( String str, ref int offset )
        {
            String sub = str.Substring( offset, 3 );
            offset += 3;
            foreach ( Opcode op in Enum.GetValues( typeof( Opcode ) ) )
            {
                String name = op.ToString();
                if ( name.ToUpper() == sub || name.ToLower() == sub )
                    return op;
            }
            throw new AssemblerException( str, offset - 3, "Unrecognised opcode encountered: " + sub );
        }

        private static DASM16Value ReadValue( String str, ref int offset )
        {
            if ( offset >= str.Length )
                throw new AssemblerException( str, offset, "Unexpected end of file" );

            int startOffset = offset;
            bool reference = false;
            if ( str[ offset ] == '[' )
            {
                reference = true;
                ++offset;
                SkipWhitespace( str, ref offset );
            }
            if ( offset >= str.Length )
                throw new AssemblerException( str, offset, "Unexpected end of file" );

            if ( IsNextSpecial( str, offset ) )
            {
                if ( reference )
                    throw new AssemblerException( str, startOffset, "Special register used as a reference" );
                return new SpecialVal( str, startOffset, ReadSpecialRegister( str, ref offset ) );
            }
            if ( IsNextRegister( str, offset ) )
            {
                Register reg = ReadRegister( str, ref offset );

                if ( !reference )
                    return new RegisterVal( str, startOffset, reg, false );

                SkipWhitespace( str, ref offset );

                if ( offset >= str.Length )
                    throw new AssemblerException( str, offset, "Unexpected end of file" );

                if ( str[ offset ] == '+' )
                {
                    ++offset;
                    SkipWhitespace( str, ref offset );
                    if ( offset >= str.Length )
                        throw new AssemblerException( str, offset, "Unexpected end of file" );

                    String label = null;
                    ushort literal = 0x0000;

                    if ( IsNextLiteral( str, offset ) )
                        literal = ReadLiteral( str, ref offset );
                    else if ( IsNextLabel( str, offset ) )
                        label = ReadLabel( str, ref offset );
                    else
                        throw new AssemblerException( str, offset, "Unexpected character encountered: " + str[ offset ] );

                    SkipWhitespace( str, ref offset );
                    if ( offset >= str.Length || str[ offset ] != ']' )
                        throw new AssemblerException( str, offset, "Expected a ] at the end of a reference value" );
                    ++offset;

                    if ( label == null )
                        return new RegisterVal( str, startOffset, reg, literal );
                    else
                        return new RegisterVal( str, startOffset, reg, label );

                }
                if ( str[ offset ] == ']' )
                {
                    ++offset;
                    return new RegisterVal( str, startOffset, reg, true );
                }

                throw new AssemblerException( str, offset, "Expected a ] at the end of a reference value" );
            }
            bool isLiteral = IsNextLiteral( str, offset );
            if ( isLiteral || IsNextLabel( str, offset ) )
            {
                String label = null;
                ushort literal = 0x0000;

                if ( isLiteral )
                    literal = ReadLiteral( str, ref offset );
                else
                    label = ReadLabel( str, ref offset );

                if ( !reference )
                {
                    if ( isLiteral )
                        return new LiteralVal( str, startOffset, literal, false );
                    else
                        return new LiteralVal( str, startOffset, label, false );
                }

                SkipWhitespace( str, ref offset );

                if ( offset >= str.Length )
                    throw new AssemblerException( str, offset, "Unexpected end of file" );

                if ( str[ offset ] == '+' )
                {
                    ++offset;
                    SkipWhitespace( str, ref offset );
                    if ( offset >= str.Length )
                        throw new AssemblerException( str, offset, "Unexpected end of file" );

                    if ( IsNextRegister( str, offset ) )
                    {
                        Register reg = ReadRegister( str, ref offset );
                        SkipWhitespace( str, ref offset );
                        if ( offset >= str.Length || str[ offset ] != ']' )
                            throw new AssemblerException( str, offset, "Expected a ] at the end of a reference value" );
                        ++offset;
                        if ( isLiteral )
                            return new RegisterVal( str, startOffset, reg, literal );
                        else
                            return new RegisterVal( str, startOffset, reg, label );
                    }
                    throw new AssemblerException( str, offset, "Unexpected character encountered: " + str[ offset ] );
                }
                if ( str[ offset ] == ']' )
                {
                    ++offset;
                    if ( isLiteral )
                        return new LiteralVal( str, startOffset, literal, true );
                    else
                        return new LiteralVal( str, startOffset, label, true );
                }

                throw new AssemblerException( str, offset, "Expected a ] at the end of a reference value" );
            }

            throw new AssemblerException( str, offset, "Expected a value" );
        }

        internal static ushort ReadLiteral( String str, ref int offset )
        {
            if ( offset >= str.Length )
                throw new AssemblerException( str, offset, "Expected a literal value" );

            if ( char.IsDigit( str[ offset ] ) )
                return ReadNumericalLiteral( str, ref offset );
            else if ( str[ offset ] == '\'' )
                return ReadCharacterLiteral( str, ref offset );

            throw new AssemblerException( str, offset, "Unexpected character encountered: " + str[ offset ] );
        }

        internal static ushort ParseLiteral( String str )
        {
            int temp = 0;
            return ReadLiteral( str, ref temp );
        }

        private static readonly char[] stNumericalChars = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'a', 'b', 'c', 'd', 'e', 'f' };

        private static ushort ReadNumericalLiteral( String str, ref int offset )
        {
            int b = 10;
            if ( str[ offset ] == '0' )
            {
                if( ++offset >= str.Length || !( char.IsDigit( str[ offset ] )
                    || str[ offset ] == 'b' || str[ offset ] == 'd' || str[ offset ] == 'x' ) )
                    return 0;
                b = 8;
                if( char.IsLetter( str[ offset ] ) )
                {
                    switch( str[ offset ] )
                    {
                        case 'b':
                            b = 2; break;
                        case 'd':
                            b = 10; break;
                        case 'x':
                            b = 16; break;
                        default:
                            throw new AssemblerException( str, offset, "Unexpected character encountered: " + str[ offset ] );
                    }
                    ++offset;
                }
            }
            String sub = "";
            while ( offset < str.Length )
            {
                char ch = char.ToLower( str[ offset ] );
                int index = Array.IndexOf( stNumericalChars, ch );
                if ( index == -1 )
                {
                    if ( char.IsLetter( ch ) )
                        throw new AssemblerException( str, offset, "Unexpected character encountered: " + ch );
                    break;
                }
                if ( index >= b )
                    throw new AssemblerException( str, offset, "Unexpected character encountered: " + str[ ch ] );
                sub += ch;
                ++offset;
            }
            try { return Convert.ToUInt16( sub, b ); }
            catch { }
            throw new InvalidLiteralException( str, offset, sub );
        }

        private static ushort ReadCharacterLiteral( String str, ref int offset )
        {
            ++offset;
            bool escaped = false;
            if ( offset >= str.Length || str[ offset ] == '\n' || str[ offset ] == '\'' )
                throw new AssemblerException( str, offset, "Expected a character in character literal" );
            if ( str[ offset ] == '\\' )
            {
                escaped = true;
                ++offset;
                if ( offset >= str.Length || str[ offset ] == '\n' )
                    throw new AssemblerException( str, offset, "Expected a character in character literal" );
            }
            char ch = str[ offset++ ];
            if ( escaped )
            {
                switch ( ch )
                {
                    case 'n':
                        ch = '\n';
                        break;
                    case 't':
                        ch = '\t';
                        break;
                    case 'r':
                        ch = '\r';
                        break;
                }
            }
            if ( offset >= str.Length || str[ offset ] != '\'' )
                throw new AssemblerException( str, offset, "Expected a ' at the end of character literal" );
            ++offset;
            return (ushort) ch;
        }

        private static SpecialRegister ReadSpecialRegister( String str, ref int offset )
        {
            String sub = "";
            while ( offset < str.Length && char.IsLetter( str[ offset ] ) )
                sub += char.ToUpper( str[ offset++ ] );

            return (SpecialRegister) Enum.Parse( typeof( SpecialRegister ), sub );
        }

        internal static Register ReadRegister( String str, ref int offset )
        {
            String sub = "";
            while ( offset < str.Length && char.IsLetter( str[ offset ] ) )
                sub += char.ToUpper( str[ offset++ ] );

            return (Register) Enum.Parse( typeof( Register ), sub );
        }

        internal static String ReadLabel( String str, ref int offset )
        {
            String sub = "";
            while ( offset < str.Length && ( char.IsLetterOrDigit( str[ offset ] ) || str[ offset ] == '_' ) )
                sub += str[ offset++ ];

            return sub;
        }

        internal static void GetCharacterPosition( String str, int offset, out int line, out int column )
        {
            line = 0;
            column = 0;
            for ( int i = 0; i < Math.Min( str.Length, offset ); ++i )
            {
                if ( str[ i ] == '\n' )
                {
                    ++line;
                    column = 0;
                }
                else
                    ++column;
            }
        }

        internal static int GetLineNumber( String str, int offset )
        {
            int line, col;
            GetCharacterPosition( str, offset, out line, out col );
            return line;
        }

        internal static int GetColumnNumber( String str, int offset )
        {
            int line, col;
            GetCharacterPosition( str, offset, out line, out col );
            return col;
        }
    }
}
