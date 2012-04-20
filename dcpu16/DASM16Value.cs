using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DCPU16
{
    public abstract class DASM16Value
    {
        public readonly int Line;
        public readonly int Column;

        public bool Reference { get; private set; }
        public bool Extended { get; protected set; }
        public ushort Assembled { get; protected set; }
        public ushort NextWord { get; protected set; }
        public String Disassembled { get; private set; }

        public DASM16Value( bool reference )
        {
            Line = -1;
            Column = -1;
            Reference = reference;
        }

        public DASM16Value( String str, int offset, bool reference )
        {
            DASM16Assembler.GetCharacterPosition( str, offset, out Line, out Column );
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

        public abstract void ResolveConstant( Dictionary<String, ushort> consts );
    }

    public class DASM16RegisterVal : DASM16Value
    {
        public static readonly DASM16RegisterVal A = new DASM16RegisterVal( DCPU16Register.A, false );
        public static readonly DASM16RegisterVal B = new DASM16RegisterVal( DCPU16Register.B, false );
        public static readonly DASM16RegisterVal C = new DASM16RegisterVal( DCPU16Register.C, false );
        public static readonly DASM16RegisterVal X = new DASM16RegisterVal( DCPU16Register.X, false );
        public static readonly DASM16RegisterVal Y = new DASM16RegisterVal( DCPU16Register.Y, false );
        public static readonly DASM16RegisterVal Z = new DASM16RegisterVal( DCPU16Register.Z, false );
        public static readonly DASM16RegisterVal I = new DASM16RegisterVal( DCPU16Register.I, false );
        public static readonly DASM16RegisterVal J = new DASM16RegisterVal( DCPU16Register.J, false );

        public static readonly DASM16RegisterVal ARef = new DASM16RegisterVal( DCPU16Register.A, true );
        public static readonly DASM16RegisterVal BRef = new DASM16RegisterVal( DCPU16Register.B, true );
        public static readonly DASM16RegisterVal CRef = new DASM16RegisterVal( DCPU16Register.C, true );
        public static readonly DASM16RegisterVal XRef = new DASM16RegisterVal( DCPU16Register.X, true );
        public static readonly DASM16RegisterVal YRef = new DASM16RegisterVal( DCPU16Register.Y, true );
        public static readonly DASM16RegisterVal ZRef = new DASM16RegisterVal( DCPU16Register.Z, true );
        public static readonly DASM16RegisterVal IRef = new DASM16RegisterVal( DCPU16Register.I, true );
        public static readonly DASM16RegisterVal JRef = new DASM16RegisterVal( DCPU16Register.J, true );

        public readonly DCPU16Register Register;
        public readonly String Label;
        public ushort Offset;

        public DASM16RegisterVal( DCPU16Register register, bool reference )
            : base( reference )
        {
            Register = register;
            Label = null;
            Offset = 0x0000;
            Extended = false;
            Assembled = (ushort) ( (ushort) Register | ( Reference ? 0x08 : 0x00 ) );
            SetDisassembled( Register.ToString() );
        }

        public DASM16RegisterVal( String str, int offset, DCPU16Register register, bool reference )
            : base( str, offset, reference )
        {
            Register = register;
            Label = null;
            Offset = 0x0000;
            Extended = false;
            Assembled = (ushort) ( (ushort) Register | ( Reference ? 0x08 : 0x00 ) );
            SetDisassembled( Register.ToString() );
        }

        public DASM16RegisterVal( DCPU16Register register, ushort addrOffset )
            : base( true )
        {
            Register = register;
            Label = null;
            Offset = addrOffset;
            NextWord = Offset;
            Extended = true;
            Assembled = (ushort) ( (ushort) Register | 0x10 );
            SetDisassembled( "0x" + Offset.ToString( "X4" ) + "+" + Register.ToString() );
        }

        public DASM16RegisterVal( String str, int offset, DCPU16Register register, ushort addrOffset )
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

        public DASM16RegisterVal( DCPU16Register register, String label )
            : base( true )
        {
            Register = register;
            Label = label;
            Offset = 0x0000;
            Extended = true;
        }

        public DASM16RegisterVal( String str, int offset, DCPU16Register register, String label )
            : base( str, offset, true )
        {
            Register = register;
            Label = label;
            Offset = 0x0000;
            Extended = true;
        }

        public override void ResolveConstant( Dictionary<String, ushort> consts )
        {
            if ( Label != null )
            {
                if ( consts.ContainsKey( Label ) )
                {
                    Offset = consts[ Label ];
                    NextWord = Offset;

                    Assembled = (ushort) ( (ushort) Register | ( Reference ? Extended ? 0x10 : 0x08 : 0x00 ) );
                    SetDisassembled( "0x" + Offset.ToString( "X4" ) + "+" + Register.ToString() );
                }
                else
                    throw new InvalidLabelException( Line, Column, Label );
            }
        }
    }

    public class DASM16SpecialVal : DASM16Value
    {
        public static readonly DASM16SpecialVal POP = new DASM16SpecialVal( DCPU16SpecialRegister.POP );
        public static readonly DASM16SpecialVal PEEK = new DASM16SpecialVal( DCPU16SpecialRegister.PEEK );
        public static readonly DASM16SpecialVal PUSH = new DASM16SpecialVal( DCPU16SpecialRegister.PUSH );
        public static readonly DASM16SpecialVal SP = new DASM16SpecialVal( DCPU16SpecialRegister.SP );
        public static readonly DASM16SpecialVal PC = new DASM16SpecialVal( DCPU16SpecialRegister.PC );
        public static readonly DASM16SpecialVal O = new DASM16SpecialVal( DCPU16SpecialRegister.O );

        public readonly DCPU16SpecialRegister Type;

        public DASM16SpecialVal( DCPU16SpecialRegister type )
            : base( false )
        {
            Extended = false;
            Type = type;
            Assembled = (ushort) Type;
            SetDisassembled( Type.ToString().ToUpper() );
        }

        public DASM16SpecialVal( String str, int offset, DCPU16SpecialRegister type )
            : base( str, offset, false )
        {
            Extended = false;
            Type = type;
            Assembled = (ushort) Type;
            SetDisassembled( Type.ToString().ToUpper() );
        }

        public override void ResolveConstant( Dictionary<String, ushort> consts )
        {
            return;
        }
    }

    public class DASM16LiteralVal : DASM16Value
    {
        public readonly String Label;
        public ushort Literal;

        public DASM16LiteralVal( char ch )
            : base( false )
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
                switch ( ch )
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

        public DASM16LiteralVal( String str, int offset, char ch )
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
                switch ( ch )
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

        public DASM16LiteralVal( ushort value, bool reference = false )
            : base( reference )
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

        public DASM16LiteralVal( String str, int offset, ushort value, bool reference )
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

        public DASM16LiteralVal( String label, bool reference = false )
            : base( reference )
        {
            Literal = 0x0000;
            Label = label;
            Extended = true;
            Assembled = (ushort) ( 0x1e | ( Reference ? 0x0 : 0x1 ) );
        }

        public DASM16LiteralVal( String str, int offset, String label, bool reference )
            : base( str, offset, reference )
        {
            Literal = 0x0000;
            Label = label;
            Extended = true;
            Assembled = (ushort) ( 0x1e | ( Reference ? 0x0 : 0x1 ) );
        }

        public override void ResolveConstant( Dictionary<String, ushort> consts )
        {
            if ( Label != null )
            {
                if ( consts.ContainsKey( Label ) )
                {
                    Literal = consts[ Label ];
                    SetDisassembled( "0x" + Literal.ToString( "X4" ) );
                    NextWord = Literal;
                }
                else
                    throw new InvalidLabelException( Line, Column, Label );
            }
        }
    }
}
