using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;

namespace DCPU16.Emulator
{
    class Program
    {
        private static readonly ushort[] stDefaultProgram =
        {
            0x81e1, 0x8fff, 0x7de1, 0x9000, 0x4000, 0x7dc1, 0x0043, 0x0da1, 
            0x11a1, 0x15a1, 0x19a1, 0x1da1, 0x8061, 0x8071, 0x7831, 0x8fff, 
            0x843c, 0x7dc1, 0x0021, 0x5831, 0x9000, 0x1841, 0x0c42, 0x0c51, 
            0x806e, 0x8452, 0x11fe, 0x4000, 0x8452, 0x005e, 0x7dc1, 0x0025, 
            0x0c62, 0x1871, 0x0c62, 0x7dc1, 0x0013, 0x806c, 0x7dc1, 0x002c, 
            0x0172, 0x9000, 0x7dc1, 0x0032, 0x85e1, 0x8999, 0x01e1, 0x9000, 
            0x85e2, 0x9000, 0x003d, 0x7dc1, 0x003a, 0x0c62, 0x5972, 0x9000, 
            0x9000, 0x0c63, 0x1801, 0x7c02, 0x9001, 0x6071, 0x6061, 0x6051, 
            0x6041, 0x6031, 0x61c1, 0x9001, 0x7c10, 0x0007, 0x7d01, 0x0000, 
            0xffff, 0x7d01, 0x0001, 0xdead, 0x7d01, 0x0002, 0xbeef, 0x7d01, 
            0x0003, 0xffff, 0x8801, 0x7c10, 0x0007, 0x7d01, 0x0000, 0x1234, 
            0x7d01, 0x0001, 0x5678, 0xc001, 0x7c10, 0x0007, 0x8061, 0x1881, 
            0x8402, 0x8462, 0x1b0e, 0x7dc1, 0x005f, 0x0000
        };

        private static ConsoleColor[] stColours =
        {
            ConsoleColor.Black, ConsoleColor.DarkGray, ConsoleColor.Gray, ConsoleColor.White,
            ConsoleColor.DarkRed, ConsoleColor.Red,
            ConsoleColor.DarkYellow, ConsoleColor.Yellow,
            ConsoleColor.DarkGreen, ConsoleColor.Green,
            ConsoleColor.DarkCyan, ConsoleColor.Cyan,
            ConsoleColor.DarkBlue, ConsoleColor.Blue,
            ConsoleColor.DarkMagenta, ConsoleColor.Magenta
        };

        private static int stScreenRows = 16;
        private static int stScreenCols = 32;
        private static int stScreenBufferLoc = 0x8000;
        private static int stKeyboardLoc = 0x8200;
        private static String stCodePath;
        private static bool stMemDump = true;

        private static DCPU16Emulator myCPU;

