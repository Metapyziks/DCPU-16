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
            ; Assembler test for DCPU
            ; by Markus Persson
 
            set a, 0xbeef                           ; Assign 0xbeef to register a
            set [0x1000], a                         ; Assign memory at 0x1000 to value of register a
            ifn a, [0x1000]                         ; Compare value of register a to memory at 0x1000 ..
                set PC, end                         ; .. and jump to end if they don't match
 
            set i, 0                                ; Init loop counter, for clarity

:nextchar   ife [data+i], 0                         ; If the character is 0 ..
            set PC, end                             ; .. jump to the end
            set [0x8000+i], [data+i]                ; Video ram starts at 0x8000, copy char there
            add i, 1                                ; Increase loop counter
            set PC, nextchar                        ; Loop
 
:data       dat ""Hello world!"", 0         ; Zero terminated string
 
:end        sub PC, 1                       ; Freeze the CPU forever";

        private static string[] stInputPaths;
        private static string stOutputDir;

        private static bool stPrint = true;

        static void Main( string[] args )
        {
            if ( !ParseArgs( args ) )
                return;

            for ( int f = 0; f < Math.Max( 1, stInputPaths.Length ); ++f )
            {
                ushort[] output = null;
                Exception ex = null;

                if ( stInputPaths.Length == 0 )
                {
                    try
                    {
                        output = DCPU16Assembler.Assemble( stDefaultProgram );
                    }
                    catch ( Exception e )
                    {
                        ex = e;
                    }
                }
                else if ( File.Exists( stInputPaths[ f ] ) )
                {
                    try
                    {
                        output = DCPU16Assembler.Assemble( File.ReadAllText( stInputPaths[ f ] ) );
                    }
                    catch ( Exception e )
                    {
                        ex = e;
                    }
                }
                else
                {
                    Console.WriteLine( "File \"" + stInputPaths[ f ] + "\" does not exist!" );
                    break;
                }

                if ( ex != null )
                {
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    if ( stInputPaths.Length > 0 )
                        Console.WriteLine( "Error while assembling " + stInputPaths[ f ] + ":" );
                    else
                        Console.WriteLine( "Error while assembling default input:" );
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine( ex.Message );
                    Console.ReadKey();
                }
                else
                {
                    if ( stPrint )
                    {
                        if ( stInputPaths.Length > 0 )
                            Console.WriteLine( "Assembled code for " + stInputPaths[ f ] + ":" );
                        else
                            Console.WriteLine( "Assembled code for default input:" );

                        for ( int i = 0; i < ( ( output.Length + 7 ) / 8 ) * 8; ++i )
                        {
                            if ( i % 8 == 0 )
                                Console.Write( "  " + i.ToString( "X4" ) + ": " );

                            if ( i < output.Length )
                                Console.Write( output[ i ].ToString( "X4" ).ToLower() + " " );
                            else
                                Console.Write( "0000 " );

                            if ( i % 8 == 7 )
                                Console.WriteLine();
                        }
                    }

                    if ( stInputPaths.Length != 0 )
                    {
                        String outPath = stOutputDir ?? Path.GetDirectoryName( stInputPaths[ f ] );

                        outPath += Path.DirectorySeparatorChar + Path.GetFileNameWithoutExtension( stInputPaths[ f ] ) + ".dcpu16";

                        using ( FileStream stream = new FileStream( outPath, FileMode.Create, FileAccess.Write ) )
                        {
                            for ( int i = 0; i < output.Length; ++i )
                            {
                                stream.WriteByte( (byte) ( ( output[ i ] >> 0x8 ) & 0xff ) );
                                stream.WriteByte( (byte) ( output[ i ] & 0xff ) );
                            }
                        }
                    }
                }
            }
        }

        static bool ParseArgs( string[] args )
        {
            List<String> inputPaths = new List<string>();

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
                        case "-outdir":
                            if( ++i < args.Length )
                                stOutputDir = args[ i ];
                            break;
                        default:
                            Console.WriteLine( "Invalid argument \"" + arg + "\"" );
                            return false;
                    }
                }
                else
                    inputPaths.Add( arg );
            }

            stInputPaths = inputPaths.ToArray();
            return true;
        }
    }
}
