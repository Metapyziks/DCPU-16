﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using System.Diagnostics;

namespace DCPU16.Emulator
{
    class Program
    {
        private static readonly ushort[] stDefaultProgram =
        {
            0x7c10, 0x00e4, 0x7c01, 0x07b7, 0x7c11, 0x0059, 0x7c21, 0x8182, 
            0x7c10, 0x0969, 0x81e1, 0x01f5, 0x81e1, 0x002d, 0x81e1, 0x04a7, 
            0xa9e1, 0x04a0, 0xa9e1, 0x04a1, 0x7c10, 0x01cb, 0x7c01, 0x0617, 
            0x7c11, 0x0180, 0x7c21, 0x8000, 0x7c10, 0x05f0, 0x7c01, 0x00e1, 
            0x8811, 0x7c21, 0x8122, 0x7c41, 0x8f00, 0x7c10, 0x01e4, 0x7c10, 
            0x027d, 0x7c10, 0x046f, 0x7dc1, 0x021e, 0x0000, 0x023f, 0x004f, 
            0x0050, 0x0051, 0x0052, 0x0053, 0x0054, 0x0055, 0x004f, 0x0050, 
            0x0051, 0x0056, 0x0057, 0x0058, 0x0059, 0x0000, 0xf014, 0xf015, 
            0xf016, 0xf017, 0x0000, 0xf033, 0xf034, 0xf035, 0xf036, 0xf037, 
            0xf038, 0x0000, 0xf039, 0xf03a, 0xf03b, 0xf03c, 0x0000, 0xf033, 
            0xf018, 0xf05c, 0xf07c, 0xf01c, 0xf038, 0xff00, 0x0f14, 0x0f15, 
            0x0f16, 0x0f17, 0xff00, 0x0f33, 0x0f18, 0x0f5c, 0x0f7c, 0x0f1c, 
            0x0f38, 0xff00, 0x0f39, 0x0f3a, 0x0f3b, 0x0f3c, 0xff00, 0x0001, 
            0x0009, 0x0003, 0x000b, 0x0003, 0x0009, 0xf019, 0xf01a, 0xf01b, 
            0xf01c, 0xf01d, 0xf01e, 0xf01f, 0xf03d, 0xf03f, 0xf03f, 0xf03f, 
            0xf03f, 0x0000, 0xf05f, 0xf07d, 0xf07e, 0x0000, 0x0000, 0x0000, 
            0x0000, 0xf000, 0xf001, 0xf002, 0xf003, 0xf004, 0xf005, 0xf006, 
            0xf007, 0xf008, 0xf009, 0xf00a, 0xf00b, 0xf00c, 0xf00d, 0xf00e, 
            0xf00f, 0xf010, 0xf011, 0xf012, 0xf020, 0xf021, 0xf022, 0xf023, 
            0xf024, 0xf025, 0xf026, 0xf027, 0xf028, 0xf029, 0xf02a, 0xf02b, 
            0xf02c, 0xf02d, 0xf02e, 0xf02f, 0xf030, 0xf031, 0xf032, 0xf032, 
            0xf040, 0xf041, 0xf042, 0xf043, 0xf044, 0xf045, 0xf046, 0xf047, 
            0xf048, 0xf049, 0xf04a, 0xf04b, 0xf04c, 0xf04d, 0xf04e, 0xf04f, 
            0xf050, 0xf051, 0xf052, 0xf053, 0xf054, 0xf055, 0xf056, 0xf057, 
            0xf058, 0xf059, 0xf05a, 0xf05b, 0xf060, 0xf061, 0xf062, 0xf063, 
            0xf064, 0xf065, 0xf066, 0xf067, 0xf068, 0xf069, 0xf06a, 0xf06b, 
            0xf06c, 0xf06d, 0xf06e, 0xf06f, 0xf070, 0xf071, 0xf072, 0xf073, 
            0xf074, 0xf075, 0xf076, 0xf077, 0xf078, 0xf079, 0xf07a, 0xf07b, 
            0x000a, 0x004e, 0x0045, 0x004e, 0x7c01, 0x0869, 0x7c11, 0x0080, 
            0x7c21, 0x8180, 0x7c10, 0x0969, 0x8061, 0x7c01, 0x8000, 0x7c01, 
            0x003d, 0x9411, 0x7c21, 0x808d, 0x7c10, 0x05f0, 0x7c01, 0x0043, 
            0x9411, 0x7c21, 0x80ad, 0x7c10, 0x05f0, 0x7c01, 0x0049, 0x9411, 
            0x7c21, 0x80cd, 0x7c10, 0x05f0, 0x8061, 0x8071, 0x8472, 0x7c7d, 
            0x03e8, 0x7dc1, 0x0106, 0x8c11, 0x1831, 0x8c36, 0x7c21, 0x800e, 
            0x1851, 0x8c55, 0x7c54, 0x0020, 0x1422, 0x803c, 0x7c01, 0x006d, 
            0x843c, 0x7c01, 0x0071, 0x883c, 0x7c01, 0x0075, 0x7c10, 0x05f0, 
            0x7c01, 0x007d, 0x7c23, 0x0020, 0x7c10, 0x05f0, 0x883c, 0x7c10, 
            0x01c4, 0x8462, 0xc46d, 0x7dc1, 0x0105, 0x7c01, 0x003d, 0x9411, 
            0x7c21, 0x808d, 0x7c10, 0x05f0, 0x7c01, 0x004f, 0x9411, 0x7c21, 
            0x80ad, 0x7c10, 0x05f0, 0x7c01, 0x0049, 0x9411, 0x7c21, 0x80cd, 
            0x7c10, 0x05f0, 0x7c01, 0x0081, 0xc811, 0x7c21, 0x8046, 0x7c10, 
            0x05f0, 0x7c01, 0x0094, 0xc811, 0x7c21, 0x8066, 0x7c10, 0x05f0, 
            0x7c01, 0x00a8, 0xec11, 0x7c21, 0x80e2, 0x7c10, 0x05f0, 0x7c01, 
            0x00c4, 0xec11, 0x7c21, 0x8102, 0x7c10, 0x05f0, 0x8071, 0x8472, 
            0x7c7d, 0x03e8, 0x7dc1, 0x015f, 0x8071, 0x7c01, 0x8000, 0x7c81, 
            0xff00, 0x8472, 0x8402, 0x7c7d, 0x0180, 0x7dc1, 0x0167, 0x7c01, 
            0x0055, 0x9411, 0x7c21, 0x808d, 0x7c10, 0x05f0, 0x7c01, 0x005b, 
            0x9411, 0x7c21, 0x80ad, 0x7c10, 0x05f0, 0x7c01, 0x0061, 0x9411, 
            0x7c21, 0x80cd, 0x7c10, 0x05f0, 0x8071, 0x8472, 0x7c01, 0x0081, 
            0xc811, 0x7c21, 0x8046, 0x7c10, 0x01a7, 0x7c01, 0x0094, 0xc811, 
            0x7c21, 0x8066, 0x7c10, 0x01a7, 0x7c01, 0x00a8, 0xec11, 0x7c21, 
            0x80e2, 0x7c10, 0x01a7, 0x7c01, 0x00c4, 0xec11, 0x7c21, 0x8102, 
            0x7c10, 0x01a7, 0x7c7d, 0x0042, 0x7dc1, 0x0185, 0x61c1, 0x19a1, 
            0x09a1, 0x8061, 0x1c31, 0x9836, 0x20a1, 0xa0a7, 0xa0a8, 0x7ca2, 
            0x0f00, 0x0c41, 0x7c42, 0x0067, 0x3041, 0xb047, 0x10aa, 0x8422, 
            0x8402, 0x8462, 0x8432, 0x9836, 0x046e, 0x7dc1, 0x01c1, 0x7dc1, 
            0x01ac, 0x6021, 0x6061, 0x61c1, 0x7c22, 0x0040, 0x7c01, 0x0079, 
            0x7c10, 0x05f0, 0x61c1, 0x8061, 0x7c71, 0x04f9, 0x7c41, 0x04a8, 
            0x80f1, 0x80c1, 0x8472, 0x8442, 0x8462, 0x7c6d, 0x0051, 0x7dc1, 
            0x01d0, 0x7c01, 0x0350, 0xa802, 0x7c81, 0x8701, 0x7c01, 0x035c, 
            0xa802, 0x7c81, 0x8702, 0x61c1, 0x19a1, 0x8061, 0x0da1, 0x2031, 
            0x103a, 0x0ca1, 0x8422, 0x8402, 0x8462, 0x046e, 0x7dc1, 0x01f2, 
            0x7dc1, 0x01e7, 0x6031, 0x6061, 0x61c1, 0x0000, 0x85ec, 0x01f5, 
            0x7c01, 0x002f, 0x89ec, 0x01f5, 0x7c01, 0x0036, 0x9811, 0x7c21, 
            0x816c, 0x85ec, 0x01f5, 0x7c41, 0x9700, 0x89ec, 0x01f5, 0x7c41, 
            0x4700, 0x7c10, 0x01e4, 0x7c21, 0x9000, 0x8011, 0x80ad, 0x2811, 
            0x80a1, 0x8422, 0x7c2d, 0x9010, 0x7dc1, 0x020e, 0xa81c, 0x7dc1, 
            0x000a, 0x80a1, 0x7c21, 0x9000, 0x7dc1, 0x020b, 0x81ed, 0x01f5, 
            0x7dc1, 0x01f6, 0x85e2, 0x04a6, 0x81ec, 0x04a7, 0x7dc1, 0x0239, 
            0x7801, 0x04a6, 0x7806, 0x002e, 0x800d, 0x7dc1, 0x0239, 0x85e2, 
            0x002d, 0x7dee, 0x002d, 0x03e7, 0x7de1, 0x002d, 0x03e7, 0x7c10, 
            0x027d, 0x7c21, 0x9000, 0x8011, 0x80ad, 0x2811, 0x80a1, 0x8422, 
            0x7c2d, 0x9010, 0x7dc1, 0x023c, 0x8001, 0x7c1c, 0x0025, 0x8801, 
            0x7c1c, 0x0026, 0x9001, 0x7c1c, 0x0027, 0x8401, 0x7c1c, 0x0028, 
            0x8c01, 0xa81c, 0x7c10, 0x02b8, 0x7c1c, 0x0066, 0x7c10, 0x02a9, 
            0x800d, 0x7c10, 0x025e, 0x80a1, 0x7dc1, 0x021e, 0x840c, 0x85e2, 
            0x04a4, 0x880c, 0x85e3, 0x04a4, 0x8c0c, 0x85e2, 0x04a5, 0x900c, 
            0x85e3, 0x04a5, 0xa9ee, 0x04a4, 0x81e1, 0x04a4, 0xa9ee, 0x04a5, 
            0x81e1, 0x04a5, 0xa1ee, 0x04a4, 0xa1e1, 0x04a4, 0xa1ee, 0x04a5, 
            0xa1e1, 0x04a5, 0x7c10, 0x046f, 0x61c1, 0x01a1, 0x05a1, 0x09a1, 
            0x7c01, 0x813b, 0x7821, 0x002d, 0x7c25, 0x0064, 0xa826, 0x7c11, 
            0x8f44, 0x0812, 0x802c, 0x7c11, 0x8f4e, 0x0481, 0x8402, 0x7821, 
            0x002d, 0xa825, 0xa826, 0x7c11, 0x8f44, 0x0812, 0x802c, 0x7c11, 
            0x8f4e, 0x0481, 0x8402, 0x7821, 0x002d, 0xa826, 0x7c11, 0x8f44, 
            0x0812, 0x802c, 0x7c11, 0x8f4e, 0x0481, 0x6021, 0x6011, 0x6001, 
            0x61c1, 0x01a1, 0x7801, 0x04a4, 0x7811, 0x04a5, 0xa414, 0x0402, 
            0x7c02, 0x04a8, 0x8482, 0x8886, 0x7c10, 0x0347, 0x6001, 0x61c1, 
            0x81ec, 0x04a7, 0x7c10, 0x042d, 0x85e1, 0x04a7, 0x01a1, 0x09a1, 
            0x7801, 0x04a4, 0x7811, 0x04a5, 0x0031, 0x0441, 0xa414, 0x0402, 
            0x0051, 0x7c52, 0x04a8, 0x7c02, 0x04f9, 0x84dc, 0x7dc1, 0x02d6, 
            0x808c, 0x7dc1, 0x02d9, 0xa88c, 0x7c10, 0x0410, 0x6021, 0x6001, 
            0x61c1, 0x8451, 0x8061, 0x7c71, 0x054a, 0x808e, 0x8462, 0x22ae, 
            0x8462, 0x846e, 0x61c1, 0x01a1, 0x05a1, 0x09a1, 0x7c01, 0x0607, 
            0x8c11, 0x7c21, 0x800e, 0x7c10, 0x05f0, 0x7c01, 0x060b, 0x8c11, 
            0x7c21, 0x802e, 0x7c10, 0x05f0, 0x6021, 0x6011, 0x6001, 0x8061, 
            0x7c10, 0x03ab, 0x806c, 0x7dc1, 0x0319, 0x7c10, 0x033b, 0x7c71, 
            0x054a, 0x8061, 0x80fc, 0x7dc1, 0x0312, 0x1c41, 0x7c43, 0x054a, 
            0x1001, 0x7c02, 0x04f9, 0xa445, 0x1c31, 0x7c33, 0x054a, 0xa436, 
            0x7c10, 0x03ab, 0x8472, 0x88fd, 0x7dc1, 0x0302, 0x806d, 0x7dc1, 
            0x02fd, 0x7c10, 0x0347, 0x7c01, 0x05ff, 0x8c11, 0x7c21, 0x800e, 
            0x7c10, 0x05f0, 0x7c01, 0x0603, 0x8c11, 0x7c21, 0x802e, 0x7c10, 
            0x05f0, 0x7c10, 0x032d, 0x7dc1, 0x02d6, 0x7c01, 0x04f9, 0x8061, 
            0x808c, 0x61c1, 0x8462, 0x8402, 0x7c6d, 0x0051, 0x7dc1, 0x0330, 
            0x85e1, 0x01f5, 0x61c1, 0x7c01, 0x059c, 0x7c11, 0x054a, 0x2091, 
            0x8081, 0x8402, 0x8412, 0x888d, 0x7dc1, 0x033f, 0x61c1, 0x8031, 
            0x8041, 0x8061, 0x7c01, 0x04f9, 0x7c51, 0x04a8, 0x7dc1, 0x0368, 
            0x8701, 0x7f1e, 0x7f21, 0x7f23, 0x7f25, 0x7f27, 0x7f29, 0x7f2b, 
            0x7f2d, 0x7f2f, 0x8701, 0x3b31, 0x8702, 0x7f1f, 0x7f22, 0x7f24, 
            0x7f26, 0x7f28, 0x7f2a, 0x7f2c, 0x7f2e, 0x7f30, 0x8702, 0x3b32, 
            0x0c11, 0x1021, 0x8814, 0x9c12, 0x8822, 0x7c24, 0x0020, 0x0812, 
            0x7c12, 0x8000, 0x2021, 0x84dd, 0x7dc1, 0x037c, 0x7c71, 0x0350, 
            0x2072, 0x7cfc, 0x8701, 0xac21, 0x7c71, 0x0350, 0x0872, 0x3c91, 
            0x8412, 0x7c71, 0x035c, 0x0872, 0x3c91, 0x8462, 0x8432, 0x8402, 
            0x8452, 0xa03e, 0x8442, 0xa03e, 0x8031, 0x19fe, 0x0051, 0x7dc1, 
            0x0368, 0x7c10, 0x0399, 0x7c10, 0x046f, 0x85e2, 0x0398, 0x61c1, 
            0x0000, 0x7811, 0x04a4, 0x7821, 0x04a5, 0x8814, 0x9c12, 0x8822, 
            0x7c24, 0x0020, 0x0812, 0x7c12, 0x8000, 0x25e1, 0x04a2, 0x8412, 
            0x25e1, 0x04a3, 0x61c1, 0x8451, 0x8432, 0x8402, 0x7c10, 0x0409, 
            0x8443, 0xa403, 0x7c10, 0x0409, 0x8433, 0x8403, 0x7c10, 0x0409, 
            0x8433, 0x8403, 0x7c10, 0x0409, 0x8442, 0xa402, 0x7c10, 0x0409, 
            0x8442, 0xa402, 0x7c10, 0x0409, 0x8432, 0x8402, 0x7c10, 0x0409, 
            0x8432, 0x8402, 0x7c10, 0x0409, 0x8433, 0x8443, 0xa803, 0x1481, 
            0x162e, 0x7c10, 0x03d4, 0x61c1, 0x8432, 0x8402, 0x808c, 0x7c10, 
            0x03fd, 0x8443, 0xa403, 0x808c, 0x7c10, 0x03fd, 0x8433, 0x8403, 
            0x808c, 0x7c10, 0x03fd, 0x8433, 0x8403, 0x808c, 0x7c10, 0x03fd, 
            0x8442, 0xa402, 0x808c, 0x7c10, 0x03fd, 0x8442, 0xa402, 0x808c, 
            0x7c10, 0x03fd, 0x8432, 0x8402, 0x808c, 0x7c10, 0x03fd, 0x8432, 
            0x8402, 0x808c, 0x7c10, 0x03fd, 0x61c1, 0x8462, 0xa03e, 0x61c1, 
            0xa04e, 0x61c1, 0x0011, 0x7c13, 0x04f9, 0x7c12, 0x059c, 0x8491, 
            0x61c1, 0xa03e, 0x61c1, 0xa04e, 0x61c1, 0xa88c, 0x8452, 0x61c1, 
            0x7c01, 0x060f, 0x8c11, 0x7c21, 0x800e, 0x7c10, 0x05f0, 0x7c01, 
            0x0613, 0x8c11, 0x7c21, 0x802e, 0x7c10, 0x05f0, 0x7c01, 0x0350, 
            0xa802, 0x7c81, 0x4c33, 0x7c01, 0x035c, 0xa802, 0x7c81, 0x4c34, 
            0x7c10, 0x0347, 0x89e1, 0x01f5, 0x61c1, 0x01a1, 0x05a1, 0x09a1, 
            0x0da1, 0x19a1, 0x8061, 0x85e2, 0x04a6, 0x7801, 0x04a6, 0x0004, 
            0xa406, 0x7811, 0x04a6, 0x7c12, 0x013d, 0x0414, 0xa416, 0x0021, 
            0x7823, 0x04a4, 0xa822, 0x8031, 0xa02e, 0x8432, 0x0ace, 0x8432, 
            0x0421, 0x7823, 0x04a5, 0xa822, 0xa02e, 0x8432, 0x0ace, 0x8432, 
            0x903c, 0x7dc1, 0x0433, 0xa414, 0x0402, 0x7c21, 0x04f9, 0x0022, 
            0xa8ac, 0x7dc1, 0x0433, 0xa8a1, 0x8462, 0x19ee, 0x00e0, 0x7dc1, 
            0x0433, 0x81e1, 0x04a6, 0x6061, 0x6031, 0x6021, 0x6011, 0x6001, 
            0x61c1, 0x7881, 0x04a2, 0x8402, 0x7881, 0x04a3, 0x61c1, 0x01a1, 
            0x05a1, 0x7801, 0x04a0, 0x7811, 0x04a1, 0x7c10, 0x0497, 0xa9ed, 
            0x04a0, 0x7c10, 0x0469, 0x7801, 0x04a4, 0x7811, 0x04a5, 0x7c10, 
            0x0497, 0x21e1, 0x04a2, 0x9087, 0x9088, 0x7c8a, 0x2a00, 0x8402, 
            0x21e1, 0x04a3, 0x9087, 0x9088, 0x7c8a, 0x2a00, 0x79e1, 0x04a0, 
            0x04a4, 0x79e1, 0x04a1, 0x04a5, 0x6011, 0x6001, 0x61c1, 0x8812, 
            0x8804, 0x9c02, 0x7c14, 0x0020, 0x0402, 0x7c02, 0x8000, 0x61c1, 
            0x000a, 0x000a, 0x000a, 0x000a, 0x0004, 0x0004, 0x0000, 0x0000, 
            0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 
            0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 
            0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 
            0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 
            0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 
            0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 
            0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 
            0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 
            0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 
            0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 
            0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 
            0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 
            0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 
            0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 
            0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 
            0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 
            0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 
            0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 
            0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 
            0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 
            0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 
            0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 
            0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 
            0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 
            0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 
            0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 
            0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 
            0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 
            0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 
            0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 
            0x0000, 0x0000, 0x0000, 0x0002, 0x0000, 0x0000, 0x0000, 0x0000, 
            0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 
            0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 
            0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 
            0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 
            0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 
            0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 
            0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 
            0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 
            0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 
            0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0002, 0x7dc1, 0x05ee, 
            0x19a1, 0x09a1, 0x8061, 0x20a1, 0x8422, 0x8402, 0x8462, 0x046e, 
            0x7dc1, 0x05fc, 0x7dc1, 0x05f3, 0x6021, 0x6061, 0x61c1, 0x6f03, 
            0x6f04, 0x6f05, 0x6f06, 0x6f07, 0x6f08, 0x6f09, 0x6f0a, 0x6f35, 
            0x6f36, 0x6f37, 0x6f38, 0x6f39, 0x6f3a, 0x6f3b, 0x6f3c, 0x6f3d, 
            0x6f3e, 0x6f3f, 0x6f40, 0x6f41, 0x6f42, 0x6f43, 0x6f44, 0x8720, 
            0x8720, 0x8720, 0x8720, 0x8720, 0x8720, 0x8720, 0x8720, 0x8720, 
            0x8720, 0x8720, 0x8720, 0x8720, 0x8720, 0x6f03, 0x6f04, 0x6f05, 
            0x6f06, 0x8720, 0x8720, 0x8720, 0x8720, 0x8720, 0x8720, 0x8720, 
            0x8720, 0x8720, 0x8720, 0x8720, 0x8720, 0x8720, 0x8720, 0x8720, 
            0x8720, 0x8720, 0x8720, 0x8720, 0x8720, 0x070b, 0x0711, 0x0711, 
            0x0711, 0x0711, 0x0711, 0x0711, 0x0711, 0x6f07, 0x6f08, 0x6f09, 
            0x6f0a, 0x0711, 0x0711, 0x0711, 0x0711, 0x0711, 0x0711, 0x0711, 
            0x070c, 0x8720, 0x8720, 0x8720, 0x8720, 0x8720, 0x8720, 0x8720, 
            0x8720, 0x8720, 0x8720, 0x8720, 0x8720, 0x070f, 0x8701, 0x8702, 
            0x8701, 0x8702, 0x8701, 0x8702, 0x8701, 0x8702, 0x8701, 0x8702, 
            0x8701, 0x8702, 0x8701, 0x8702, 0x8701, 0x8702, 0x8701, 0x8702, 
            0x0710, 0x8720, 0x8720, 0x8720, 0x8720, 0x8720, 0x8720, 0x8720, 
            0x8720, 0x8720, 0x8720, 0x8720, 0x8720, 0x070f, 0x8701, 0x8702, 
            0x8701, 0x8702, 0x8701, 0x8702, 0x8701, 0x8702, 0x8701, 0x8702, 
            0x8701, 0x8702, 0x8701, 0x8702, 0x8701, 0x8702, 0x8701, 0x8702, 
            0x0710, 0x8720, 0x8720, 0x8720, 0x8720, 0x8720, 0x8720, 0x8720, 
            0x8720, 0x8720, 0x8720, 0x8720, 0x8720, 0x070f, 0x8701, 0x8702, 
            0x8701, 0x8702, 0x8701, 0x8702, 0x8701, 0x8702, 0x8701, 0x8702, 
            0x8701, 0x8702, 0x8701, 0x8702, 0x8701, 0x8702, 0x8701, 0x8702, 
            0x0710, 0x8720, 0x8720, 0x8720, 0x8720, 0x8720, 0x8720, 0x8720, 
            0x8720, 0x8720, 0x8720, 0x8720, 0x8720, 0x070f, 0x8701, 0x8702, 
            0x8701, 0x8702, 0x8701, 0x8702, 0x8701, 0x8702, 0x8701, 0x8702, 
            0x8701, 0x8702, 0x8701, 0x8702, 0x8701, 0x8702, 0x8701, 0x8702, 
            0x0710, 0x8720, 0x8720, 0x8720, 0x8720, 0x8720, 0x8720, 0x8720, 
            0x8720, 0x8720, 0x8720, 0x8720, 0x8720, 0x070f, 0x8701, 0x8702, 
            0x8701, 0x8702, 0x8701, 0x8702, 0x8701, 0x8702, 0x8701, 0x8702, 
            0x8701, 0x8702, 0x8701, 0x8702, 0x8701, 0x8702, 0x8701, 0x8702, 
            0x0710, 0x8720, 0x8720, 0x8720, 0x8720, 0x8720, 0x8720, 0x8720, 
            0x8720, 0x8720, 0x8720, 0x8720, 0x8720, 0x070f, 0x8701, 0x8702, 
            0x8701, 0x8702, 0x8701, 0x8702, 0x8701, 0x8702, 0x8701, 0x8702, 
            0x8701, 0x8702, 0x8701, 0x8702, 0x8701, 0x8702, 0x8701, 0x8702, 
            0x0710, 0x8720, 0x8720, 0x8720, 0x8720, 0x8720, 0x8720, 0x8720, 
            0x8713, 0x8714, 0x8715, 0x8716, 0x8717, 0x070f, 0x8701, 0x8702, 
            0x8701, 0x8702, 0x8701, 0x8702, 0x8701, 0x8702, 0x8701, 0x8702, 
            0x8701, 0x8702, 0x8701, 0x8702, 0x8701, 0x8702, 0x8701, 0x8702, 
            0x0710, 0x8718, 0x8719, 0x871a, 0x871b, 0x871c, 0x8720, 0x8720, 
            0x871d, 0x8f20, 0x8f20, 0x8f20, 0x8710, 0x070f, 0x8701, 0x8702, 
            0x8701, 0x8702, 0x8701, 0x8702, 0x8701, 0x8702, 0x8701, 0x8702, 
            0x8701, 0x8702, 0x8701, 0x8702, 0x8701, 0x8702, 0x8701, 0x8702, 
            0x0710, 0x871d, 0x8f20, 0x8f20, 0x8f20, 0x8710, 0x8720, 0x8720, 
            0x870d, 0x8712, 0x8712, 0x8712, 0x870e, 0x070f, 0x8701, 0x8702, 
            0x8701, 0x8702, 0x8701, 0x8702, 0x8701, 0x8702, 0x8701, 0x8702, 
            0x8701, 0x8702, 0x8701, 0x8702, 0x8701, 0x8702, 0x8701, 0x8702, 
            0x0710, 0x870d, 0x8712, 0x8712, 0x8712, 0x870e, 0x8720, 0x8720, 
            0x8720, 0x8720, 0x8720, 0x8720, 0x8720, 0x070d, 0x0712, 0x0712, 
            0x0712, 0x0712, 0x0712, 0x0712, 0x0712, 0x0712, 0x0712, 0x0712, 
            0x0712, 0x0712, 0x0712, 0x0712, 0x0712, 0x0712, 0x0712, 0x0712, 
            0x070e, 0x8720, 0x8720, 0x8720, 0x8720, 0x8720, 0x8720, 0x8720, 
            0x8720, 0x8720, 0x8720, 0x8720, 0x8720, 0x070d, 0x0712, 0x0712, 
            0x0712, 0x0712, 0x0712, 0x0712, 0x0712, 0x0712, 0x0712, 0x0712, 
            0x0712, 0x0712, 0x0712, 0x0712, 0x0712, 0x0712, 0x0712, 0x0712, 
            0x070e, 0x8720, 0x8720, 0x8720, 0x8720, 0x8720, 0x8720, 0xff83, 
            0x45a9, 0x51b9, 0x7dff, 0xff01, 0x01c1, 0x7151, 0xc9c9, 0x49c9, 
            0xd171, 0xc101, 0x01ff, 0xff80, 0x8083, 0x8c89, 0x9292, 0x9290, 
            0x888c, 0x8380, 0x80ff, 0x0000, 0x0080, 0x8000, 0x0000, 0x0000, 
            0x0001, 0x0100, 0x0000, 0x0000, 0x55ff, 0xff00, 0x0000, 0xc080, 
            0xc080, 0x0101, 0x0101, 0x003c, 0x0888, 0xbc80, 0xbc80, 0xbc88, 
            0x90bc, 0x80bc, 0xaca4, 0x8028, 0x2414, 0x0000, 0x0084, 0xbc84, 
            0x80bc, 0x80bc, 0x8888, 0xbc80, 0xbcac, 0xa400, 0x0000, 0x0000, 
            0x00ff, 0xff01, 0x0101, 0x0101, 0x0101, 0x0000, 0x0000, 0xff01, 
            0x0149, 0x7d41, 0x0101, 0xff01, 0x0149, 0x6559, 0x0101, 0xff01, 
            0x0145, 0x5539, 0x0101, 0xff01, 0x011d, 0x117d, 0x0101, 0xff01, 
            0x015d, 0x5525, 0x0101, 0xff01, 0x0139, 0x5525, 0x0101, 0xff01, 
            0x0145, 0x350d, 0x0101, 0xff01, 0x0129, 0x5529, 0x0101, 0xff83, 
            0x010d, 0x0d7d, 0x0183, 0xff01, 0x5539, 0x6d39, 0x5501, 0xff01, 
            0x01e1, 0x3131, 0xa969, 0x69a9, 0x7171, 0xa101, 0x01ff, 0xff80, 
            0x8083, 0x8c88, 0x9096, 0x9690, 0x888c, 0x8380, 0x80ff, 0xff01, 
            0x01c1, 0x3151, 0xc949, 0x2999, 0x1131, 0xc101, 0x01ff, 0xff80, 
            0x8083, 0xcc88, 0xd0de, 0x9ed0, 0xc8cc, 0x8380, 0x80ff, 0x0048, 
            0x7c40, 0x0048, 0x6458, 0x0044, 0x5438, 0x001c, 0x107c, 0x005c, 
            0x5424, 0x0038, 0x5424, 0x0044, 0x340c, 0x0028, 0x5428, 0x0048, 
            0x5438, 0x0038, 0x4438, 0x0018, 0xe018, 0x0060, 0x9060, 0x0070, 
            0x80f0, 0x0000, 0x00f8, 0x8040, 0x80f8, 0x00e8, 0x00f0, 0x20e0, 
            0x0000, 0x00f8, 0x8080, 0x0060, 0x9060, 0x00b0, 0x90d0, 0x00f0, 
            0x90b0, 0x0000, 0x0101, 0xc3eb, 0xebeb, 0xebea, 0xe8e0, 0xc000, 
            0x80e0, 0xe0e8, 0xeaeb, 0xebeb, 0x0b01, 0x0101, 0x0000, 0x0001, 
            0x01e1, 0xebeb, 0xebeb, 0xeb0b, 0x0101, 0x0100, 0x0101, 0xc1eb, 
            0xebeb, 0xebea, 0xe8eb, 0xe3eb, 0x2b03, 0x0101, 0x0000, 0x0101, 
            0xc1eb, 0xebeb, 0xebeb, 0xeb43, 0xc1c1, 0x0303, 0x0b00, 0x0000, 
            0x0000, 0xf0f0, 0x3030, 0x0c0c, 0x0c0c, 0x0c0c, 0x0c0c, 0x3030, 
            0xf0f0, 0x0303, 0x3333, 0x0303, 0x0303, 0x0f0f, 0x0f0f, 0x0303, 
            0x0f0f, 0x0f0f, 0x0303, 0x1818, 0x1818, 0x7878, 0x7878, 0x1818, 
            0x7878, 0x4040, 0x6078, 0x7f61, 0x434f, 0x1f7f, 0x3f1f, 0x4f47, 
            0x417f, 0x7f7f, 0x7f7f, 0x7f6f, 0x4040, 0x0000, 0x0040, 0x4040, 
            0x787f, 0x7f7f, 0x7f7f, 0x4740, 0x4040, 0x0040, 0x6070, 0x7f71, 
            0x6347, 0x0f1f, 0x3f7f, 0x7f0f, 0x0000, 0x0000, 0x4040, 0x4060, 
            0x7f7f, 0x7f7f, 0x7f7f, 0x4340, 0x6073, 0x703c, 0x0600, 0x0000, 
            0xffff, 0x0000, 0x3030, 0xc3c3, 0xc0c0, 0xc0c0, 0x0303, 0x0000, 
            0x0000, 0xffff, 0x0000, 0x0f0f, 0x0c0c, 0x3030, 0x3030, 0x3030, 
            0x3030, 0x0c0c, 0x0f0f, 0x7878, 0x1818, 0xc0c0, 0xc0c0, 0xc0c0, 
            0xc0c0, 0xc0d0, 0xd4d4, 0xd2d2, 0xc2c6, 0xc6d6, 0x0601, 0x0000, 
            0x0216, 0xd6d6, 0xd6d6, 0xd6d2, 0x12d0, 0xd0d0, 0xd4d4, 0xd486, 
            0x0284, 0x4416, 0x0602, 0x0002, 0x02d6, 0xd6d6, 0xd6d6, 0xd686, 
            0x8282, 0x0616, 0x0000, 0x0002, 0x02d6, 0xd6d6, 0xd6d6, 0xd686, 
            0x8282, 0x0616, 0x0000, 0x0002, 0x02d6, 0xd6d6, 0xd6d6, 0xd602, 
            0x02d6, 0xd400, 0x0002, 0x02d6, 0xd6d6, 0xd6d6, 0xd686, 0x8282, 
            0x0616, 0x0000, 0x0000, 0x0202, 0xd6d6, 0xd6d6, 0xd6d6, 0x0606, 
            0x8454, 0xcfcf, 0xcfcf, 0xc0c0, 0xc0c0, 0xc0c0, 0xc0c0, 0x0303, 
            0x0303, 0xfb67, 0xcf8f, 0x9f9f, 0x9f7f, 0x7f1f, 0x0700, 0x0000, 
            0x0000, 0x0f7f, 0xffff, 0x3f1f, 0x0e03, 0x0f3f, 0xff7f, 0x3f0f, 
            0x0601, 0x0000, 0x0080, 0x8080, 0xf0ff, 0xffff, 0xffff, 0x87c0, 
            0xc3e7, 0xf00c, 0x0080, 0x8080, 0xf0ff, 0xffff, 0xffff, 0x87c0, 
            0xc3e7, 0xf00c, 0x0080, 0x80c0, 0xfeff, 0xffff, 0xffff, 0x8282, 
            0x0301, 0x0080, 0x8080, 0xf0ff, 0xffff, 0xffff, 0x87c0, 0xc3e7, 
            0xf00c, 0x0000, 0x8080, 0xc0fe, 0xffff, 0xffff, 0xbf81, 0x83ff, 
            0xfffe, 0xc3c3, 0x0f0f, 0x0000, 0x0303, 0x0303, 0x0000, 0x0000, 
            0x0000, 0x19a1, 0x8814, 0x8061, 0x20a1, 0x8402, 0x8462, 0x8422, 
            0x046c, 0x7dc1, 0x0975, 0x7dc1, 0x096c, 0x6061, 0x61c1
        };

