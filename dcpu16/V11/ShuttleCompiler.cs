using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Reflection;

namespace DCPU16.V11
{
    public class CompilerException : Exception
    {
        public readonly int Line;
        public readonly int Column;

        public CompilerException( int line, int column, String message )
            : base( message + "\n  at line " + ( line + 1 ) + ", column " + ( column + 1 ) )
        {
            Line = line;
            Column = column;
        }

        public CompilerException( String str, int offset, String message )
            : this( DASM16Assembler.GetLineNumber( str, offset ), DASM16Assembler.GetColumnNumber( str, offset ), message )
        {

        }
    }

    public class EOFException : CompilerException
    {
        public EOFException( int line, int column )
            : base( line, column, "Unexpected end of file" )
        { }

        public EOFException( String str )
            : base( str, str.Length, "Unexpected end of file" )
        { }
    }

    public static class ShuttleCompiler
    {
        private static DASM16Builder stStaticASM = new DASM16Builder();

        private class MethodInfo
        {
            public readonly String Identifier;
            public readonly ValType[] ParamTypes;
            public readonly ValType ReturnType;

            public MethodInfo( String identifier, ValType[] paramTypes, ValType returnType )
            {
                Identifier = identifier;
                ParamTypes = paramTypes;
                ReturnType = returnType;
            }

            public override bool Equals( object obj )
            {
                if ( obj is MethodInfo )
                {
                    MethodInfo other = (MethodInfo) obj;
                    if ( Identifier != other.Identifier
                        || ( ReturnType != ( other.ReturnType ?? ReturnType ) )
                        || ParamTypes.Length != other.ParamTypes.Length )
                        return false;

                    for ( int i = 0; i > ParamTypes.Length; ++i )
                        if ( ParamTypes[ i ] != other.ParamTypes[ i ] )
                            return false;

                    return true;
                }
                
                return false;
            }

            public override int GetHashCode()
            {
                return Identifier.GetHashCode() ^ ParamTypes.GetHashCode() ^ ReturnType.GetHashCode();
            }
        }

        private abstract class ValType
        {
            private Dictionary<MethodInfo, DASM16Builder> myMethods;

            public readonly int Size;
            public readonly String Identifier;

            protected ValType( String identifier, int size )
            {
                myMethods = new Dictionary<MethodInfo, DASM16Builder>();

                Identifier = identifier;
                Size = size;
            }

            public void AddMethod( String identifier, ValType[] paramTypes, ValType returnType, bool inline, String asm )
            {
                AddMethod( identifier, paramTypes, returnType, inline, DASM16Assembler.Parse( asm ) );
            }

            public void AddMethod( String identifier, ValType[] paramTypes, ValType returnType, bool inline, DASM16Builder method )
            {
                if ( inline )
                    myMethods.Add( new MethodInfo( identifier, paramTypes, returnType ), method );
                else
                {
                    String label = "__" + Identifier + "__" + identifier;
                    foreach ( ValType type in paramTypes )
                        label += "__" + type.Identifier;
                    stStaticASM.AddLabel( label, false );
                    stStaticASM.AddChild( method );
                    AddMethod( identifier, paramTypes, returnType, true, "JSR " + label );
                }
            }

            public virtual bool HasMethod( MethodInfo info )
            {
                return myMethods.Keys.FirstOrDefault( x => x.Equals( info ) ) != null;
            }

            public virtual bool HasMethod( String identifier, ValType[] paramTypes )
            {
                return HasMethod( new MethodInfo( identifier, paramTypes, null ) );
            }

            public virtual DASM16Builder GetMethod( MethodInfo info )
            {
                return myMethods.First( x => x.Key.Equals( info ) ).Value;
            }

            public virtual DASM16Builder GetMethod( String identifier, ValType[] paramTypes )
            {
                return GetMethod( new MethodInfo( identifier, paramTypes, null ) );
            }
        }

        private class LitType : ValType
        {
            private static Dictionary<String, LitType> stTypes;

