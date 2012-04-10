using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Reflection;

namespace DCPU16
{
    public static class DCPU16Compiler
    {
        private static readonly String stPreamble = @"
            
            
        ";

        private abstract class ValType
        {
            public readonly int Size;
            public readonly String Identifier;

            protected ValType( String identifier, int size )
            {
                Identifier = identifier;
                Size = size;
            }
        }

        private abstract class LitType : ValType
        {
            private static Dictionary<String, LitType> stTypes;

            public static LitType[] GetAll()
            {
                if( stTypes == null )
                    FindTypes();

                return stTypes.Values.ToArray();
            }

            private static void FindTypes()
            {
                Assembly asm = Assembly.GetExecutingAssembly();
                stTypes = new Dictionary<String,LitType>();
                foreach( Type type in asm.GetTypes() )
                {
                    if( type.BaseType == typeof( LitType ) )
                    {
                        ConstructorInfo cons = type.GetConstructor( new Type[ 0 ] );
                        if( cons != null )
                        {
                            LitType ltype = (LitType) cons.Invoke( new object[ 0 ] );
                            stTypes.Add( ltype.Identifier, ltype );
                        }
                    }
                }
            }

            public readonly ushort[] Default;

            protected LitType( String identifier, int size, ushort[] defaultValue = null )
                : base( identifier, size )
            {
                if( defaultValue == null )
                    Default = new ushort[ size ];
            }

            public abstract int MatchLiteral( String str );
            public abstract ushort[] Construct( String literal );
            public abstract ushort[] TypeCast( LitType type, ushort[] value );
        }

        private class U16Type : LitType
        {
            public U16Type()
                : base( "u16", 1 )
            {

            }

            public override int MatchLiteral( string str )
            {
                if( str.Length > 1 && str[ 0 ] == '+' )
                    str = str.Substring( 1 ).Trim();

                int val = 0;
                Match match;
                if( ( match = Regex.Match( str, "(0d)?[0-9]+" ) ).Success
                    && match.Index == 0 )
                {
                    String sub = match.Value;
                    if( sub.Length > 2 && sub[ 1 ] == 'd' )
                        sub = sub.Substring( 2 );

                    val = int.Parse( sub );
                }
                else if( ( match = Regex.Match( str, "0b(0|1)+" ) ).Success
                    && match.Index == 0 )
                {
                    val = int.Parse( match.Value );
                }
                else if( ( match = Regex.Match( str, "0x[0-9a-f]+" ) ).Success
                    && match.Index == 0 )
                {
                    val = int.Parse( match.Value );
                }
                else
                    return -1;
                
                if( val < 0x10000 )
                    return match.Length;

                return -1;
            }

            public override ushort[] Construct( String literal )
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
            }

            public override ushort[] TypeCast( LitType type, ushort[] value )
            {
                throw new NotImplementedException();
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
