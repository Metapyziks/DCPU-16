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

namespace DCPU16.Emulator
{
    class VirtualDisplay : GameWindow
    {
        private Texture2D myCharacterSet;
        private CharacterShader myCharShader;
        private Character[] myCharMap;

        private int myKeyIndex;
        private Queue<ushort> myKeyQueue;

        public readonly V11.DCPU16Emulator CPU;

        public readonly int Rows;
        public readonly int Columns;
        public readonly int Scale;

        public readonly ushort VideoBufferLoc;
        public readonly ushort VideoBufferLength;
        public readonly ushort CharacterSetLoc;
        public readonly ushort KeyBufferLoc;

        public bool Ready { get; private set; }

        public VirtualDisplay( V11.DCPU16Emulator cpu, int rows = 12, int cols = 32, int scale = 2,
            ushort vidBufferLoc = 0x8000, ushort charSetLoc = 0x8180, ushort keyBufferLoc = 0x9000 )
            : base( ( cols + 4 ) * 4 * scale, ( rows + 2 ) * 8 * scale,
                new GraphicsMode( new ColorFormat( 8, 8, 8, 0 ), 0 ),
                "DCPU16 Virtual Display" )
        {
            CPU = cpu;

            Rows = rows;
            Columns = cols;
            Scale = scale;

            VideoBufferLoc = vidBufferLoc;
            VideoBufferLength = (ushort) ( Rows * Columns );
            CharacterSetLoc = charSetLoc;
            KeyBufferLoc = keyBufferLoc;

            myKeyIndex = 0;
            myKeyQueue = new Queue<ushort>();
        }

        protected override void OnLoad( EventArgs e )
        {
            String exePath = Path.GetDirectoryName( Assembly.GetExecutingAssembly().GetName().CodeBase );
            if ( exePath.StartsWith( "file:\\" ) )
                exePath = exePath.Substring( 6 );
            myCharacterSet = new Texture2D( new Bitmap( exePath + Path.DirectorySeparatorChar + "charset.png" ) );
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
                    CPU.SetMemory( CharacterSetLoc + ( i << 2 ) + w, word );
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

            CPU.MemoryChanged += delegate( object sender, V11.MemoryChangedEventArgs me )
            {
                if ( me.Location >= VideoBufferLoc && me.Location < VideoBufferLoc + VideoBufferLength )
                {
                    int index = me.Location - VideoBufferLoc;
                    myCharMap[ index ].Value = me.Value;
                }
                else if ( me.Location >= CharacterSetLoc && me.Location < CharacterSetLoc + 256 )
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
                else if ( me.Location >= KeyBufferLoc && me.Location < KeyBufferLoc + 0x10 )
                {
                    if ( me.Value == 0x0000 && myKeyQueue.Count > 0 )
                        WriteKey( myKeyQueue.Dequeue() );
                }
            };

            Keyboard.KeyRepeat = true;
            Keyboard.KeyDown += delegate( object sender, KeyboardKeyEventArgs ke )
            {
                ushort key = KeyToUShort( ke.Key );

                if ( key == 0xffff )
                    return;

                if ( CPU.GetMemory( KeyBufferLoc + myKeyIndex ) == 0x0000 )
                    WriteKey( key );
                else
                    myKeyQueue.Enqueue( key );
            };

            Ready = true;
        }

        protected override void OnKeyPress( KeyPressEventArgs e )
        {
            ushort key = KeyCharToUShort( e.KeyChar );

            if ( key == 0xffff )
                return;

            if ( CPU.GetMemory( KeyBufferLoc + myKeyIndex ) == 0x0000 )
                WriteKey( key );
            else
                myKeyQueue.Enqueue( key );
        }

        private ushort KeyToUShort( Key key )
        {
            switch( key )
            {
                case Key.Left:
                    return 0x25;
                case Key.Up:
                    return 0x26;
                case Key.Right:
                    return 0x27;
                case Key.Down:
                    return 0x28;
                default:
                    return 0xffff;
            }
        }

        private ushort KeyCharToUShort( ushort keyChar )
        {
            ushort key = (ushort) keyChar;

            if ( key == 0x0d )
                return 0x0a;
            
            if( key < 0x80 )
                return key;

            return 0xffff;
        }

        protected override void OnRenderFrame( FrameEventArgs e )
        {
            myCharShader.Begin();
            foreach ( Character ch in myCharMap )
                ch.Render( myCharShader );
            myCharShader.End();

            SwapBuffers();
        }

        private void WriteKey( ushort key )
        {
            CPU.SetMemory( KeyBufferLoc + myKeyIndex, key );
            CPU.SetMemory( KeyBufferLoc + 0x10, (ushort) ( KeyBufferLoc + myKeyIndex ) );
            myKeyIndex = ( myKeyIndex + 1 ) & 0xf;
        }
    }
}
