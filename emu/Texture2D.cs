﻿using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace DCPU16.Emulator
{
    public class Texture2D : Texture
    {
        public static readonly Texture2D Blank;

        static Texture2D()
        {
            Bitmap blankBmp = new Bitmap( 1, 1 );
            blankBmp.SetPixel( 0, 0, Color.White );
            Blank = new Texture2D( blankBmp );
        }

        public int Width { get; private set; }
        public int Height { get; private set; }
        public Bitmap Bitmap { get; private set; }

        public Texture2D( Bitmap bitmap )
            : base( TextureTarget.Texture2D )
        {
            Width = bitmap.Width;
            Height = bitmap.Height;

            int size = GetNextPOTS( bitmap.Width, bitmap.Height );

            if ( size == bitmap.Width && size == bitmap.Height )
                Bitmap = bitmap;
            else
            {
                Bitmap = new Bitmap( size, size );

                for ( int x = 0; x < Width; ++x )
                    for ( int y = 0; y < Height; ++y )
                        Bitmap.SetPixel( x, y, bitmap.GetPixel( x, y ) );
            }
        }

        public Vector2 GetCoords( Vector2 pos )
        {
            return GetCoords( pos.X, pos.Y );
        }

        public Vector2 GetCoords( float x, float y )
        {
            return new Vector2
            {
                X = x / Bitmap.Width,
                Y = y / Bitmap.Height
            };
        }

        public Color GetPixel( int x, int y )
        {
            return Bitmap.GetPixel( x, y );
        }

        public void SetPixel( int x, int y, Color colour )
        {
            if ( this == Blank )
                return;

            lock ( this )
            {
                Bitmap.SetPixel( x, y, colour );
                Update();
            }
        }

        protected override void Load()
        {
            GL.TexEnv( TextureEnvTarget.TextureEnv, TextureEnvParameter.TextureEnvMode, (float) TextureEnvMode.Modulate );

            BitmapData data = Bitmap.LockBits( new Rectangle( 0, 0, Bitmap.Width, Bitmap.Height ), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb );

            GL.TexImage2D( TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, Bitmap.Width, Bitmap.Height, 0, OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0 );

            Bitmap.UnlockBits( data );

            GL.TexParameter( TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (float) TextureMinFilter.Nearest );
            GL.TexParameter( TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (float) TextureMagFilter.Nearest );
        }
    }
}
