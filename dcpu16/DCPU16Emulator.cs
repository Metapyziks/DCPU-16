﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DCPU16
{
    public enum Register : byte
    {
        A = 0x0, B = 0x1, C = 0x2,
        X = 0x3, Y = 0x4, Z = 0x5,
        I = 0x6, J = 0x7
    }

    public enum SpecialRegister : byte
    {
        POP = 0x18, PEEK = 0x19, PUSH = 0x1a,
        SP  = 0x1b, PC   = 0x1c, O    = 0x1d
    }

    public enum Opcode : byte
    {
        Dat = 0x0,
        Set = 0x1, 
        Add = 0x2, Sub = 0x3, Mul = 0x4, Div = 0x5,
        Mod = 0x6,
        ShL = 0x7, ShR = 0x8,
        And = 0x9, BOr = 0xa, XOr = 0xb,
        IfE = 0xc, IfN = 0xd, IfG = 0xe, IfB = 0xf,

        Jsr = 0x10
    }

    public class RegisterChangedEventArgs : EventArgs
    {
        public readonly Register Register;
        public readonly ushort Value;

        public RegisterChangedEventArgs( Register register, ushort value )
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
        private bool myExited;
        private ushort myPC;
        private ushort mySP;
        private ushort myOverflow;
        private bool mySkip;
        private ushort myCurPC;

        public event EventHandler<RegisterChangedEventArgs> RegisterChanged;
        public event EventHandler<MemoryChangedEventArgs> MemoryChanged;

        public DCPU16Emulator()
        {
            myMemory = new UInt16[ RamSizeWords ];
            myRegisters = new UInt16[ 8 ];

            ResetState();
        }

        public void ResetState()
        {
            myPC = 0;
            mySP = 0;
            myOverflow = 0x0000;
            mySkip = false;
            myExited = false;
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

        public ushort GetRegister( Register register )
        {
            return myRegisters[ (int) register ];
        }

        public void SetRegister( Register register, ushort value )
        {
            if ( !mySkip )
            {
                myRegisters[ (int) register ] = value;

                if ( RegisterChanged != null )
                    RegisterChanged( this, new RegisterChangedEventArgs( register, value ) );
            }
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
            return myMemory[ mySP++ ];
        }

        public void Pop( ushort value )
        {
            if ( !mySkip )
                SetMemory( mySP++, value );
        }

        public ushort Peek()
        {
            return myMemory[ mySP ];
        }

        public void Peek( ushort value )
        {
            if ( !mySkip )
                SetMemory( mySP, value );
        }

        public ushort Push()
        {
            return myMemory[ --mySP ];
        }

        public void Push( ushort value )
        {
            if ( !mySkip )
                SetMemory( --mySP, value );
        }

        public bool Exited
        {
            get { return myExited; }
            set { if ( !mySkip ) myExited = value; }
        }

        public ushort ProgramCounter
        {
            get { return myPC; }
            set { if ( !mySkip ) myPC = value; }
        }

        public ushort StackPointer
        {
            get { return mySP; }
            set { if ( !mySkip ) mySP = value; }
        }

        public ushort Overflow
        {
            get { return myOverflow; }
            set { if ( !mySkip ) myOverflow = value; }
        }

        public bool InstructionSkip
        {
            get { return mySkip; }
            set { if ( !mySkip ) mySkip = value; }
        }

        public int Step()
        {
            bool skip = mySkip;
            ushort word = myMemory[ myCurPC = myPC++ ];

            if ( word == 0x0000 )
            {
                Exited = true;
                myPC = myCurPC;
                return 1;
            }

            int cycles = 0;

            byte opcode = (byte) ( word & 0xf );
            byte a = (byte) ( ( word >> 4 ) & 0x3f );
            byte b = (byte) ( ( word >> 10 ) & 0x3f );
            if ( opcode == 0x0 )
            {
                ushort valA;
                opcode = a;
                a = b;
                switch( (Opcode) ( opcode << 0x4 ) )
                {
                    case Opcode.Jsr:
                        valA = LoadValue( a, ref cycles );
                        Push( (ushort) myPC );
                        cycles += 2;
                        ProgramCounter = valA;
                        break;
                    default:
                        break;
                }
            }
            else
            {
                ushort valA, valB;
                long val;
                switch ( (Opcode) opcode )
                {
                    case Opcode.Set:
                        valA = LoadValue( a, ref cycles );
                        valB = LoadValue( b, ref cycles );
                        if( !skip )
                            StoreValue( a, valB, ref cycles );
                        cycles += 1;
                        break;
                    case Opcode.Add:
                        val = LoadValue( a, ref cycles ) + LoadValue( b, ref cycles );
                        Overflow = (ushort) ( val > 0xffff ? 0x0001 : 0x0000 );
                        if ( !skip )
                            StoreValue( a, (ushort) ( val & 0xffff ), ref cycles );
                        cycles += 2;
                        break;
                    case Opcode.Sub:
                        valA = LoadValue( a, ref cycles );
                        valB = LoadValue( b, ref cycles );
                        if ( !skip )
                        {
                            if ( valA >= valB )
                            {
                                val = valA - valB;
                                Overflow = 0x0000;
                            }
                            else
                            {
                                val = 0x10000 + ( valA - valB );
                                Overflow = 0xffff;
                            }
                            StoreValue( a, (ushort) val, ref cycles );
                        }
                        cycles += 2;
                        break;
                    case Opcode.Mul:
                        valA = LoadValue( a, ref cycles );
                        valB = LoadValue( b, ref cycles );
                        if ( !skip )
                        {
                            val = valA * valB;
                            Overflow = (ushort) ( ( val >> 0x10 ) & 0xffff );
                            StoreValue( a, (ushort) ( val & 0xffff ), ref cycles );
                        }
                        cycles += 2;
                        break;
                    case Opcode.Div:
                        valA = LoadValue( a, ref cycles );
                        valB = LoadValue( b, ref cycles );
                        if ( !skip )
                        {
                            if ( valB == 0 )
                            {
                                val = 0;
                                Overflow = 0x0000;
                            }
                            else
                            {
                                val = valA / valB;
                                Overflow = (ushort) ( ( ( valA << 0x10 ) / valB ) & 0xffff );
                            }
                            StoreValue( a, (ushort) ( val & 0xffff ), ref cycles );
                        }
                        cycles += 3;
                        break;
                    case Opcode.Mod:
                        valA = LoadValue( a, ref cycles );
                        valB = LoadValue( b, ref cycles );
                        if ( !skip )
                        {
                            if ( valB == 0 )
                                val = 0;
                            else
                                val = valA % valB;
                            StoreValue( a, (ushort) ( val & 0xffff ), ref cycles );
                        }
                        cycles += 3;
                        break;
                    case Opcode.ShL:
                        valA = LoadValue( a, ref cycles );
                        valB = (ushort) ( LoadValue( b, ref cycles ) & 0x1f );
                        if ( !skip )
                        {
                            val = valA << valB;
                            Overflow = (ushort) ( ( val >> 0x10 ) & 0xffff );
                            StoreValue( a, (ushort) ( val & 0xffff ), ref cycles );
                        }
                        cycles += 2;
                        break;
                    case Opcode.ShR:
                        valA = LoadValue( a, ref cycles );
                        valB = (ushort) ( LoadValue( b, ref cycles ) & 0x1f );
                        if ( !skip )
                        {
                            val = ( valA << 0x10 ) >> valB;
                            Overflow = (ushort) ( val & 0xffff );
                            StoreValue( a, (ushort) ( ( val >> 0x10 ) & 0xffff ), ref cycles );
                        }
                        cycles += 2;
                        break;
                    case Opcode.And:
                        valA = LoadValue( a, ref cycles );
                        valB = LoadValue( b, ref cycles );
                        if ( !skip )
                            StoreValue( a, (ushort) ( valA & valB ), ref cycles );
                        cycles += 1;
                        break;
                    case Opcode.BOr:
                        valA = LoadValue( a, ref cycles );
                        valB = LoadValue( b, ref cycles );
                        if ( !skip )
                            StoreValue( a, (ushort) ( valA | valB ), ref cycles );
                        cycles += 1;
                        break;
                    case Opcode.XOr:
                        valA = LoadValue( a, ref cycles );
                        valB = LoadValue( b, ref cycles );
                        if ( !skip )
                            StoreValue( a, (ushort) ( valA ^ valB ), ref cycles );
                        cycles += 1;
                        break;
                    case Opcode.IfE:
                        valA = LoadValue( a, ref cycles );
                        valB = LoadValue( b, ref cycles );
                        InstructionSkip = valA != valB;
                        cycles += 2;
                        break;
                    case Opcode.IfN:
                        valA = LoadValue( a, ref cycles );
                        valB = LoadValue( b, ref cycles );
                        InstructionSkip = valA == valB;
                        cycles += 2;
                        break;
                    case Opcode.IfG:
                        valA = LoadValue( a, ref cycles );
                        valB = LoadValue( b, ref cycles );
                        InstructionSkip = valA <= valB;
                        cycles += 2;
                        break;
                    case Opcode.IfB:
                        valA = LoadValue( a, ref cycles );
                        valB = LoadValue( b, ref cycles );
                        InstructionSkip = ( valA & valB ) == 0;
                        cycles += 2;
                        break;
                    default:
                        break;
                }
            }

            if ( skip )
            {
                mySkip = false;
                return 1;
            }

            return cycles;
        }

        private ushort LoadValue( int identifier, ref int cycles )
        {
            if ( mySkip )
            {
                if ( ( identifier >= 0x10 && identifier < 0x18 ) || ( identifier >= 0x1e && identifier < 0x20 ) )
                    ++myPC;
                return 0x0000;
            }

            ushort value;
            bool reference = false;

            if ( identifier < 0x18 )
            {
                value = GetRegister( (Register) ( identifier & 0x7 ) );
                reference = identifier >= 0x8;

                if ( identifier >= 0x10 )
                {
                    value += myMemory[ myPC++ ];
                    ++cycles;
                }
            }
            else
            {
                switch( identifier )
                {
                    case 0x18:
                        value = mySP++;
                        reference = true;
                        break;
                    case 0x19:
                        value = mySP;
                        reference = true;
                        break;
                    case 0x1a:
                        value = (ushort) ( mySP - 1 );
                        reference = true;
                        break;
                    case 0x1b:
                        value = mySP;
                        reference = false;
                        break;
                    case 0x1c:
                        value = myCurPC;
                        reference = false;
                        break;
                    case 0x1d:
                        value = myOverflow;
                        reference = false;
                        break;
                    case 0x1e:
                    case 0x1f:
                        value = myMemory[ myPC++ ];
                        reference = identifier == 0x1e;
                        ++cycles;
                        break;
                    default:
                        value = (ushort) ( identifier & 0x1f );
                        reference = false;
                        break;
                }
            }

            if ( reference )
                value = myMemory[ value ];

            return value;
        }

        private void StoreValue( int identifier, ushort value, ref int cycles )
        {
            ushort oldPC = myPC;
            myPC = (ushort) ( myCurPC + 1 );

            if ( identifier < 0x18 )
            {
                Register register = (Register) ( identifier & 0x7 );

                if ( identifier < 0x08 )
                    SetRegister( register, value );
                else if ( identifier < 0x10 )
                    SetMemory( GetRegister( register ), value );
                else
                {
                    SetMemory( GetRegister( register ) + myMemory[ myPC++ ], value );
                    ++cycles;
                }
            }
            else
            {
                switch ( identifier )
                {
                    case 0x18:
                        SetMemory( mySP++, value );
                        break;
                    case 0x19:
                        SetMemory( mySP, value );
                        break;
                    case 0x1a:
                        SetMemory( --mySP, value );
                        break;
                    case 0x1b:
                        mySP = value;
                        break;
                    case 0x1c:
                        myPC = value;
                        break;
                    case 0x1d:
                        myOverflow = value;
                        break;
                    case 0x1e:
                        SetMemory( myMemory[ myPC++ ], value );
                        ++cycles;
                        break;
                    case 0x1f:
                        ++myPC;
                        ++cycles;
                        break;
                    default:
                        break;
                }
            }

            if ( identifier != 0x1c )
                myPC = oldPC;
        }
    }
}