        static void Main( String[] args )
        {
            if ( !ParseArgs( args ) )
                return;

            if ( stCodePath != null && !File.Exists( stCodePath ) )
            {
                Console.WriteLine( "No such file found at \"" + stCodePath + "\"" );
                return;
            }

            Console.SetWindowSize( stScreenCols + 2, stScreenRows + 2 );
            Console.SetBufferSize( stScreenCols + 2, stScreenRows + 2 );

            int screenBufferSize = stScreenCols * stScreenRows;

            myCPU = new DCPU16Emulator();

            if ( stCodePath == null )
                myCPU.LoadProgram( stDefaultProgram );
            else
                myCPU.LoadProgram( File.ReadAllBytes( stCodePath ) );

            Console.CursorVisible = false;

            myCPU.MemoryChanged += delegate( object sender, MemoryChangedEventArgs e )
            {
                if ( e.Location >= stScreenBufferLoc && e.Location < stScreenBufferLoc + screenBufferSize )
                {
                    int pos = e.Location - stScreenBufferLoc;
                    int x = pos % stScreenCols;
                    int y = pos / stScreenCols;
                    Console.SetCursorPosition( x + 1, y + 1 );
                    char c = (char) ( e.Value & 0xff );
                    int fclr = ( e.Value >> 8 ) & 0xf;
                    int bclr = ( e.Value >> 12 ) & 0xf;
                    Console.ForegroundColor = stColours[ fclr ];
                    Console.BackgroundColor = stColours[ bclr ];
                    Console.Write( c );
                }
            };

            Thread inputThread = new Thread( InputThreadEntry );
            inputThread.Start();

            while ( !myCPU.Exited )
                myCPU.Step();

            if ( stMemDump )
            {
                String nl = Environment.NewLine;
                using ( FileStream stream = new FileStream( "memorydump.txt", FileMode.Create, FileAccess.Write ) )
                {
                    using ( StreamWriter writer = new StreamWriter( stream ) )
                    {
                        writer.Write( "// Internal Registers:" + nl );
                        writer.Write( "  PC: " + myCPU.ProgramCounter.ToString( "X4" ).ToLower() + nl );
                        writer.Write( "  SP: " + myCPU.StackPointer.ToString( "X4" ).ToLower() + nl );
                        writer.Write( "   O: " + myCPU.Overflow.ToString( "X4" ).ToLower() + nl + nl );

                        writer.Write( "// General Registers:" + nl );
                        writer.Write( "  A: " + myCPU.GetRegister( Register.A ).ToString( "X4" ).ToLower() + nl );
                        writer.Write( "  B: " + myCPU.GetRegister( Register.B ).ToString( "X4" ).ToLower() + nl );
                        writer.Write( "  C: " + myCPU.GetRegister( Register.C ).ToString( "X4" ).ToLower() + nl );
                        writer.Write( "  X: " + myCPU.GetRegister( Register.X ).ToString( "X4" ).ToLower() + nl );
                        writer.Write( "  Y: " + myCPU.GetRegister( Register.Y ).ToString( "X4" ).ToLower() + nl );
                        writer.Write( "  Z: " + myCPU.GetRegister( Register.Z ).ToString( "X4" ).ToLower() + nl );
                        writer.Write( "  I: " + myCPU.GetRegister( Register.I ).ToString( "X4" ).ToLower() + nl );
                        writer.Write( "  J: " + myCPU.GetRegister( Register.J ).ToString( "X4" ).ToLower() + nl + nl );

                        writer.Write( "// Memory Dump:" + nl );

                        for ( int i = 0; i < 0x10000; ++i )
                        {
                            if ( i % 16 == 0 )
                                writer.Write( i.ToString( "X4" ).ToLower() + ": " );

                            writer.Write( myCPU.GetMemory( i ).ToString( "X4" ).ToLower() + " " );

                            if ( i % 16 == 15 )
                                writer.Write( nl );
                        }
                    }
                }
            }
        }

        static void InputThreadEntry()
        {
            Queue<ConsoleKeyInfo> keyQueue = new Queue<ConsoleKeyInfo>();

            myCPU.MemoryChanged += delegate( object sender, MemoryChangedEventArgs e )
            {
                if ( e.Location == stKeyboardLoc && e.Value == 0x0000 && keyQueue.Count > 0 )
                    lock ( keyQueue )
                        myCPU.SetMemory( stKeyboardLoc, (ushort) keyQueue.Dequeue().KeyChar );
            };

            while ( !myCPU.Exited )
            {
                ConsoleKeyInfo key = Console.ReadKey( true );
                lock ( keyQueue )
                {
                    if ( keyQueue.Count == 0 && myCPU.GetMemory( stKeyboardLoc ) == 0x0000 )
                        myCPU.SetMemory( stKeyboardLoc, (ushort) key.KeyChar );
                    else
                        keyQueue.Enqueue( key );
                }
            }
        }

        static bool ParseArgs( String[] args )
        {
            for ( int i = 0; i < args.Length; ++i )
            {
                String arg = args[ i ];
                if ( arg.StartsWith( "-" ) )
                {
                    switch ( arg.ToLower() )
                    {
                        case "-rows":
                            if ( !int.TryParse( args[ ++i ], out stScreenRows ) )
                            {
                                Console.WriteLine( "Invalid value for argument \"" + arg + "\"" );
                                return false;
                            }
                            break;
                        case "-cols":
                            if ( !int.TryParse( args[ ++i ], out stScreenCols ) )
                            {
                                Console.WriteLine( "Invalid value for argument \"" + arg + "\"" );
                                return false;
                            }
                            break;
                        case "-vidloc":
                            if ( !int.TryParse( args[ ++i ], out stScreenBufferLoc ) )
                            {
                                Console.WriteLine( "Invalid value for argument \"" + arg + "\"" );
                                return false;
                            }
                            break;
                        case "-keyloc":
                            if ( !int.TryParse( args[ ++i ], out stKeyboardLoc ) )
                            {
                                Console.WriteLine( "Invalid value for argument \"" + arg + "\"" );
                                return false;
                            }
                            break;
                        case "-memdump":
                            stMemDump = true;
                            break;
                        default:
                            Console.WriteLine( "Invalid argument \"" + arg + "\"" );
                            return false;
                    }
                }
                else if( stCodePath == null )
                {
                    stCodePath = arg;
                }
            }

            return true;
        }
    }
}
