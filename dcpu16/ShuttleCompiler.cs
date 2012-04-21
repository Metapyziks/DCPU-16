using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Reflection;

namespace DCPU16
{
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

                LitType tu16 = new LitType( "u16", 1, null,
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
                stTypes.Add( "u16", tu16 );
                LitType tbool = new LitType( "bool", 1, null,
                    delegate( String str )
                    {
                        Match match;
                        if ( ( match = Regex.Match( str, "true" ) ).Success
                            && match.Index == 0 )
                            return 4;
                        else if ( ( match = Regex.Match( str, "false" ) ).Success
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
                stTypes.Add( "bool", tbool );

                tu16.AddMethod( "__ctor", new ValType[] { tu16 }, tu16, true, "" );
                tu16.AddMethod( "__ctor", new ValType[] { tbool }, tu16, true, "" );
                tu16.AddMethod( "__not", new ValType[] { tu16 }, tu16, true, @"
                    XOR A, 0xffff
                " );
                tu16.AddMethod( "__add", new ValType[] { tu16, tu16 }, tu16, true, @"
                    ADD A, B
                " );
                tu16.AddMethod( "__sub", new ValType[] { tu16, tu16 }, tu16, true, @"
                    SUB A, B
                " );
                tu16.AddMethod( "__mul", new ValType[] { tu16, tu16 }, tu16, true, @"
                    MUL A, B
                " );
                tu16.AddMethod( "__div", new ValType[] { tu16, tu16 }, tu16, true, @"
                    DIV A, B
                " );
                tu16.AddMethod( "__mod", new ValType[] { tu16, tu16 }, tu16, true, @"
                    MOD A, B
                " );
                tu16.AddMethod( "__shl", new ValType[] { tu16, tu16 }, tu16, true, @"
                    SHL A, B
                " );
                tu16.AddMethod( "__shr", new ValType[] { tu16, tu16 }, tu16, true, @"
                    SHR A, B
                " );
                tu16.AddMethod( "__and", new ValType[] { tu16, tu16 }, tu16, true, @"
                    AND A, B
                " );
                tu16.AddMethod( "__bor", new ValType[] { tu16, tu16 }, tu16, true, @"
                    BOR A, B
                " );
                tu16.AddMethod( "__xor", new ValType[] { tu16, tu16 }, tu16, true, @"
                    XOR A, B
                " );
                tu16.AddMethod( "__equ", new ValType[] { tu16, tu16 }, tbool, false, @"
                    SET C, A
                    SET A, 0
                    IFE B, C
                        SET A, 1
                    SET PC, POP
                " );
                tu16.AddMethod( "__neq", new ValType[] { tu16, tu16 }, tbool, false, @"
                    SET C, A
                    SET A, 0
                    IFN B, C
                        SET A, 1
                    SET PC, POP
                " );
                tu16.AddMethod( "__grt", new ValType[] { tu16, tu16 }, tbool, false, @"
                    SET C, A
                    SET A, 0
                    IFG B, C
                        SET A, 1
                    SET PC, POP
                " );
                tu16.AddMethod( "__gre", new ValType[] { tu16, tu16 }, tbool, false, @"
                    SET C, A
                    SET A, 1
                    IFG C, B
                        SET A, 0
                    SET PC, POP
                " );
                tu16.AddMethod( "__lst", new ValType[] { tu16, tu16 }, tbool, false, @"
                    SET C, A
                    SET A, 0
                    IFG C, B
                        SET A, 1
                    SET PC, POP
                " );
                tu16.AddMethod( "__lse", new ValType[] { tu16, tu16 }, tbool, false, @"
                    SET C, A
                    SET A, 1
                    IFG B, C
                        SET A, 0
                    SET PC, POP
                " );

                tbool.AddMethod( "__ctor", new ValType[] { tbool }, tbool, true, "" );
                tbool.AddMethod( "__ctor", new ValType[] { tu16 }, tbool, false, @"
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

            public static ObjType[] GetAll()
            {
                if( stTypes == null )
                    return new ObjType[ 0 ];

                return stTypes.Values.ToArray();
            }

            public static void Register( ObjType type )
            {
                if( stTypes == null )
                    stTypes = new Dictionary<String,ObjType>();

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

            private Dictionary<String, VariableInfo> myMembers; 

            public Scope()
            {
                Parent = null;
                Root = true;

                myMembers = new Dictionary<String,VariableInfo>();
            }

            public Scope( Scope parent )
            {
                Parent = parent;
                Root = false;

                myMembers = new Dictionary<String,VariableInfo>();
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

        private abstract class Statement
        {
            public abstract String[] Compile( Scope scope );
        }

        public static String[] Compile( String source )
        {
            Scope scope = new Scope();

            return new String[ 0 ];
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
