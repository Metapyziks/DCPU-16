using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;

namespace DCPU16
{
    public static class DCPU16Assembler
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
                : this( GetLineNumber( str, offset ), GetColumnNumber( str, offset ), message )
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

        private abstract class Value
        {
            public readonly int Line;
            public readonly int Column;

            public bool Reference { get; private set; }
            public bool Extended { get; protected set; }
            public ushort Assembled { get; protected set; }
            public ushort NextWord { get; protected set; }
            public String Disassembled { get; private set; }

            public Value( String str, int offset, bool reference )
            {
                GetCharacterPosition( str, offset, out Line, out Column );
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

            public RegisterVal( String str, int offset, Register register, bool reference )
                : base( str, offset, reference )
            {
                Register = register;
                Label = null;
                Offset = 0x0000;
                Extended = false;
                Assembled = (ushort) ( (ushort) Register | ( Reference ? 0x08 : 0x00 ) );
                SetDisassembled( Register.ToString() );
            }

            public RegisterVal( String str, int offset, Register register, ushort addrOffset )
                : base( str, offset, true )
            {
                Register = register;
                Label = null;
                Offset = addrOffset;
                NextWord = Offset;
                Extended = true;
                Assembled = (ushort) ( (ushort) Register | 0x10 );
                SetDisassembled( "0x" + Offset.ToString( "X4" ) + "+" + Register.ToString() );
            }

            public RegisterVal( String str, int offset, Register register, String label )
                : base( str, offset, true )
            {
                Register = register;
                Label = label;
                Offset = 0x0000;
                Extended = true;
            }

            public override void ResolveLabel( Dictionary<String, ushort> labels )
            {
                if ( Label != null )
                {
                    if ( labels.ContainsKey( Label ) )
                    {
                        Offset = labels[ Label ];
                        NextWord = Offset;

                        Assembled = (ushort) ( (ushort) Register | ( Reference ? Extended ? 0x10 : 0x08 : 0x00 ) );
                        SetDisassembled( "0x" + Offset.ToString( "X4" ) + "+" + Register.ToString() );
                    }
                    else
                        throw new InvalidLabelException( Line, Column, Label );
                }
            }
        }

        private class SpecialVal : Value
        {
            public readonly SpecialRegister Type;

            public SpecialVal( String str, int offset, SpecialRegister type )
                : base( str, offset, false )
            {
                Extended = false;
                Type = type;
                Assembled = (ushort) Type;
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

            public LiteralVal( String str, int offset, char ch )
                : base( str, offset, false )
            {
                Literal = (ushort) ch;
                NextWord = Literal;
                Label = null;
                Extended = true;
                Assembled = (ushort) ( 0x1e | ( Reference ? 0x0 : 0x1 ) );
                if ( char.IsLetterOrDigit( ch ) || char.IsSymbol( ch ) || char.IsPunctuation( ch ) || ch == ' ' )
                    SetDisassembled( "'" + ch + "'" );
                else
                {
                    switch( ch )
                    {
                        case '\r':
                            SetDisassembled( "'\\r'" ); break;
                        case '\n':
                            SetDisassembled( "'\\n'" ); break;
                        case '\t':
                            SetDisassembled( "'\\t'" ); break;
                        default:
                            SetDisassembled( "0x" + Literal.ToString( "X2" ) ); break;
                    }
                }
            }

            public LiteralVal( String str, int offset, ushort value, bool reference )
                : base( str, offset, reference )
            {
                Literal = (ushort) value;
                NextWord = Literal;
                Label = null;
                Extended = Literal >= 0x20;
                if ( Extended )
                    Assembled = (ushort) ( 0x1e | ( Reference ? 0x0 : 0x1 ) );
                else
                    Assembled = (ushort) ( Literal | ( Reference ? 0x28 : 0x20 ) );
                SetDisassembled( "0x" + Literal.ToString( "X4" ) );
            }

            public LiteralVal( String str, int offset, String label, bool reference )
                : base( str, offset, reference )
            {
                Literal = 0x0000;
                Label = label;
                Extended = true;
                Assembled = (ushort) ( 0x1e | ( Reference ? 0x0 : 0x1 ) );
            }

            public override void ResolveLabel( Dictionary<String, ushort> labels )
            {
                if ( Label != null )
                {
                    if ( labels.ContainsKey( Label ) )
                    {
                        Literal = labels[ Label ];
                        SetDisassembled( "0x" + Literal.ToString( "X4" ) );
                        NextWord = Literal;
                    }
                    else
                        throw new InvalidLabelException( Line, Column, Label );
                }
            }
        }

        private enum Command
        {
            Define,
            LDefine,
            Include
        }

        private class CommandInfo
        {
            public readonly Command Command;
            public readonly String[] Args;

            public CommandInfo( Command command, String str, ref int offset )
            {
                Command = command;
                int argCount = 0;
                switch( command )
                {
                    case Command.Define:
                    case Command.LDefine:
                        argCount = 2; break;
                    case Command.Include:
                        argCount = 1; break;
                }
                Args = new String[ argCount ];
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
                            switch( str[ offset ] )
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
                    Args[ i++ ] = arg;
                }
            }
        }

