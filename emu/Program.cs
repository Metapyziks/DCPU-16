using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using System.Diagnostics;

namespace DCPU16.Emulator
{
    class Program
    {
        private static readonly ushort[] stDefaultProgram =
        {
            0x7c01, 0x0030, 0x7fc1, 0x0020, 0x1000, 0x7803, 0x1000, 0xc413,
            0x7f81, 0x0019, 0xacc1, 0x7c01, 0x2000, 0x22c1, 0x2000, 0x88c3,
            0x84d3, 0xbb81, 0x9461, 0x7c20, 0x0017, 0x7f81, 0x0019, 0x946f,
            0x6381, 0x0027
        };

        private static int stScreenRows = 12;
        private static int stScreenCols = 32;
        private static int stCycleFreq = 100000;
        private static String stCodePath;
        private static V15.DCPU16Emulator myCPU;
        private static bool stMemDump =
#if DEBUG
            true;
#else
            false;
#endif

        private static LEM1802 myDisplay;
        private static bool myStopCPU;

        static void Main( String[] args )
        {
            if ( !ParseArgs( args ) )
                return;

            if ( stCodePath != null && !File.Exists( stCodePath ) )
            {
                Console.WriteLine( "No such file found at \"" + stCodePath + "\"" );
                return;
            }

            myCPU = new V15.DCPU16Emulator();

            if ( stCodePath == null )
                myCPU.LoadProgram( stDefaultProgram );
            else
                myCPU.LoadProgram( File.ReadAllBytes( stCodePath ) );

            myDisplay = new LEM1802( myCPU, stScreenRows, stScreenCols, 4 );
            myCPU.ConnectHardware( myDisplay );

            myStopCPU = false;

            Thread cpuThread = new Thread( CPUThreadEntry );
            cpuThread.Start();

            myDisplay.Run();
            myStopCPU = true;
        }

        private static void CPUThreadEntry()
        {
            while ( !myDisplay.Ready )
                Thread.Sleep( 10 );

            long cycles = 0;
            Stopwatch timer = new Stopwatch();
            timer.Start();

            while ( !myCPU.Exited && !myStopCPU )
            {
                cycles += myCPU.Step();
                if ( stCycleFreq > 0 )
                    Thread.Sleep( Math.Max( 0, (int) ( ( cycles * 1000 / stCycleFreq ) - timer.ElapsedMilliseconds ) ) );
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
                        case "-freq":
                            if ( !int.TryParse( args[ ++i ], out stCycleFreq ) )
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
