using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DCPU16
{
    public class DASM16Builder
    {
        private List<DASM16Instruction> myInstructions;
        private Dictionary<ushort, DASM16Builder> myChildren;
        private Dictionary<String, ushort> myGlobalConstants;
        private Dictionary<String, ushort> myGlobalLabels;
        private Dictionary<String, ushort> myLocalConstants;
        private Dictionary<String, ushort> myLocalLabels;

        public int InstructionCount { get; private set; }
        public ushort Length { get; private set; }

        public DASM16Builder()
        {
            myInstructions = new List<DASM16Instruction>();
            myChildren = new Dictionary<ushort, DASM16Builder>();

            myGlobalConstants = new Dictionary<String, ushort>();
            myGlobalLabels = new Dictionary<String, ushort>();
            myLocalConstants = new Dictionary<String, ushort>();
            myLocalLabels = new Dictionary<String, ushort>();

            InstructionCount = 0;
            Length = 0;
        }

        public void AddInstruction( DASM16Instruction ins )
        {
            myInstructions.Add( ins );
            ++InstructionCount;
            Length += ins.Length;
        }

        public void AddLabel( String label, bool local = false )
        {
            Dictionary<String, ushort> dict = ( local ? myLocalLabels : myGlobalLabels );

            if ( !dict.ContainsKey( label ) )
                dict.Add( label, Length );
        }

        public void AddConstant( String label, ushort value, bool local = false )
        {
            Dictionary<String, ushort> dict = ( local ? myLocalLabels : myGlobalLabels );

            if ( !dict.ContainsKey( label ) )
                dict.Add( label, value );
        }

        public void AddChild( DASM16Builder child )
        {
            if ( child.Length > 0 )
            {
                myChildren.Add( Length, child );
                InstructionCount += child.InstructionCount;
                Length += child.Length;
            }
        }

        public DASM16Assembly Assemble( ushort offset = 0x0000 )
        {
            Dictionary<String, ushort> consts = new Dictionary<string, ushort>();
            CombineGlobals( consts, offset );

            return new DASM16Assembly( Assemble( consts, offset ), offset );
        }

        private void CombineGlobals( Dictionary<String, ushort> consts, ushort offset )
        {
            foreach ( KeyValuePair<String, ushort> keyVal in myGlobalLabels )
                if ( !consts.ContainsKey( keyVal.Key ) )
                    consts.Add( keyVal.Key, (ushort) ( keyVal.Value + offset ) );

            foreach ( KeyValuePair<String, ushort> keyVal in myGlobalConstants )
                if ( !consts.ContainsKey( keyVal.Key ) )
                    consts.Add( keyVal.Key, keyVal.Value );

            foreach ( KeyValuePair<ushort, DASM16Builder> child in myChildren )
                child.Value.CombineGlobals( consts, (ushort) ( offset + child.Key ) );
        }

        private Dictionary<String, ushort> CombineLocals( Dictionary<String, ushort> consts, ushort offset )
        {
            Dictionary<String, ushort> newConsts = new Dictionary<string, ushort>();

            foreach ( KeyValuePair<String, ushort> keyVal in myLocalLabels )
                newConsts.Add( keyVal.Key, (ushort) ( keyVal.Value + offset ) );

            foreach ( KeyValuePair<String, ushort> keyVal in myLocalConstants )
                if ( !newConsts.ContainsKey( keyVal.Key ) )
                    newConsts.Add( keyVal.Key, keyVal.Value );

            foreach ( KeyValuePair<String, ushort> keyVal in consts )
                if ( !newConsts.ContainsKey( keyVal.Key ) )
                    newConsts.Add( keyVal.Key, keyVal.Value );

            return newConsts;
        }

        private DASM16Instruction[] Assemble( Dictionary<String, ushort> consts, ushort offset )
        {
            Dictionary<String, ushort> newConsts = CombineLocals( consts, offset );

            foreach ( DASM16Instruction instruction in myInstructions )
                instruction.ResolveConstants( newConsts );

            DASM16Instruction[] output = new DASM16Instruction[ InstructionCount ];

            ushort word = 0;
            int i = 0, ins = 0;
            while ( word < Length )
            {
                if ( myChildren.ContainsKey( word ) )
                {
                    DASM16Builder child = myChildren[ word ];
                    foreach ( DASM16Instruction instruction in child.Assemble( consts, (ushort) ( offset + word ) ) )
                    {
                        word += instruction.Length;
                        output[ i++ ] = instruction;
                    }
                }
                else if ( ins < myInstructions.Count )
                {
                    DASM16Instruction instruction = myInstructions[ ins++ ];
                    word += instruction.Length;
                    output[ i++ ] = instruction;
                }
            }

            return output;
        }
    }
}
