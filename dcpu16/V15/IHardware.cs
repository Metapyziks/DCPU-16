using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DCPU16.V15
{
    public interface IHardware
    {
        uint HardwareID { get; }
        ushort HardwareVersion { get; }
        uint Manufacturer { get; }

        int Interrupt( DCPU16Emulator cpu );
    }
}
