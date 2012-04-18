using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DCPU16
{
    public enum DASM16Command
    {
        Define,
        LDefine,
        Include
    }

    public class DASM16CommandInfo
    {
        public readonly DASM16Command Command;
        public readonly String[] Args;

        public DASM16CommandInfo( DASM16Command command, String[] args )
        {
            Command = command;
            Args = args;
        }
    }
}
