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
            ; Rainbow display test
            ; By Metapyziks

.define     vidloc 0x8000
.define     vidwid 0x20
.define     vidhei 0x20
.define     char 'X'

            set x, 0
:xloop
            set y, 0
:yloop
            set c, x
            div c, 2
            set a, c
            shl a, 0x4
            add a, y
            shl a, 0x8
            add a, char
            
            set b, y
            mul b, vidwid
            add b, x
            set [vidloc+b], a
            
            add y, 1
            ifn y, vidhei
                set PC, yloop
                
            add x, 1
            ifn x, vidwid
                set PC, xloop
:end
            set PC, end";
        
        private static String[] stInputPaths;
        private static String stOutputDir;

        private static bool stPrint =
#if DEBUG
            true;
#else
            false;
#endif
        private static bool stCFormat =
#if DEBUG
            true;
#else
            false;
#endif

        static void Main( String[] args )
        {
            if ( !ParseArgs( args ) )
                return;

            bool error = false;

            for ( int f = 0; f < Math.Max( 1, stInputPaths.Length ); ++f )
            {
                ushort[] output = null;
                Exception ex = null;

                if ( stInputPaths.Length == 0 )
                {
#if DEBUG
                    output = V11.DASM16Assembler.AssembleString( stDefaultProgram ).Words;
#else
                    try
                    {
                        output = V11.DASM16Assembler.AssembleString( stDefaultProgram ).Words;
                    }
                    catch ( Exception e )
                    {
                        ex = e;
                    }
#endif
                }
                else
                {
#if DEBUG
                    output = V11.DASM16Assembler.AssembleFile( stInputPaths[ f ] ).Words;
#else
                    try
                    {
                        output = V11.DASM16Assembler.AssembleFile( stInputPaths[ f ] ).Words;
                    }
                    catch ( Exception e )
                    {
                        ex = e;
                    }
#endif
                }

                if ( ex != null )
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    if ( stInputPaths.Length > 0 )
                        Console.WriteLine( "Error while assembling " + stInputPaths[ f ] + ":" );
                    else
                        Console.WriteLine( "Error while assembling default input:" );
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine( ex.Message );
                    error = true;
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

                        if ( stCFormat )
                        {
                            outPath += ".txt";

                            String nl = Environment.NewLine;
                            using ( FileStream stream = new FileStream( outPath, FileMode.Create, FileAccess.Write ) )
                            {
                                using ( StreamWriter writer = new StreamWriter( stream ) )
                                {
                                    writer.Write( "{" + nl + "    " );
                                    for ( int i = 0; i < output.Length; ++i )
                                    {
                                        writer.Write( "0x" + output[ i ].ToString( "X4" ).ToLower() );

                                        if ( i < output.Length - 1 )
                                        {
                                            writer.Write( ", " );

                                            if ( i % 8 == 7 )
                                                writer.Write( nl + "    " );
                                        }
                                    }
                                    writer.Write( nl + "}" + nl );
                                }
                            }
                        }
                    }
                }
            }

            if ( stPrint || error )
            {
                Console.WriteLine( "\nPress any key to exit..." );
                Console.ReadKey();
            }
        }

        static bool ParseArgs( String[] args )
        {
            List<String> inputPaths = new List<String>();

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
                        case "-cform":
                            stCFormat = true;
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
