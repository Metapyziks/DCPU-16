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
            0x7c01, 0x0030, 0x7de1, 0x1000, 0x0020, 0x7803, 0x1000, 0xc00d,
            0x7dc1, 0x001a, 0xa861, 0x7c01, 0x2000, 0x2161, 0x2000, 0x8463,
            0x806d, 0x7dc1, 0x000d, 0x9031, 0x7c10, 0x0018, 0x7dc1, 0x001a,
            0x9037, 0x61c1, 0x7dc1, 0x001a, 0x0000, 0x0000, 0x0000, 0x0000
        };

        private static int stScreenRows = 32;
        private static int stScreenCols = 64;
        private static int stRamSizeWords = 0x10000;
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

            Console.WindowWidth = stScreenCols;
            Console.WindowHeight = stScreenRows;
            Console.BufferWidth = stScreenCols;
            Console.BufferHeight = stScreenRows;

            myCPU = new DCPU16Emulator( stRamSizeWords );

            if ( stCodePath == null )
                myCPU.LoadProgram( stDefaultProgram );
            else
                myCPU.LoadProgram( File.ReadAllBytes( stCodePath ) );

            while ( !myCPU.Exited )
                Thread.Sleep( myCPU.Step() * 10 );
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
                        case "-ram":
                            if ( !int.TryParse( args[ ++i ], out stRamSizeWords ) )
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
