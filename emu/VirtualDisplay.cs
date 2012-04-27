using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Reflection;
using System.IO;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Input;

using DCPU16.V15;

namespace DCPU16.Emulator
{
    class LEM1802 : GameWindow, IHardware
    {
        private ushort[] myDefaultCharSet;

        private Texture2D myCharacterSet;
        private CharacterShader myCharShader;
        private Character[] myCharMap;

        private bool myDumping;

        private int myKeyIndex;
        private Queue<ushort> myKeyQueue;

        public DCPU16Emulator CPU { get; private set; }

        public readonly int Rows;
        public readonly int Columns;
        public readonly int Scale;

        public ushort VideoBufferLoc { get; private set; }
        public ushort VideoBufferLength { get; private set; }
        public ushort CharacterSetLoc { get; private set; }
        
        public uint HardwareID
        {
            get { return 0x7349f615; }
        }

        public ushort HardwareVersion
        {
            get { return 0x1802; }
        }

        public uint Manufacturer
        {
            get { return 0x1c6c8b36; }
        }

        public bool Ready { get; private set; }

        public LEM1802( DCPU16Emulator cpu, int rows = 12, int cols = 32, int scale = 2 )
            : base( ( cols + 4 ) * 4 * scale, ( rows + 2 ) * 8 * scale,
                new GraphicsMode( new ColorFormat( 8, 8, 8, 0 ), 0 ),
                "DCPU16 Virtual Display" )
        {
            CPU = cpu;

            Rows = rows;
            Columns = cols;
            Scale = scale;

            VideoBufferLoc = 0;
            VideoBufferLength = (ushort) ( Rows * Columns );
            CharacterSetLoc = 0;

            myKeyIndex = 0;
            myKeyQueue = new Queue<ushort>();

            myDumping = false;
        }

        public int Interrupt( DCPU16Emulator cpu )
        {
            switch ( cpu.A )
            {
                case 0: // MEM_MAP_SCREEN
                    VideoBufferLoc = cpu.B;
                    return 0;
                case 1: // MEM_MAP_FONT
                    CharacterSetLoc = cpu.B;
                    return 0;
                case 2: // MEM_MAP_PALETTE
                    throw new NotImplementedException();
                case 3: // SET_BORDER_COLOR
                    throw new NotImplementedException();
                case 4: // MEM_DUMP_FONT
                    myDumping = true;
                    for ( int i = 0; i < 256; ++i )
                        cpu.SetMemory( cpu.B + i, myDefaultCharSet[ i ] );
                    myDumping = false;
                    return 256;
                case 5: // MEM_DUMP_PALETTE
                    throw new NotImplementedException();
            }

            return 0;
        }

        protected override void OnLoad( EventArgs e )
        {
            String exePath = Path.GetDirectoryName( Assembly.GetExecutingAssembly().GetName().CodeBase );
            if ( exePath.StartsWith( "file:\\" ) )
                exePath = exePath.Substring( 6 );
            myCharacterSet = new Texture2D( new Bitmap( exePath + Path.DirectorySeparatorChar + "charset.png" ) );
            myDefaultCharSet = new ushort[ 256 ];
            for ( int i = 0; i < 127; ++i )
            {
                for ( int w = 0; w < 2; ++w )
                {
                    ushort word = 0x0000;
                    for ( int j = 0; j < 16; ++j )
                    {
                        int x = ( ( i & 0xf ) << 2 ) + ( w << 1 ) + ( j >> 3 );
                        int y = ( ( i >> 4 ) << 3 ) + ( j & 7 );
                        if ( myCharacterSet.GetPixel( x, y ).R >= 128 )
                            word |= (ushort) ( 1 << ( 15 - j ) );
                    }
                    myDefaultCharSet[ ( i << 1 ) + w ] = word;
                }
            }

            myCharShader = new CharacterShader( myCharacterSet, Width, Height );
            myCharMap = new Character[ VideoBufferLength ];
            for ( int i = 0; i < VideoBufferLength; ++i )
            {
                myCharMap[ i ] = new Character( Scale );
                myCharMap[ i ].Position = new Vector2( ( i % Columns + 2 ) * 4 * Scale,
                    ( i / Columns + 1 ) * 8 * Scale );
                myCharMap[ i ].Value = 0x0000;
            }

            CPU.MemoryChanged += delegate( object sender, MemoryChangedEventArgs me )
            {
                if ( myDumping )
                    return;

                if ( VideoBufferLoc != 0 && me.Location >= VideoBufferLoc && me.Location < VideoBufferLoc + VideoBufferLength )
                {
                    int index = me.Location - VideoBufferLoc;
                    myCharMap[ index ].Value = me.Value;
                }
                else if ( CharacterSetLoc != 0 && me.Location >= CharacterSetLoc && me.Location < CharacterSetLoc + 256 )
                {
                    int index = me.Location - CharacterSetLoc;
                    int x = ( index % 32 ) * 2;
                    int y = ( index / 32 ) * 8;
                    ushort val = me.Value;
                    for ( int i = 0; i < 16; ++i )
                    {
                        if ( ( ( val >> i ) & 1 ) == 1 )
                            myCharacterSet.SetPixel( x + 1 - i / 8, y + i % 8, Color.White );
                        else
                            myCharacterSet.SetPixel( x + 1 - i / 8, y + i % 8, Color.Black );
                    }
                }
            };

            Ready = true;
        }

        protected override void OnRenderFrame( FrameEventArgs e )
        {
            myCharShader.Begin();
            foreach ( Character ch in myCharMap )
                ch.Render( myCharShader );
            myCharShader.End();

            SwapBuffers();
        }
    }
}