            static LitType()
            {
                stTypes = new Dictionary<string, LitType>();

                LitType tword = new LitType( "u16", 1, null,
                    delegate( String str )
                    {
                        int val = 0;
                        Match match;
                        if ( ( match = Regex.Match( str, "(0d)?[0-9]+" ) ).Success
                            && match.Index == 0 )
                        {
                            String sub = match.Value;
                            if ( sub.Length > 2 && sub[ 1 ] == 'd' )
                                sub = sub.Substring( 2 );

                            val = int.Parse( sub );
                        }
                        else if ( ( match = Regex.Match( str, "0b(0|1)+" ) ).Success
                            && match.Index == 0 )
                        {
                            val = int.Parse( match.Value );
                        }
                        else if ( ( match = Regex.Match( str, "0x[0-9a-f]+" ) ).Success
                            && match.Index == 0 )
                        {
                            val = int.Parse( match.Value );
                        }
                        else
                            return -1;

                        if ( val < 0x10000 )
                            return match.Length;

                        return -1;
                    },
                    delegate( String literal )
                    {
                        if( literal.StartsWith( "'" ) )
                        {
                            char c = literal[ 1 ];
                            if( c == '\\' )
                            {
                                c = literal[ 2 ];
                                switch( c )
                                {
                                    case 'r':
                                        c = '\r'; break;
                                    case 'n':
                                        c = '\n'; break;
                                    case 't':
                                        c = '\t'; break;
                                }
                            }
                            return new ushort[] { (ushort) c };
                        }

                        if( literal.Length > 2 && char.IsLetter( literal[ 1 ] ) )
                        {
                            int b = 10;
                            switch( literal[ 1 ] )
                            {
                                case 'b':
                                    b = 2; break;
                                case 'd':
                                    b = 10; break;
                                case 'x':
                                    b = 16; break;
                            }
                            return new ushort[] { Convert.ToUInt16( literal.Substring( 2 ), b ) };
                        }

                        return new ushort[] { ushort.Parse( literal ) };
                    } );
                stTypes.Add( tword.Identifier, tword );
                LitType tbool = new LitType( "bool", 1, null,
                    delegate( String str )
                    {
                        Match match;
                        if ( ( match = Regex.Match( str, "true[^a-zA-Z0-9_]" ) ).Success
                            && match.Index == 0 )
                            return 4;
                        else if ( ( match = Regex.Match( str, "false[^a-zA-Z0-9_]" ) ).Success
                            && match.Index == 0 )
                            return 5;

                        return -1;
                    },
                    delegate( String literal )
                    {
                        if ( literal == "true" )
                            return new ushort[] { 1 };
                        return new ushort[] { 0 };
                    } );
                stTypes.Add( tbool.Identifier, tbool );

                tword.AddMethod( "__ctor", new ValType[] { tword }, tword, true, "" );
                tword.AddMethod( "__ctor", new ValType[] { tbool }, tword, true, "" );
                tword.AddMethod( "__not", new ValType[] { tword }, tword, true, @"
                    XOR A, 0xffff
                " );
                tword.AddMethod( "__add", new ValType[] { tword, tword }, tword, true, @"
                    ADD A, B
                " );
                tword.AddMethod( "__sub", new ValType[] { tword, tword }, tword, true, @"
                    SUB A, B
                " );
                tword.AddMethod( "__mul", new ValType[] { tword, tword }, tword, true, @"
                    MUL A, B
                " );
                tword.AddMethod( "__div", new ValType[] { tword, tword }, tword, true, @"
                    DIV A, B
                " );
                tword.AddMethod( "__mod", new ValType[] { tword, tword }, tword, true, @"
                    MOD A, B
                " );
                tword.AddMethod( "__shl", new ValType[] { tword, tword }, tword, true, @"
                    SHL A, B
                " );
                tword.AddMethod( "__shr", new ValType[] { tword, tword }, tword, true, @"
                    SHR A, B
                " );
                tword.AddMethod( "__and", new ValType[] { tword, tword }, tword, true, @"
                    AND A, B
                " );
                tword.AddMethod( "__bor", new ValType[] { tword, tword }, tword, true, @"
                    BOR A, B
                " );
                tword.AddMethod( "__xor", new ValType[] { tword, tword }, tword, true, @"
                    XOR A, B
                " );
                tword.AddMethod( "__equ", new ValType[] { tword, tword }, tbool, false, @"
                    SET C, A
                    SET A, 0
                    IFE B, C
                        SET A, 1
                    SET PC, POP
                " );
                tword.AddMethod( "__neq", new ValType[] { tword, tword }, tbool, false, @"
                    SET C, A
                    SET A, 0
                    IFN B, C
                        SET A, 1
                    SET PC, POP
                " );
                tword.AddMethod( "__grt", new ValType[] { tword, tword }, tbool, false, @"
                    SET C, A
                    SET A, 0
                    IFG B, C
                        SET A, 1
                    SET PC, POP
                " );
                tword.AddMethod( "__gre", new ValType[] { tword, tword }, tbool, false, @"
                    SET C, A
                    SET A, 1
                    IFG C, B
                        SET A, 0
                    SET PC, POP
                " );
                tword.AddMethod( "__lst", new ValType[] { tword, tword }, tbool, false, @"
                    SET C, A
                    SET A, 0
                    IFG C, B
                        SET A, 1
                    SET PC, POP
                " );
                tword.AddMethod( "__lse", new ValType[] { tword, tword }, tbool, false, @"
                    SET C, A
                    SET A, 1
                    IFG B, C
                        SET A, 0
                    SET PC, POP
                " );

                tbool.AddMethod( "__ctor", new ValType[] { tbool }, tbool, true, "" );
                tbool.AddMethod( "__ctor", new ValType[] { tword }, tbool, false, @"
                    SET B, A
                    SET A, 0
                    IFG B, 0
                        SET A, 1
                    SET PC, POP
                " );
                tbool.AddMethod( "__not", new ValType[] { tbool }, tbool, true, @"
                    XOR A, 1
                " );
                tbool.AddMethod( "__and", new ValType[] { tbool, tbool }, tbool, true, @"
                    AND A, B
                " );
                tbool.AddMethod( "__bor", new ValType[] { tbool, tbool }, tbool, true, @"
                    BOR A, B
                " );
                tbool.AddMethod( "__xor", new ValType[] { tbool, tbool }, tbool, true, @"
                    XOR A, B
                " );
                tbool.AddMethod( "__equ", new ValType[] { tbool, tbool }, tbool, false, @"
                    SET C, A
                    SET A, 0
                    IFE B, C
                        SET A, 1
                    SET PC, POP
                " );
                tbool.AddMethod( "__neq", new ValType[] { tbool, tbool }, tbool, false, @"
                    SET C, A
                    SET A, 0
                    IFN B, C
                        SET A, 1
                    SET PC, POP
                " );
            }

            public static LitType[] GetAll()
            {
                return stTypes.Values.ToArray();
            }

            public readonly ushort[] Default;

            public readonly Func<String, int> MatchLiteral;
            public readonly Func<String, ushort[]> Construct;

            protected LitType( String identifier, int size, ushort[] defaultValue, Func<String, int> matchLiteral, Func<String, ushort[]> construct )
                : base( identifier, size )
            {
                if ( defaultValue == null )
                    Default = new ushort[ size ];
                else
                    Default = defaultValue;

                MatchLiteral = matchLiteral;
                Construct = construct;
            }
        }

        private struct VariableInfo
        {
            public readonly ushort Offset;
            public readonly int Size;
            public readonly String Name;
            public readonly ValType Type;
            public readonly int Length;
            public readonly bool Reference;

            public VariableInfo( String name, ValType type, bool reference, int arrayLength = 1 )
            {
                Offset = 0xffff;
                Name = name;
                Type = type;
                Length = 1;
                Reference = reference;
                Size = ( Reference ? 1 : Type.Size * Length + 1 );
            }

            public VariableInfo( ushort offset, VariableInfo info )
            {
                Offset = offset;
                Name = info.Name;
                Type = info.Type;
                Length = info.Length;
                Reference = info.Reference;
                Size = info.Size;
            }
        }

        private class ObjType : ValType
        {
            private static Dictionary<String, ObjType> stTypes;

            public static void ClearTypes()
            {
                stTypes = new Dictionary<String, ObjType>();
            }

            public static ObjType[] GetAll()
            {
                return stTypes.Values.ToArray();
            }

            public static void Register( ObjType type )
            {
                stTypes.Add( type.Identifier, type );
            }

            public readonly ObjType BaseType;
            public readonly bool HasBase;

            private Dictionary<String, VariableInfo> myMembers;

            protected ObjType( String identifier, ICollection<VariableInfo> members, ObjType baseType = null )
                : base( identifier, members.Sum( x => x.Size ) + ( baseType != null ? baseType.Size : 0 ) )
            {
                BaseType = baseType;
                HasBase = baseType != null;
                ushort offset = (ushort) ( HasBase ? BaseType.Size : 0 );
                foreach( VariableInfo member in members )
                    myMembers.Add( member.Name, new VariableInfo( offset, member ) );
            }

            public bool HasMember( String identifier )
            {
                if( myMembers.ContainsKey( identifier ) )
                    return true;

                if( HasBase )
                    return BaseType.HasMember( identifier );

                return false;
            }

            public VariableInfo GetMemberInfo( String identifier )
            {
                if( myMembers.ContainsKey( identifier ) )
                    return myMembers[ identifier ];

                if( HasBase )
                    return BaseType.GetMemberInfo( identifier );

                throw new KeyNotFoundException( "Member \"" + identifier + "\" not found in object of type \"" + Identifier + "\"" );
            }
        }

        private class Scope
        {
            public readonly Scope Parent;
            public readonly bool Root;

            public int Count { get; private set; }

            private Dictionary<String, VariableInfo> myMembers;

            public Scope()
            {
                Parent = null;
                Root = true;

                Count = 0;

                myMembers = new Dictionary<String,VariableInfo>();
            }

            public Scope( Scope parent )
            {
                Parent = parent;
                Root = false;

                Count = 0;

                myMembers = new Dictionary<String, VariableInfo>();
            }

            public void AddMember( VariableInfo info )
            {
                myMembers.Add( info.Name, new VariableInfo( (ushort) ( ( Root ? 0 : Parent.Count ) + Count ), info ) );
                ++Count;
            }

            public Scope Pop()
            {
                return Parent;
            }

            public Scope Push()
            {
                return new Scope( this );
            }

            public bool HasMember( String identifier )
            {
                if( myMembers.ContainsKey( identifier ) )
                    return true;

                if( !Root )
                    return Parent.HasMember( identifier );

                return false;
            }

            public VariableInfo GetMember( String identifier )
            {
                if( myMembers.ContainsKey( identifier ) )
                    return myMembers[ identifier ];

                if( !Root )
                    return Parent.GetMember( identifier );

                throw new KeyNotFoundException( "Variable \"" + identifier + "\" has not been declared" );
            }
        }

        public static Assembly Compile( String str )
        {
            ObjType.ClearTypes();

            ReadClasses( str );

            return null;
        }

        private static void ReadClasses( String str )
        {
            int offset = 0;
            while ( offset < str.Length )
            {
                SkipWhitespace( str, ref offset );
                if ( IsNextClass( str, offset ) )
                    ObjType.Register( ReadClass( str, ref offset ) );
            }
        }

        private static bool IsNextClass( String str, int offset )
        {
            return str.Length - offset >= 7 && str.Substring( offset, 5 ) == "class";
        }

        private static ObjType ReadClass( String str, ref int offset )
        {
            offset += 5;
            SkipWhitespace( str, ref offset );

            if ( str[ offset ] == ':' )
            {
                if ( ++offset >= str.Length )
                    throw new EOFException( str );

            }

            throw new NotImplementedException();
        }

        private static void SkipWhitespace( String str, ref int offset )
        {
            bool lineComment = false;
            bool longComment = false;
            while( offset < str.Length )
            {
                if( !longComment && !lineComment && ( str[ offset ] == '/' && offset + 1 < str.Length ) )
                {
                    if( str[ offset + 1 ] == '/' )
                        lineComment = true;
                    else if( str[ offset + 1 ] == '*' )
                        longComment = true;
                    else
                        return;

                    ++offset;
                }
                else if( longComment && str[ offset ] == '*' && offset + 1 < str.Length && str[ offset + 1 ] == '/' )
                {
                    longComment = false;
                    ++offset;
                }
                else if( lineComment && str[ offset ] == '\n' )
                    lineComment = false;
                else if( !char.IsWhiteSpace( str[ offset ] ) )
                    return;

                ++offset;
            }
        }

        private static String ReadIdentifier( String str, ref int offset )
        {
            SkipWhitespace( str, ref offset );

            int i = offset;
            char c;
            while ( offset < str.Length && ( char.IsLetter( c = str[ offset ] ) || c == '_' || ( offset > i && char.IsDigit( c ) ) ) )
                ++offset;

            return str.Substring( i, offset - i );
        }

        private static ValType GetTypeFromIdentifier( String identifier )
        {
            foreach ( LitType type in LitType.GetAll() )
                if ( type.Identifier == identifier )
                    return type;

            foreach ( ObjType type in ObjType.GetAll() )
                if ( type.Identifier == identifier )
                    return type;

            return null;
        }
    }
}
