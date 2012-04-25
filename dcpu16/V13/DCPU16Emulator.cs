using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DCPU16.V13
{
    public enum DCPU16Register : byte
    {
        A = 0x0, B = 0x1, C = 0x2,
        X = 0x3, Y = 0x4, Z = 0x5,
        I = 0x6, J = 0x7
    }

    public enum DCPU16SpecialRegister : byte
    {
        POP = 0x18, PUSH = POP,  PEEK = 0x19, PICK = 0x1a,
        SP  = 0x1b, PC   = 0x1c, EX   = 0x1d
    }

    public enum DCPU16Opcode : byte
    {
        SET = 0x01,
        ADD = 0x02,
        SUB = 0x03,
        MUL = 0x04, MLI = 0x05,
        DIV = 0x06, DVI = 0x07,
        MOD = 0x08,
        AND = 0x09,
        BOR = 0x0a,
        XOR = 0x0b,
        SHR = 0x0c,
        ASR = 0x0d,
        SHL = 0x0e,
        
        MVI = 0x0f,

        IFB = 0x10,
        IFC = 0x11,
        IFE = 0x12,
        IFN = 0x13,
        IFG = 0x14,
        IFA = 0x15,
        IFL = 0x16,
        IFU = 0x17,

        ADX = 0x1a,
        SUX = 0x1b,

        JSR = 0x01 | 0x20,

        INT = 0x08 | 0x20,
        IAG = 0x09 | 0x20,
        IAS = 0x0a | 0x20,

        HWN = 0x10 | 0x20,
        HWQ = 0x11 | 0x20,
        HWI = 0x12 | 0x20
    }

    public class DCPU16Emulator
    {
        public const int RamSizeWords = 0x10000;

        private ushort[] myMemory;
        private ushort[] myRegisters;
        private bool Exited { get; private set; }
        private ushort PC { get; private set; }
        private ushort SP { get; private set; }
        private ushort EX { get; private set; }
        private ushort IA { get; private set; }

        public DCPU16Emulator()
        {
            myMemory = new ushort[ RamSizeWords ];
            myRegisters = new ushort[ 8 ];

            ResetState();
        }

        public void ResetState()
        {
            PC = 0;
            SP = 0;
            EX = 0;
            IA = 0;
            Exited = false;
        }

        public void LoadProgram( ushort[] program, ushort offset = 0x0000 )
        {
            for ( int i = 0; i < program.Length; ++i )
                SetMemory( i + offset, program[ i ] );
        }

        public void LoadProgram( byte[] program, ushort offset = 0x0000 )
        {
            for ( int i = 0; i < program.Length; ++i )
            {
                int j = i / 2 + offset;
                int s = i % 2;

                if ( s == 0 )
                    SetMemory( j, (ushort) ( program[ i ] << 0x8 ) );
                else
                    SetMemory( j, (ushort) ( myMemory[ j ] | program[ i ] ) );
            }
        }

        public ushort GetRegister( DCPU16Register register )
        {
            return myRegisters[ (int) register ];
        }

        public void SetRegister( DCPU16Register register, ushort value )
        {
            myRegisters[ (int) register ] = value;
        }

        public ushort GetMemory( int location )
        {
            return myMemory[ location & 0xffff ];
        }

        public void SetMemory( int location, ushort value )
        {
            location &= 0xffff;
            myMemory[ location ] = value;
        }

        public ushort Pop()
        {
            return myMemory[ SP++ ];
        }

        public void Peek( ushort value )
        {
            SetMemory( SP, value );
        }

        public ushort Peek()
        {
            return myMemory[ SP ];
        }

        public void Push( ushort value )
        {
            SetMemory( --SP, value );
        }

        public void Pick( ushort n, ushort value )
        {
            SetMemory( SP + n, value );
        }

        public ushort Pick( ushort n )
        {
            return myMemory[ ( SP + n ) & 0xffff ];
        }

        public int Step()
        {
            ushort word = GetMemory( PC );
            if ( word == 0x0000 )
            {
                Exited = true;
                return 1;
            }

            int cycles = 0;

            byte opbits = (byte) ( word & 0x1f );
            byte abits = (byte) ( ( word >> 10 ) & 0x3f );
            byte bbits = (byte) ( ( word >> 5 ) & 0x1f );
            if ( opbits == 0x00 )
                opbits = (byte) ( bbits | 0x20 );

            ushort aval = LoadValue( abits, true, ref cycles );
            ushort bval = (ushort) ( ( opbits & 0x20 ) == 0 ? LoadValue( bbits, false, ref cycles ) : 0x0000 );
            
            switch ( (DCPU16Opcode) opbits )
            {
                case DCPU16Opcode.SET:
                    cycles += 1;
                    StoreValue( bbits, aval, ref cycles );
                    break;
                case DCPU16Opcode.ADD:
                    cycles += 2;
                    StoreValue( bbits, (ushort) ( bval + aval ), ref cycles );
                    break;
                case DCPU16Opcode.SUB:
                    cycles += 2;
                    StoreValue( bbits, (ushort) ( bval - aval ), ref cycles );
                    break;
                case DCPU16Opcode.MUL:
                    cycles += 2;
                    StoreValue( bbits, (ushort) ( bval * aval ), ref cycles );
                    break;
            }

            return cycles;
        }

        public ushort LoadValue( byte identifier, bool a, ref int cycles )
        {
            if ( identifier < 0x18 )
            {
                DCPU16Register reg = (DCPU16Register) ( identifier & 0x7 );
                ushort val = GetRegister( reg );

                if ( identifier >= 0x08 )
                {
                    if ( identifier >= 0x10 )
                    {
                        ++cycles;
                        val += GetMemory( ++PC );
                    }
                    val = GetMemory( val );
                }
                return val;
            }
            else if ( identifier >= 0x20 )
                return (ushort) ( 0xffff + ( identifier & 0x1f ) );

            switch ( identifier )
            {
                case (byte) DCPU16SpecialRegister.POP:
                    if ( a )
                        return Pop();
                    return 0x0000;
                case (byte) DCPU16SpecialRegister.PEEK:
                    return Peek();
                case (byte) DCPU16SpecialRegister.PICK:
                    ++cycles;
                    return Pick( GetMemory( ++PC ) );
                case (byte) DCPU16SpecialRegister.SP:
                    return SP;
                case (byte) DCPU16SpecialRegister.PC:
                    return PC;
                case (byte) DCPU16SpecialRegister.EX:
                    return EX;
                case 0x1e:
                    ++cycles;
                    return GetMemory( GetMemory( ++PC ) );
                case 0x1f:
                    ++cycles;
                    return GetMemory( ++PC );
                default:
                    return 0x0000;
            }
        }

        public void StoreValue( int identifier, ushort value, ref int cycles )
        {
            if ( identifier < 0x18 )
            {
                DCPU16Register register = (DCPU16Register) ( identifier & 0x7 );

                if ( identifier < 0x08 )
                    SetRegister( register, value );
                else if ( identifier < 0x10 )
                    SetMemory( GetRegister( register ), value );
                else
                {
                    ++cycles;
                    SetMemory( GetRegister( register ) + GetMemory( PC ), value );
                }
            }
            else
            {
                switch ( identifier )
                {
                    case (byte) DCPU16SpecialRegister.PUSH:
                        Push( value );
                        break;
                    case (byte) DCPU16SpecialRegister.PEEK:
                        Peek( value );
                        break;
                    case (byte) DCPU16SpecialRegister.PICK:
                        ++cycles;
                        Pick( GetMemory( PC ), value );
                        break;
                    case (byte) DCPU16SpecialRegister.SP:
                        SP = value;
                        break;
                    case (byte) DCPU16SpecialRegister.PC:
                        PC = value;
                        break;
                    case (byte) DCPU16SpecialRegister.EX:
                        EX = value;
                        break;
                    case 0x1e:
                        ++cycles;
                        SetMemory( GetMemory( PC ), value );
                        break;
                    case 0x1f:
                        ++cycles;
                        SetMemory( PC, value );
                        break;
                }
            }
        }
    }
}