        private static int stScreenRows = 12;
        private static int stScreenCols = 32;
        private static int stScreenBufferLoc = 0x8000;
        private static int stCharSetLoc = 0x8180;
        private static int stKeyboardLoc = 0x9000;
        private static int stCycleFreq = 100000;
        private static String stCodePath;
        private static V11.DCPU16Emulator myCPU;
        private static bool stMemDump =
#if DEBUG
            true;
#else
            false;
#endif

        private static VirtualDisplay myDisplay;
        private static bool myStopCPU;

        static void Main( String[] args )
        {
            if ( !ParseArgs( args ) )
                return;

            if ( stCodePath != null && !File.Exists( stCodePath ) )
            {
                Console.WriteLine( "No such file found at \"" + stCodePath + "\"" );
                return;
            }

            myCPU = new V11.DCPU16Emulator();

            if ( stCodePath == null )
                myCPU.LoadProgram( stDefaultProgram );
            else
                myCPU.LoadProgram( File.ReadAllBytes( stCodePath ) );

            myDisplay = new VirtualDisplay( myCPU, stScreenRows, stScreenCols, 4,
                (ushort) stScreenBufferLoc, (ushort) stCharSetLoc, (ushort) stKeyboardLoc );

            myStopCPU = false;

            Thread cpuThread = new Thread( CPUThreadEntry );
            cpuThread.Start();

            myDisplay.Run();
            myStopCPU = true;
        }

