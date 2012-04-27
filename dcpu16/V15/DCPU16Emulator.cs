using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DCPU16.V15
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
        MDI = 0x09,
        AND = 0x0a,
        BOR = 0x0b,
        XOR = 0x0c,
        SHR = 0x0d,
        ASR = 0x0e,
        SHL = 0x0f,

        IFB = 0x10,
        IFC = 0x11,
        IFE = 0x12,
        IFN = 0x13,
        IFG = 0x14,
        IFA = 0x15,
        IFL = 0x16,
        IFU = 0x17,

        ADX = 0x1a,
        SBX = 0x1b,

        STI = 0x1e,
        STD = 0x1f,

        JSR = 0x01 | 0x20,

        HCF = 0x07 | 0x20,

        INT = 0x08 | 0x20,
        IAG = 0x09 | 0x20,
        IAS = 0x0a | 0x20,
        IAP = 0x0b | 0x20,
        IAQ = 0x0c | 0x20,

        HWN = 0x10 | 0x20,
        HWQ = 0x11 | 0x20,
        HWI = 0x12 | 0x20
    }

    public class RegisterChangedEventArgs : EventArgs
    {
        public readonly DCPU16Register Register;
        public readonly ushort Value;

        public RegisterChangedEventArgs( DCPU16Register register, ushort value )
        {
            Register = register;
            Value = value;
        }
    }

    public class MemoryChangedEventArgs : EventArgs
    {
        public readonly int Location;
        public readonly ushort Value;

        public MemoryChangedEventArgs( int location, ushort value )
        {
            Location = location;
            Value = value;
        }
    }

    public class DCPU16Emulator
    {
        public const int RamSizeWords = 0x10000;

        private ushort[] myMemory;
        private ushort[] myRegisters;

        private Queue<ushort> myInterruptQueue;
        private List<IHardware> myHardware;

        private Random myRandom;

        public bool Exited { get; private set; }
        public bool OnFire { get; private set; }
        public ushort PC { get; private set; }
        public ushort SP { get; private set; }
        public ushort EX { get; private set; }
        public ushort IA { get; private set; }

        public ushort A
        {
            get { return GetRegister( DCPU16Register.A ); }
            private set { SetRegister( DCPU16Register.A, value ); }
        }
        public ushort B
        {
            get { return GetRegister( DCPU16Register.B ); }
            private set { SetRegister( DCPU16Register.B, value ); }
        }
        public ushort C
        {
            get { return GetRegister( DCPU16Register.C ); }
            private set { SetRegister( DCPU16Register.C, value ); }
        }
        public ushort X
        {
            get { return GetRegister( DCPU16Register.X ); }
            private set { SetRegister( DCPU16Register.X, value ); }
        }
        public ushort Y
        {
            get { return GetRegister( DCPU16Register.Y ); }
            private set { SetRegister( DCPU16Register.Y, value ); }
        }
        public ushort Z
        {
            get { return GetRegister( DCPU16Register.Z ); }
            private set { SetRegister( DCPU16Register.Z, value ); }
        }
        public ushort I
        {
            get { return GetRegister( DCPU16Register.I ); }
            private set { SetRegister( DCPU16Register.I, value ); }
        }
        public ushort J
        {
            get { return GetRegister( DCPU16Register.J ); }
            private set { SetRegister( DCPU16Register.J, value ); }
        }

        public ushort HardwareCount
        {
            get { return (ushort) myHardware.Count; }
        }

        public event EventHandler<RegisterChangedEventArgs> RegisterChanged;
        public event EventHandler<MemoryChangedEventArgs> MemoryChanged;

        public bool QueueInterrupts { get; private set; }

        public DCPU16Emulator()
        {
            myMemory = new ushort[ RamSizeWords ];
            myInterruptQueue = new Queue<ushort>();
            myHardware = new List<IHardware>();
            myRandom = new Random();

            ResetState();
        }

        public void ConnectHardware( IHardware device )
        {
            if( myHardware.Count < 0xffff )
                myHardware.Add( device );
        }

        public void ResetState()
        {
            myRegisters = new ushort[ 8 ];
            myInterruptQueue.Clear();

            Exited = false;
            OnFire = false;

            PC = 0;
            SP = 0;
            EX = 0;
            IA = 0;

            QueueInterrupts = false;
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

            if ( RegisterChanged != null )
                RegisterChanged( this, new RegisterChangedEventArgs( register, value ) );
        }

        public ushort GetMemory( int location )
        {
            return myMemory[ location & 0xffff ];
        }

        public void SetMemory( int location, ushort value )
        {
            location &= 0xffff;
            myMemory[ location ] = value;

            if ( MemoryChanged != null )
                MemoryChanged( this, new MemoryChangedEventArgs( location, value ) );
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
            if ( OnFire )
            {
                ushort loc = (ushort) myRandom.Next( 0x10000 );
                ushort bit = (ushort) ( 1 << myRandom.Next( 0x10 ) );
                SetMemory( loc, (ushort) ( GetMemory( loc ) ^ bit ) );
            }

            if ( Exited )
                return 1;

            ushort word = GetMemory( PC++ );
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
                    EX = (ushort) ( (int) bval + aval >= 0x10000 ? 0x1 : 0x0 );
                    break;
                case DCPU16Opcode.SUB:
                    cycles += 2;
                    StoreValue( bbits, (ushort) ( ( 0x10000 + bval - aval ) & 0xffff ), ref cycles );
                    EX = (ushort) ( (int) bval - aval < 0 ? 0xffff : 0x0 );
                    break;
                case DCPU16Opcode.MUL:
                    cycles += 2;
                    StoreValue( bbits, (ushort) ( ( (uint) ( bval * aval ) ) & 0xffff ), ref cycles );
                    EX = (ushort) ( ( ( (uint) ( bval * aval ) ) >> 0x10 ) & 0xffff );
                    break;
                case DCPU16Opcode.MLI:
                    cycles += 2;
                    StoreValue( bbits, (ushort) ( ( (uint) ( (short) bval * (short) aval ) ) & 0xffff ), ref cycles );
                    EX = (ushort) ( ( (uint) ( (short) bval * (short) aval ) >> 0x10 ) & 0xffff );
                    break;
                case DCPU16Opcode.DIV:
                    cycles += 3;
                    if ( aval == 0x0000 )
                        StoreValue( bbits, 0x0000, ref cycles );
                    else
                    {
                        StoreValue( bbits, (ushort) ( ( (uint) ( bval / aval ) ) & 0xffff ), ref cycles );
                        EX = (ushort) ( ( ( (uint) bval << 0x10 ) / aval ) & 0xffff );
                    }
                    break;
                case DCPU16Opcode.DVI:
                    cycles += 3;
                    if ( aval == 0x0000 )
                        StoreValue( bbits, 0x0000, ref cycles );
                    else
                    {
                        StoreValue( bbits, (ushort) ( ( (uint) ( bval / aval ) ) & 0xffff ), ref cycles );
                        EX = (ushort) ( ( ( (uint) bval << 0x10 ) / aval ) & 0xffff );
                    }
                    break;
                case DCPU16Opcode.MOD:
                    cycles += 3;
                    if ( aval == 0x0000 )
                        StoreValue( bbits, 0x0000, ref cycles );
                    else
                        StoreValue( bbits, (ushort) ( bval % aval ), ref cycles );
                    break;
                case DCPU16Opcode.MDI:
                    cycles += 3;
                    if ( aval == 0x0000 )
                        StoreValue( bbits, 0x0000, ref cycles );
                    else
                        StoreValue( bbits, (ushort) ( (short) bval % (short) aval ), ref cycles );
                    break;
                case DCPU16Opcode.AND:
                    cycles += 1;
                    StoreValue( bbits, (ushort) ( bval & aval ), ref cycles );
                    break;
                case DCPU16Opcode.BOR:
                    cycles += 1;
                    StoreValue( bbits, (ushort) ( bval | aval ), ref cycles );
                    break;
                case DCPU16Opcode.XOR:
                    cycles += 1;
                    StoreValue( bbits, (ushort) ( bval ^ aval ), ref cycles );
                    break;
                case DCPU16Opcode.SHR:
                    cycles += 2;
                    StoreValue( bbits, (ushort) ( bval >> aval ), ref cycles );
                    EX = (ushort) ( (uint) ( ( bval << 0x10 ) >> aval ) & 0xffff );
                    break;
                case DCPU16Opcode.ASR:
                    cycles += 2;
                    StoreValue( bbits, (ushort) ( (int) bval >> aval ), ref cycles );
                    EX = (ushort) ( (uint) ( (int) ( bval << 0x10 ) >> aval ) & 0xffff );
                    break;
                case DCPU16Opcode.SHL:
                    cycles += 2;
                    StoreValue( bbits, (ushort) ( bval << aval ), ref cycles );
                    EX = (ushort) ( (uint) ( (uint) ( bval << aval ) >> 0x10 ) & 0xffff );
                    break;
                case DCPU16Opcode.IFB:
                    cycles += 2;
                    if ( ( aval & bval ) == 0x0 ) cycles += Skip();
                    break;
                case DCPU16Opcode.IFC:
                    cycles += 2;
                    if ( ( aval & bval ) != 0x0 ) cycles += Skip();
                    break;
                case DCPU16Opcode.IFE:
                    cycles += 2;
                    if ( aval != bval ) cycles += Skip();
                    break;
                case DCPU16Opcode.IFN:
                    cycles += 2;
                    if ( aval == bval ) cycles += Skip();
                    break;
                case DCPU16Opcode.IFG:
                    cycles += 2;
                    if ( aval >= bval ) cycles += Skip();
                    break;
                case DCPU16Opcode.IFA:
                    cycles += 2;
                    if ( (short) aval >= (short) bval ) cycles += Skip();
                    break;
                case DCPU16Opcode.IFL:
                    cycles += 2;
                    if ( aval <= bval ) cycles += Skip();
                    break;
                case DCPU16Opcode.IFU:
                    cycles += 2;
                    if ( (short) aval <= (short) bval ) cycles += Skip();
                    break;
                case DCPU16Opcode.ADX:
                    cycles += 3;
                    StoreValue( bbits, (ushort) ( bval + aval + EX ), ref cycles );
                    EX = (ushort) ( (int) bval + aval + (short) EX >= 0x10000 ? 0x1 : 0x0 );
                    break;
                case DCPU16Opcode.SBX:
                    cycles += 3;
                    StoreValue( bbits, (ushort) ( bval - aval + EX ), ref cycles );
                    EX = (ushort) ( (int) bval - aval + (short) EX < 0x0 ? 0xffff : 0x0 );
                    break;
                case DCPU16Opcode.STI:
                    cycles += 2;
                    StoreValue( bbits, aval, ref cycles );
                    I += 1;
                    J += 1;
                    break;
                case DCPU16Opcode.STD:
                    cycles += 2;
                    StoreValue( bbits, aval, ref cycles );
                    I -= 1;
                    J -= 1;
                    break;

                case DCPU16Opcode.JSR:
                    cycles += 3;
                    Push( PC );
                    PC = aval;
                    break;
                case DCPU16Opcode.HCF:
                    cycles += 9;
                    --PC;
                    Exited = true;
                    OnFire = true;
                    break;
                case DCPU16Opcode.INT:
                    if ( Interrupt( aval ) )
                        cycles += 4;
                    else
                        cycles += 2;
                    break;
                case DCPU16Opcode.IAG:
                    cycles += 1;
                    StoreValue( abits, IA, ref cycles );
                    break;
                case DCPU16Opcode.IAS:
                    cycles += 1;
                    IA = aval;
                    break;
                case DCPU16Opcode.IAP:
                    cycles += 3;
                    if ( IA != 0x0000 )
                    {
                        Push( IA );
                        IA = aval;
                    }
                    break;
                case DCPU16Opcode.IAQ:
                    cycles += 3;
                    QueueInterrupts = aval != 0x0000;
                    break;
                case DCPU16Opcode.HWN:
                    cycles += 2;
                    StoreValue( abits, HardwareCount, ref cycles );
                    break;
                case DCPU16Opcode.HWQ:
                    cycles += 4;
                    if ( aval < HardwareCount )
                    {
                        IHardware device = myHardware[ aval ];
                        A = (ushort) ( device.HardwareID & 0xffff );
                        B = (ushort) ( ( device.HardwareID >> 0x10 ) & 0xffff );
                        C = device.HardwareVersion;
                        X = (ushort) ( device.Manufacturer & 0xffff );
                        Y = (ushort) ( ( device.Manufacturer >> 0x10 ) & 0xffff );
                    }
                    break;
                case DCPU16Opcode.HWI:
                    cycles += 4;
                    if ( aval < HardwareCount )
                        myHardware[ aval ].Interrupt( this );
                    break;
            }

            if ( myInterruptQueue.Count > 0 && !QueueInterrupts )
            {
                ushort message = myInterruptQueue.Dequeue();
                if ( IA != 0x0000 )
                {
                    Push( PC );
                    Push( GetRegister( DCPU16Register.A ) );
                    PC = IA;
                    SetRegister( DCPU16Register.A, message );
                }
            }

            return cycles;
        }

        public bool Interrupt( ushort message )
        {
            if ( IA == 0x0000 )
                return false;

            if ( myInterruptQueue.Count >= 256 )
            {
                OnFire = true;
                return false;
            }

            myInterruptQueue.Enqueue( message );
            return true;
        }

        private int Skip()
        {
            int cycles = 0;
            ushort word;
            do
            {
                word = GetMemory( PC );
                PC += (ushort) GetInstructionLength( word );
                word &= 0x1f;
                ++cycles;
            }
            while ( word >= 0x10 && word < 0x18 );
            return cycles;
        }

        private int GetInstructionLength( ushort word )
        {
            if ( ( word & 0x1f ) == 0x0 )
                return 1 + GetValueLength( (byte) ( word >> 0xa ) );

            return 1 + GetValueLength( (byte) ( word >> 0xa ) )
                + GetValueLength( (byte) ( ( word >> 0x5 ) & 0x1f ) );
        }

        private int GetValueLength( byte identifier )
        {
            return ( identifier >= 0x10 && identifier < 0x18 )
                || identifier == 0x1e || identifier == 0x1f ? 1 : 0;
        }

        private ushort LoadValue( byte identifier, bool a, ref int cycles )
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
                        val += GetMemory( PC++ );
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
                    return Pick( GetMemory( PC++ ) );
                case (byte) DCPU16SpecialRegister.SP:
                    return SP;
                case (byte) DCPU16SpecialRegister.PC:
                    return PC;
                case (byte) DCPU16SpecialRegister.EX:
                    return EX;
                case 0x1e:
                    ++cycles;
                    return GetMemory( GetMemory( PC++ ) );
                case 0x1f:
                    ++cycles;
                    return GetMemory( PC++ );
                default:
                    return 0x0000;
            }
        }

        private void StoreValue( int identifier, ushort value, ref int cycles )
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
                    SetMemory( GetRegister( register ) + GetMemory( PC - 1 ), value );
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
                        Pick( GetMemory( PC - 1 ), value );
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
                        SetMemory( GetMemory( PC - 1 ), value );
                        break;
                    case 0x1f:
                        ++cycles;
                        break;
                }
            }
        }
    }
}
