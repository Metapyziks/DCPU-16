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
			add a, [char]
			
			set b, y
			mul b, [width]
			add b, x
			set [0x8000+b], a
			
			add y, 1
			ifn y, [height]
				set PC, yloop
				
			add x, 1
			ifn x, [width]
				set PC, xloop
:end
			set PC, end

:char		dat 'X'
:width		dat 0x20
:height		dat 0x10";
        
        private static string[] stInputPaths;
        private static string stOutputDir;

        private static bool stPrint = true;

        static void Main( string[] args )
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
                    }
                }
            }

            if ( stPrint || error )
            {
                Console.WriteLine( "\nPress any key to exit..." );
                Console.ReadKey();
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
