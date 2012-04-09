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
            0x8031, 0x8041, 0x0c01, 0xb007, 0x7c02, 0x0061, 0x1002, 0x1011,
            0x7c14, 0x001a, 0x0c12, 0x0111, 0x0019, 0x8442, 0x11fe, 0x001b,
            0x7dc1, 0x0002, 0x8432, 0x0dfe, 0x001a, 0x7dc1, 0x0001, 0x7dc1,
            0x0000, 0x8000, 0x0020, 0x0010, 0x0000, 0x0000, 0x0000, 0x0000
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
        private static string stCodePath;

        private static DCPU16Emulator myCPU;

        static void Main( string[] args )
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

            while ( !myCPU.Exited )
                myCPU.Step();
        }

        static bool ParseArgs( string[] args )
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
