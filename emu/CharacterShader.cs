using OpenTK.Graphics.OpenGL;
using System;

namespace DCPU16.Emulator
{
    public class CharacterShader : ShaderProgram2D
    {
        private DateTime myCreateTime;
        private Texture2D myTexture;
        private int myBlinkPhase;
        private int myBlinkPhaseLoc;

        public Texture2D Texture
        {
            get
            {
                return myTexture;
            }
            set
            {
                if ( myTexture != value )
                {
                    SetTexture( "texture0", value );
                    myTexture = value;
                }
            }
        }

        public CharacterShader( Texture2D texture )
        {
            ShaderBuilder vert = new ShaderBuilder( ShaderType.VertexShader, true );
            vert.AddUniform( ShaderVarType.Int, "blink_phase" );
            vert.AddAttribute( ShaderVarType.Vec2, "in_position" );
            vert.AddAttribute( ShaderVarType.Vec2, "in_value" );
            vert.AddVarying( ShaderVarType.Vec2, "var_texture" );
            vert.AddVarying( ShaderVarType.Vec3, "var_fore_colour" );
            vert.AddVarying( ShaderVarType.Vec3, "var_back_colour" );
            vert.Logic = @"
                void main( void )
                {
                    gl_Position = in_position;

                    int ch = int( in_value.x ) & 0x7f;
                    int corner = int( in_value.y );
                    var_texture = vec2( ( ( ch % 16 ) + ( corner % 2 ) ) * 4.0, ( ( ch / 16 ) + ( corner / 2 ) ) * 8.0 ) / 64.0;
                    int fore = ( int( in_value ) >> 12 ) & 0xf;
                    int back = ( int( in_value ) >> 8 ) & 0xf;

                    var_back_colour = vec3( ( back >> 2 ) & 1, ( back >> 1 ) & 1, back & 1 ) * 2.0 / 3.0;
                    if( ( back & 0x8 ) != 0 )
                        var_back_colour += vec3( 1.0, 1.0, 1.0 ) / 3.0;
                    if( blink_phase == 1 || ( int( in_value.x ) & 0x80 ) == 0 )
                    {
                        var_fore_colour = vec3( ( fore >> 2 ) & 1, ( fore >> 1 ) & 1, fore & 1 ) * 2.0 / 3.0;
                        if( ( fore & 0x8 ) != 0 )
                            var_fore_colour += vec3( 1.0, 1.0, 1.0 ) / 3.0;
                    }
                    else
                        var_fore_colour = var_back_colour;
                }
            ";

            ShaderBuilder frag = new ShaderBuilder( ShaderType.FragmentShader, true );
            frag.AddUniform( ShaderVarType.Sampler2D, "texture0" );
            frag.AddVarying( ShaderVarType.Vec2, "var_texture" );
            frag.AddVarying( ShaderVarType.Vec3, "var_fore_colour" );
            frag.AddVarying( ShaderVarType.Vec3, "var_back_colour" );
            frag.Logic = @"
                void main( void )
                {
                    if( texture2D( texture0, var_texture ).r > 0.5 )
                        out_frag_colour = vec4( var_fore_colour.rgb, 1.0 );
                    else
                        out_frag_colour = vec4( var_back_colour.rgb, 1.0 );
                }
            ";

            VertexSource = vert.Generate( GL3 );
            FragmentSource = frag.Generate( GL3 );

            myTexture = texture;

            BeginMode = BeginMode.Quads;
        }

        public CharacterShader( Texture2D texture, int width, int height )
            : this( texture )
        {
            Create();
            SetScreenSize( width, height );
        }

        protected override void OnCreate()
        {
            base.OnCreate();

            AddAttribute( "in_value", 2 );
            AddAttribute( "in_position", 2 );

            AddTexture( "texture0", TextureUnit.Texture0 );
            SetTexture( "texture0", myTexture );

            myBlinkPhaseLoc = GL.GetUniformLocation( Program, "blink_phase" );

            myCreateTime = DateTime.Now;
            myBlinkPhase = 0;

            GL.Uniform1( myBlinkPhaseLoc, myBlinkPhase );
        }

        protected override void OnStartBatch()
        {
            lock ( myTexture )
                myTexture.Bind();

            DateTime now = DateTime.Now;
            int blinkPhase = ( (int) ( now - myCreateTime ).TotalMilliseconds / 500 ) % 2;
            if ( blinkPhase != myBlinkPhase )
            {
                myBlinkPhase = blinkPhase;
                GL.Uniform1( myBlinkPhaseLoc, myBlinkPhase );
            }
        }
    }
}