        private static void CPUThreadEntry()
        {
            while ( !myDisplay.Ready )
                Thread.Sleep( 10 );

            long cycles = 0;
            Stopwatch timer = new Stopwatch();
            timer.Start();

            while ( !myCPU.Exited && !myStopCPU )
            {
                cycles += myCPU.Step();
                if ( stCycleFreq > 0 )
                    Thread.Sleep( Math.Max( 0, (int) ( ( cycles * 1000 / stCycleFreq ) - timer.ElapsedMilliseconds ) ) );
            }
        }

        static bool ParseArgs( String[] args )
        {
            for ( int i = 0; i < args.Length; ++i )
            {
                String arg = args[ i ];
                if ( arg.StartsWith( "-" ) )
                {
                    switch ( arg.ToLower() )
                    {
                        case "-rows":
                            if ( !int.TryParse( args[ ++i ], out stScreenRows ) )
                            {
                                Console.WriteLine( "Invalid value for argument \"" + arg + "\"" );
                                return false;
                            }
                            break;
                        case "-cols":
                            if ( !int.TryParse( args[ ++i ], out stScreenCols ) )
                            {
                                Console.WriteLine( "Invalid value for argument \"" + arg + "\"" );
                                return false;
                            }
                            break;
                        case "-vidloc":
                            if ( !int.TryParse( args[ ++i ], out stScreenBufferLoc ) )
                            {
                                Console.WriteLine( "Invalid value for argument \"" + arg + "\"" );
                                return false;
                            }
                            break;
                        case "-keyloc":
                            if ( !int.TryParse( args[ ++i ], out stKeyboardLoc ) )
                            {
                                Console.WriteLine( "Invalid value for argument \"" + arg + "\"" );
                                return false;
                            }
                            break;
                        case "-freq":
                            if ( !int.TryParse( args[ ++i ], out stCycleFreq ) )
                            {
                                Console.WriteLine( "Invalid value for argument \"" + arg + "\"" );
                                return false;
                            }
                            break;
                        case "-memdump":
                            stMemDump = true;
                            break;
                        default:
                            Console.WriteLine( "Invalid argument \"" + arg + "\"" );
                            return false;
                    }
                }
                else if( stCodePath == null )
                {
                    stCodePath = arg;
                }
            }

            return true;
        }
    }
}
