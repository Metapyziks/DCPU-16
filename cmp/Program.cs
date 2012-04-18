using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DCPU16.Compiler
{
    class Program
    {
        private static readonly String stDefaultProgram = @"
            u16 testA = 56;
            u16 testB = 67 + 12;
            u16 testC = ( 71 - 32 + 8 ) * 2;
            u16 testD = testB - testA + testC;";

        static void Main( String[] args )
        {

        }
    }
}
