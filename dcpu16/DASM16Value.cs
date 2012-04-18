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

    public class RegisterVal : DASM16Value
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

    public class SpecialVal : DASM16Value
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

        public override void ResolveConstant( Dictionary<String, ushort> consts )
        {
            return;
        }
    }

    public class LiteralVal : DASM16Value
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