        private class Instruction
        {
            public readonly Opcode Opcode;

            public readonly Value[] Values;

            public ushort Length { get; private set; }
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

                Length = (ushort) ( 1 + ( Values[ 0 ].Extended ? 1 : 0 ) );
            }

            public Instruction( Opcode opcode, Value valueA, Value valueB )
                : this( opcode )
            {
                Values = new Value[] { valueA, valueB };

                Length = (ushort) ( 1 + ( Values[ 0 ].Extended ? 1 : 0 ) + ( Values[ 1 ].Extended ? 1 : 0 ) );
            }

            public Instruction( LiteralVal[] values )
                : this( Opcode.Dat )
            {
                Values = values;
                Length = (ushort) ( values.Length );
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

        private class DCPU16Assembly
        {
            private Dictionary<String, ushort> myLocalConstants;
            private Dictionary<ushort, Dictionary<String, ushort>> myLocalLabels;

            public readonly DCPU16Assembly Parent;
            public readonly ushort Offset;
            public readonly String Code;
            public ushort Length { get; private set; }
            public Instruction[] Instructions { get; private set; }
            public Dictionary<String, ushort> Constants { get; private set; }
            public DCPU16Assembly[] Children { get; private set; }
            public ushort[] Assembled;
        
            public DCPU16Assembly( String code, ushort offset = 0x0000 )
            {
                Parent = null;
                Offset = offset;
                Code = code;
                Constants = new Dictionary<String, ushort>();

                Assemble();
                ResolveLabels();
            }

            public DCPU16Assembly( String code, ushort offset, DCPU16Assembly parent )
            {
                Parent = parent;
                Offset = offset;
                Code = code;
                Constants = parent.Constants;

                Assemble();
            }

            private void Assemble()
            {
                List<Instruction> instructions = new List<Instruction>();
                List<DCPU16Assembly> children = new List<DCPU16Assembly>();
                myLocalConstants = new Dictionary<String, ushort>();
                myLocalLabels = new Dictionary<ushort, Dictionary<String, ushort>>();
                Dictionary<String, ushort> lastLocals = new Dictionary<string, ushort>();
                myLocalLabels.Add( Offset, lastLocals );

                String str = Code;

                int offset = 0;
                ushort word = Offset;
                while ( offset < str.Length )
                {
                    SkipWhitespace( str, ref offset );
                    if ( IsNextCommand( str, offset ) )
                    {
                        int startOffset = offset;
                        CommandInfo cmd = ReadCommand( str, ref offset );
                        switch( cmd.Command )
                        {
                            case Command.Define:
                            case Command.LDefine:
                                ushort val;
                                try
                                {
                                    val = ParseLiteral( cmd.Args[ 1 ] );
                                }
                                catch ( AssemblerException )
                                {
                                    throw new AssemblerException( str, startOffset, "Invalid second argument for definition, expected literal value" );
                                }
                                if ( cmd.Command == Command.Define )
                                {
                                    if ( Constants.ContainsKey( cmd.Args[ 0 ] ) )
                                        throw new AssemblerException( str, startOffset, "A value has already been defined for the identifier: " + cmd.Args[ 0 ] );
                                    Constants.Add( cmd.Args[ 0 ], val );
                                }
                                else
                                {
                                    if ( myLocalConstants.ContainsKey( cmd.Args[ 0 ] ) )
                                        throw new AssemblerException( str, startOffset, "A value has already been defined for the identifier: " + cmd.Args[ 0 ] );
                                    myLocalConstants.Add( cmd.Args[ 0 ], val );
                                }
                                break;
                            case Command.Include:
                                String path = cmd.Args[ 0 ];
                                if( !File.Exists( path ) )
                                    throw new AssemblerException( str, startOffset, "Cannot include file at \"" + path + "\", file does not exist" );
                                String code = File.ReadAllText( path );
                                DCPU16Assembly asm;
                                try
                                {
                                    asm = new DCPU16Assembly( code, word, this );
                                }
                                catch ( AssemblerException e )
                                {
                                    throw new AssemblerException( str, startOffset, e.Message + "\n  in included file \"" + path + "\"" );
                                }
                                word += asm.Length;
                                children.Add( asm );
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

                        if ( local )
                        {
                            if ( lastLocals.ContainsKey( label ) )
                                throw new AssemblerException( str, startOffset, "A value has already been defined for the identifier: " + label );
                            lastLocals.Add( label, word );
                        }
                        else
                        {
                            if ( Constants.ContainsKey( label ) )
                                throw new AssemblerException( str, startOffset, "A value has already been defined for the identifier: " + label );
                            Constants.Add( label, word );
                            if ( !myLocalLabels.ContainsKey( word ) )
                            {
                                lastLocals = new Dictionary<String, ushort>();
                                myLocalLabels.Add( word, lastLocals );
                            }
                        }
                        continue;
                    }
                    if ( IsNextOpcode( str, offset ) )
                    {
                        Instruction ins = ReadInstruction( str, ref offset );
                        instructions.Add( ins );
                        word += ins.Length;
                        continue;
                    }
                    if( offset < str.Length )
                        throw new AssemblerException( str, offset, "Expected .command, :label or opcode" );
                }
                Length = (ushort) ( word - Offset );
                Instructions = instructions.ToArray();
                Children = children.ToArray();
            }

            private void ResolveLabels()
            {
                Assembled = new ushort[ Length ];

                int childIndex = 0;

                ushort i = Offset;
                Dictionary<String, ushort> dict = ConstructValueDictionary( Offset );
                foreach ( Instruction ins in Instructions )
                {
                    while ( childIndex < Children.Length && Children[ childIndex ].Offset == i )
                    {
                        DCPU16Assembly child = Children[ childIndex++ ];
                        child.ResolveLabels();

                        foreach ( ushort word in child.Assembled )
                            Assembled[ i++ - Offset ] = word;
                    }

                    if ( myLocalLabels.ContainsKey( i ) )
                        dict = ConstructValueDictionary( i );

                    ins.ResolveLabels( dict );

                    foreach ( ushort word in ins.Assembled )
                        Assembled[ i++ - Offset ] = word;
                }

                foreach ( DCPU16Assembly child in Children )
                    child.ResolveLabels();
            }

            private Dictionary<String, ushort> ConstructValueDictionary( ushort word )
            {
                Dictionary<String, ushort> dict = new Dictionary<String, ushort>();

                foreach ( KeyValuePair<String, ushort> entry in myLocalLabels[ word ] )
                    dict.Add( entry.Key, entry.Value );
                foreach ( KeyValuePair<String, ushort> entry in myLocalConstants )
                    if ( !dict.ContainsKey( entry.Key ) )
                        dict.Add( entry.Key, entry.Value );
                foreach ( KeyValuePair<String, ushort> entry in Constants )
                    if( !dict.ContainsKey( entry.Key ) )
                        dict.Add( entry.Key, entry.Value );

                return dict;
            }
        }

        public static ushort[] AssembleString( String src )
        {
            DCPU16Assembly asm = new DCPU16Assembly( src );
            return asm.Assembled;
        }

        public static ushort[] AssembleFile( String filePath )
        {
            if ( !File.Exists( filePath ) )
                throw new FileNotFoundException( "Cannot assemble file - file not found", filePath );

            return AssembleString( File.ReadAllText( filePath ) );
        }

        private static Instruction ReadInstruction( String str, ref int offset )
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

                return new Instruction( vals.ToArray() );
            }
            else
            {
                SkipWhitespace( str, ref offset );
                Value valA = ReadValue( str, ref offset );
                if ( (int) opcode < 0x10 )
                {
                    if ( str[ offset ] != ',' )
                        throw new AssemblerException( str, offset, "Expected a , between values" );
                    ++offset;
                    SkipWhitespace( str, ref offset );
                    Value valB = ReadValue( str, ref offset );
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

        private static bool IsNextCommand( String str, int offset )
        {
            if ( offset < str.Length && str[ offset ] == '.' )
                return true;

            return false;
        }

        private static bool IsNextLabelDefinition( String str, int offset )
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

        private static CommandInfo ReadCommand( String str, ref int offset )
        {
            int startOffset = ++offset;
            String cmd = "";
            while ( offset < str.Length && char.IsLetterOrDigit( str[ offset ] ) )
                cmd += str[ offset++ ];
            foreach ( String name in Enum.GetNames( typeof( Command ) ) )
            {
                if ( name.ToLower() == cmd )
                {
                    Command cmdType = (Command) Enum.Parse( typeof( Command ), name );
                    return new CommandInfo( cmdType, str, ref offset );
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

        private static Value ReadValue( String str, ref int offset )
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

        private static ushort ReadLiteral( String str, ref int offset )
        {
            if ( offset >= str.Length )
                throw new AssemblerException( str, offset, "Expected a literal value" );

            if ( char.IsDigit( str[ offset ] ) )
                return ReadNumericalLiteral( str, ref offset );
            else if ( str[ offset ] == '\'' )
                return ReadCharacterLiteral( str, ref offset );

            throw new AssemblerException( str, offset, "Unexpected character encountered: " + str[ offset ] );
        }

        private static ushort ParseLiteral( String str )
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

        private static Register ReadRegister( String str, ref int offset )
        {
            String sub = "";
            while ( offset < str.Length && char.IsLetter( str[ offset ] ) )
                sub += char.ToUpper( str[ offset++ ] );

            return (Register) Enum.Parse( typeof( Register ), sub );
        }

        private static String ReadLabel( String str, ref int offset )
        {
            String sub = "";
            while ( offset < str.Length && ( char.IsLetterOrDigit( str[ offset ] ) || str[ offset ] == '_' ) )
                sub += str[ offset++ ];

            return sub;
        }

        private static void GetCharacterPosition( String str, int offset, out int line, out int column )
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

        private static int GetLineNumber( String str, int offset )
        {
            int line, col;
            GetCharacterPosition( str, offset, out line, out col );
            return line;
        }

        private static int GetColumnNumber( String str, int offset )
        {
            int line, col;
            GetCharacterPosition( str, offset, out line, out col );
            return col;
        }
    }
}
