using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace DCPU16.Assembler
{
    class Program
    {
        private static readonly String stDefaultProgram = @"
        ; Try some basic stuff
                      SET A, 0x30              ; 7c01 0030
                      SET [0x1000], 0x20       ; 7de1 1000 0020
                      SUB A, [0x1000]          ; 7803 1000
                      IFN A, 0x10              ; c00d 
                          SET PC, crash        ; 7dc1 001a [*]
                      
        ; Do a loopy thing
                      SET I, 10                ; a861
                      SET A, 0x2000            ; 7c01 2000
        :loop         SET [0x2000+I], [A]      ; 2161 2000
                      SUB I, 1                 ; 8463
                      IFN I, 0                 ; 806d
                          SET PC, loop         ; 7dc1 000d [*]
        
        ; Call a subroutine
                      SET X, 0x4               ; 9031
                      JSR testsub              ; 7c10 0018 [*]
                      SET PC, crash            ; 7dc1 001a [*]
        
        :testsub      SHL X, 4                 ; 9037
                      SET PC, POP              ; 61c1
                        
        ; Hang forever. X should now be 0x40 if everything went right.
        :crash        SET PC, crash            ; 7dc1 001a [*]
        
        ; [*]: Note that these can be one word shorter and one cycle faster by using the short form (0x00-0x1f) of literals,
        ;      but my assembler doesn't support short form labels yet.";

        private static string stInputPath;
        private static string stOutputPath;

        private static bool stPrint;

        static void Main( string[] args )
        {
            if ( !ParseArgs( args ) )
                return;

            if ( stInputPath != null && !File.Exists( stInputPath ) )
            {
                Console.WriteLine( "No such file found at \"" + stInputPath + "\"" );
                return;
            }

            ushort[] output;

            if ( stInputPath == null )
                output = DCPU16Assembler.Assemble( stDefaultProgram );
            else
                output = DCPU16Assembler.Assemble( File.ReadAllText( stInputPath ) );

            if ( stPrint )
            {
                for ( int i = 0; i < ( ( output.Length + 7 ) / 8 ) * 8; ++i )
                {
                    if ( i % 8 == 0 )
                        Console.Write( i.ToString( "X4" ) + ": " );

                    if ( i < output.Length )
                        Console.Write( output[ i ].ToString( "X4" ).ToLower() + " " );
                    else
                        Console.Write( "0000 " );

                    if ( i % 8 == 7 )
                        Console.WriteLine();
                }

                Console.WriteLine( "Press any key to continue..." );
                Console.ReadKey();
            }

            if ( stOutputPath == null && stInputPath != null )
            {
                stOutputPath = stInputPath;
                int dot = stOutputPath.IndexOf( '.' );
                if ( dot > -1 )
                    stOutputPath = stOutputPath.Substring( 0, dot ) + ".dcpu16";
            }

            if ( stOutputPath != null )
            {
                using ( FileStream stream = new FileStream( stOutputPath, FileMode.Create, FileAccess.Write ) )
                {
                    for ( int i = 0; i < output.Length; ++i )
                    {
                        stream.WriteByte( (byte) ( ( output[ i ] >> 0x8 ) & 0xff ) );
                        stream.WriteByte( (byte) ( output[ i ] & 0xff ) );
                    }
                }
            }
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
                        case "-print":
                            stPrint = true;
                            break;
                        default:
                            Console.WriteLine( "Invalid argument \"" + arg + "\"" );
                            return false;
                    }
                }
                else if ( stInputPath == null )
                {
                    stInputPath = arg;
                }
                else if ( stOutputPath == null )
                {
                    stOutputPath = arg;
                }
            }

            return true;
        }
    }
}
