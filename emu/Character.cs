using System;

using OpenTK;
using OpenTK.Graphics;

namespace DCPU16.Emulator
{
    public class Character
    {
        internal float[] Vertices
        {
            get
            {
                return myVertices;
            }
        }

        private float[] myVertices;

        private Vector2 myPosition;
        private Vector2 myScale;

        private float myValue;

        protected bool VertsChanged;
        
        public virtual Vector2 Position
        {
            get
            {
                return myPosition;
            }
            set
            {
                if ( value != myPosition )
                {
                    myPosition = value;
                    VertsChanged = true;
                }
            }
        }

        public virtual Vector2 Size
        {
            get
            {
                return new Vector2( 4 * Scale.X, 8 * Scale.Y );
            }
            set
            {
                Scale = new Vector2( value.X / 4, value.Y / 8 );
            }
        }

        public virtual Vector2 Scale
        {
            get
            {
                return myScale;
            }
            set
            {
                if ( value != myScale )
                {
                    myScale = value;
                    VertsChanged = true;
                }
            }
        }

        public float X
        {
            get
            {
                return Position.X;
            }
            set
            {
                Position = new Vector2( value, Y );
            }
        }
        public float Y
        {
            get
            {
                return Position.Y;
            }
            set
            {
                Position = new Vector2( X, value );
            }
        }

        public ushort Value
        {
            get
            {
                return (ushort) myValue;
            }
            set
            {
                if ( (ushort) myValue != value )
                {
                    myValue = value;
                    VertsChanged = true;
                }
            }
        }

        public float Width
        {
            get
            {
                return Size.X;
            }
            set
            {
                Scale = new Vector2( value / 4, Scale.Y );
            }
        }
        public float Height
        {
            get
            {
                return Size.Y;
            }
            set
            {
                Scale = new Vector2( Scale.X, value / 8 );
            }
        }

        public Character( float scale = 1.0f )
        {
            Position = new Vector2();
            Scale = new Vector2( scale, scale );
            Value = 0x0000;
        }

        protected virtual float[] FindVerts()
        {
            float[,] verts = new float[ , ]
            {
                { Position.X, Position.Y },
                { Position.X + Width, Position.Y },
                { Position.X + Width, Position.Y + Height },
                { Position.X, Position.Y + Height }
            };

            return new float[]
            {
                Value, 0, verts[ 0, 0 ], verts[ 0, 1 ],
                Value, 1, verts[ 1, 0 ], verts[ 1, 1 ],
                Value, 3, verts[ 2, 0 ], verts[ 2, 1 ],
                Value, 2, verts[ 3, 0 ], verts[ 3, 1 ],
            };
        }

        public virtual void Render( CharacterShader shader )
        {
            if ( VertsChanged )
            {
                myVertices = FindVerts();
                VertsChanged = false;
            }

            shader.Render( myVertices );
        }
    }
}
