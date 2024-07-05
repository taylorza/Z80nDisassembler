using System.Text;

namespace Z80nDisassembler
{
    public record Z80Instruction(int Address, byte[] Bytes, string Instruction, bool Lowercase)
    {
        public override string ToString()
        {
            var s = $"{Address:X4}\t{BytesToArray(Bytes),-16}{Instruction}";
            if (Lowercase) s = s.ToLower();
            return s;
        }

        private static string BytesToArray(byte[] bytes)
        {
            StringBuilder sb = new();
            foreach (byte b in bytes)
            {
                if (sb.Length > 0) sb.Append(' ');
                sb.Append($"{b:X2}");
            }
            return sb.ToString();
        }
    }

    public class Disassembler(int org, Stream stream, bool lowercase = true)
    {
        byte x;
        byte y;
        byte z;
        byte p;
        byte q;
        readonly List<byte> _instructionBytes = [];
        
        private byte GetOp()
        {
            var op = GetByte();
            x = (byte)(op >> 6);
            y = (byte)((op & 0b00111000) >> 3);
            z = (byte)(op & 0b00000111);
            p = (byte)(y >> 1);
            q = (byte)((y & 1));
            return op;
        }

        private string Index(int i, byte r)
        {
            if (i != 0 && r == 6)
            {
                sbyte d = GetSByte();
                return IndexDisp(i, r, d);
            }
            return $"{_tblR[i, r]}";            
        }

        private string IndexDisp(int i, byte r, sbyte d)
        {
            if (i != 0 && r == 6)
            {
                if (d == 0) return $"({_tblR[i, r]})";
                if (d > 0) return $"({_tblR[i, r]}+{d})";
                if (d < 0) return $"({_tblR[i, r]}{d})";
            }
            return $"{_tblR[i, r]}";
        }

        public Z80Instruction DisassembleInstruction()
        {
            var asm = new StringBuilder();
            var address = org;
            _instructionBytes.Clear();

            sbyte d;
            byte n;
            byte n2;
            ushort nn;

            int i = 0;
            
            bool valid = false;
            var op = GetOp();
            do
            {
                switch (op)
                {
                    case 0xdd:
                        op = GetOp();
                        switch (op)
                        {
                            case 0xdd:
                            case 0xed:
                            case 0xfd: asm.Append("nop"); break;                           
                            default: i = 1; valid = true; break;

                        }
                        break;

                    case 0xfd:
                        op = GetOp();
                        switch (op)
                        {
                            case 0xdd:
                            case 0xed:
                            case 0xfd: asm.Append("nop"); break;
                            default: i = 2; valid = true; break;

                        }
                        break;
                    default:
                        valid = true;
                        break;

                }
            } while (!valid);

            switch (op)
            {
                case 0xcb:
                    if (i == 0)
                    {
                        GetOp();
                        switch (x)
                        {
                            case 0: asm.Append($"{_tblROT[y]} {_tblR[0, z]}"); break;
                            case 1: asm.Append($"BIT {y}, {_tblR[0, z]}"); break;
                            case 2: asm.Append($"RES {y}, {_tblR[0, z]}"); break;
                            case 3: asm.Append($"SET {y}, {_tblR[0, z]}"); break;
                        }
                    }
                    else
                    {
                        d = GetSByte();
                        GetOp();
                        switch (x)
                        {
                            case 0:
                                if (z == 6)
                                {
                                    asm.Append($"{_tblROT[y]} {IndexDisp(i, z, d)}");
                                }
                                else
                                {
                                    asm.Append($"{_tblROT[y]} {IndexDisp(i, 6, d)}, {_tblR[0, z]}");
                                }
                                break;
                            case 1: asm.Append($"BIT {y}, {IndexDisp(i, 6, d)}"); break;
                            case 2:
                                if (z == 6)
                                {
                                    asm.Append($"RES {y}, {IndexDisp(i, z, d)}");
                                }
                                else
                                {
                                    asm.Append($"RES {y}, {IndexDisp(i, 6, d)}, {_tblR[0, z]}");
                                }
                                break;

                            case 3:
                                if (z == 6)
                                {
                                    asm.Append($"SET {y}, {IndexDisp(i, z, d)}");
                                }
                                else
                                {
                                    asm.Append($"SET {y}, {IndexDisp(i, 6, d)}, {_tblR[0, z]}");
                                }
                                break;
                        }
                    }
                    break;
                case 0xed:
                    op = GetOp();
                    switch (op)
                    {
                        case 0x23: asm.Append("SWAPNIB"); break;
                        case 0x24: asm.Append("MIRROR A"); break;
                        case 0x27: n = GetByte(); asm.Append($"TEST ${n:X2}"); break;
                        case 0x28: asm.Append($"BSLA DE, B"); break;
                        case 0x29: asm.Append($"BSRA DE, B"); break;
                        case 0x2a: asm.Append($"BSRL DE, B"); break;
                        case 0x2b: asm.Append($"BSRF DE, B"); break;
                        case 0x2c: asm.Append($"BSLC DE, B"); break;
                        case 0x30: asm.Append($"MUL D, E"); break;
                        case 0x31: asm.Append($"ADD HL, A"); break;
                        case 0x32: asm.Append($"ADD DE, A"); break;
                        case 0x33: asm.Append($"ADD BC, A"); break;
                        case 0x34: nn = GetWord(); asm.Append($"ADD HL, ${nn:X4}"); break;
                        case 0x35: nn = GetWord(); asm.Append($"ADD DE, ${nn:X4}"); break;
                        case 0x36: nn = GetWord(); asm.Append($"ADD BC, ${nn:X4}"); break;
                        case 0x8a: nn = GetWord(); asm.Append($"PUSH ${nn:X4}"); break;
                        case 0x90: asm.Append("OUTINB"); break;
                        case 0x91: n = GetByte(); n2 = GetByte(); asm.Append($"NEXTREG ${n:X2}, ${n2:X2}"); break;
                        case 0x92: n = GetByte(); asm.Append($"NEXTREG ${n:X2}, A"); break;
                        case 0x93: asm.Append("PIXELDN"); break;
                        case 0x94: asm.Append("PIXELAD"); break;
                        case 0x95: asm.Append("SETAE"); break;
                        case 0x98: asm.Append("JP (C)"); break;
                        case 0xa4: asm.Append("LDIX"); break;
                        case 0xa5: asm.Append("LDWS"); break;
                        case 0xac: asm.Append($"LDDX"); break;
                        case 0xb4: asm.Append($"LDIRX"); break;
                        case 0xb7: asm.Append($"LDPIRX"); break;
                        case 0xbc: asm.Append($"LDDRX"); break;
                        default:
                            switch (x)
                            {
                                case 0:
                                case 3:

                                    break;
                                case 1:
                                    switch (z)
                                    {
                                        case 0:
                                            if (y == 6)
                                                asm.Append("IN (C)");
                                            else
                                                asm.Append($"IN {_tblR[0, y]}, (C)");
                                            break;
                                        case 1:
                                            if (y == 6)
                                                asm.Append("OUT (C), 0");
                                            else
                                                asm.Append($"OUT (C), {_tblR[0, y]}");
                                            break;
                                        case 2:
                                            switch (q)
                                            {
                                                case 0: asm.Append($"SBC HL, {_tblRP[0, p]}"); break;
                                                case 1: asm.Append($"ADC HL, {_tblRP[0, p]}"); break;
                                            }
                                            break;
                                        case 3:
                                            switch (q)
                                            {
                                                case 0: nn = GetWord(); asm.Append($"LD (${nn:X4}), {_tblRP[i, p]}"); break;
                                                case 1: nn = GetWord(); asm.Append($"LD {_tblRP[i, p]}, (${nn:X4})"); break;
                                            }
                                            break;
                                        case 4: asm.Append("NEG"); break;
                                        case 5:
                                            if (y == 1)
                                                asm.Append("RETI");
                                            else
                                                asm.Append("RETN");
                                            break;
                                        case 6: asm.Append($"IM {_tblIM[y]}"); break;
                                        case 7:
                                            switch (y)
                                            {
                                                case 0: asm.Append("LD I, A"); break;
                                                case 1: asm.Append("LD R, A"); break;
                                                case 2: asm.Append("LD A, I"); break;
                                                case 3: asm.Append("LD A, I"); break;
                                                case 4: asm.Append("RRD"); break;
                                                case 5: asm.Append("RLD"); break;
                                                case 6: asm.Append("NOP"); break;
                                                case 7: asm.Append("NOP"); break;
                                            }
                                            break;
                                    }
                                    break;

                                case 2: // ED, X = 2
                                    if (z <= 3 && y >= 4)
                                        asm.Append($"{_matBLI[y-4, z]}");
                                    else
                                    {
                                        GetByte();
                                        asm.Append('?');
                                    }
                                    break;
                            }
                            break;
                    }
                    break;
                default:
                    switch (x)
                    {
                        case 0: // X = 0
                            switch (z)
                            {
                                case 0: // X = 0, Z = 0
                                    switch (y)
                                    {
                                        case 0: asm.Append("NOP"); break;
                                        case 1: asm.Append("EX AF, AF'"); break;
                                        case 2: d = GetSByte(); asm.Append($"DJNZ ${org + d:X4}"); break;
                                        case 3: d = GetSByte(); asm.Append($"JR ${org + d:X4}"); break;
                                        default: d = GetSByte(); asm.Append($"JR {_tblCC[y - 4]}, ${org + d:X4}"); break;
                                    }
                                    break;

                                case 1: // X = 0, Z = 1
                                    switch (q)
                                    {
                                        case 0: nn = GetWord(); asm.Append($"LD {_tblRP[i,p]}, ${nn:X4}"); break;
                                        case 1: asm.Append($"ADD {_tblRP[i, 2]}, {_tblRP[i,p]}"); break;
                                    }
                                    break;
                                case 2: // X = 0, Z = 2
                                    switch (q)
                                    {
                                        case 0:
                                            switch (p)
                                            {
                                                case 0: asm.Append("LD (BC), A"); break;
                                                case 1: asm.Append("LD (DE), A"); break;
                                                case 2: nn = GetWord(); asm.Append($"LD (${nn:X4}), {_tblRP[i,2]}"); break;
                                                case 3: nn = GetWord(); asm.Append($"LD (${nn:X4}), A"); break;
                                            }
                                            break;
                                        case 1:
                                            switch (p)
                                            {
                                                case 0: asm.Append("LD A, (BC)"); break;
                                                case 1: asm.Append("LD A, (DE)"); break;
                                                case 2: nn = GetWord(); asm.Append($"LD {_tblRP[i, 2]}, (${nn:X4})"); break;
                                                case 3: nn = GetWord(); asm.Append($"LD A, (${nn:X4})"); break;
                                            }
                                            break;
                                    }
                                    break;
                                case 3: // X = 0, Z = 3
                                    switch (q)
                                    {
                                        case 0: asm.Append($"INC {_tblRP[i, p]}"); break;
                                        case 1: asm.Append($"DEC {_tblRP[i, p]}"); break;
                                    }
                                    break;
                                case 4: asm.Append($"INC {_tblR[i, y]}"); break;
                                case 5: asm.Append($"DEC {_tblR[i, y]}"); break;
                                case 6: n = GetByte(); asm.Append($"LD {Index(i, y)}, ${n:X2}"); break;
                                case 7: // X = 0, Z = 7
                                    switch (y)
                                    {
                                        case 0: asm.Append("RLCA"); break;
                                        case 1: asm.Append("RRCA"); break;
                                        case 2: asm.Append("RLA"); break;
                                        case 3: asm.Append("RRA"); break;
                                        case 4: asm.Append("DAA"); break;
                                        case 5: asm.Append("CPL"); break;
                                        case 6: asm.Append("SCF"); break;
                                        case 7: asm.Append("CCF"); break;
                                    }
                                    break;
                            }
                            break;
                        case 1: // X = 1
                            if (z == 6 && y == 6)
                            {
                                asm.Append("HALT");
                            }
                            else
                            {                                    
                                asm.Append($"LD {Index(i, y)}, {Index(i, z)}");                            
                            }
                            break;
                        case 2: // X = 2
                            asm.Append($"{_tblALU[y]} {Index(i, z)}");
                            break;
                        case 3: // X = 3
                            switch (z)
                            {
                                case 0: asm.Append($"RET {_tblCC[y]}"); break;
                                case 1: // X = 3, Z = 1
                                    switch (q)
                                    {
                                        case 0: asm.Append($"POP {_tblRP2[i, p]}"); break;
                                        case 1:
                                            switch (p)
                                            {
                                                case 0: asm.Append("RET"); break;
                                                case 1: asm.Append("EXX"); break;
                                                case 2: asm.Append($"JP ({_tblRP[i, 2]})"); break;
                                                case 3: asm.Append($"LD SP, {_tblRP[i, 2]}"); break;
                                            }
                                            break;
                                    }
                                    break;
                                case 2: nn = GetWord(); asm.Append($"JP {_tblCC[y]}, ${nn:X4}"); break;
                                case 3: // X = 3, Z = 3
                                    switch (y)
                                    {
                                        case 0: nn = GetWord(); asm.Append($"JP ${nn:X4}"); break;
                                        case 1: break; // CB PREFIX
                                        case 2: n = GetByte(); asm.Append($"OUT (${n:X2}), A"); break;
                                        case 3: n = GetByte(); asm.Append($"IN A, (${n:X2})"); break;
                                        case 4: asm.Append("EX (SP), HL"); break;
                                        case 5: asm.Append("EX DE, HL"); break;
                                        case 6: asm.Append("DI"); break;
                                        case 7: asm.Append("EI"); break;
                                    }
                                    break;
                                case 4: nn = GetWord(); asm.Append($"CALL ${_tblCC[y]} ${nn:X4}"); break;
                                case 5: // X = 3, Z = 5
                                    switch (q)
                                    {
                                        case 0: asm.Append($"PUSH {_tblRP2[i, p]}"); break;
                                        case 1: // X = 3, Z = 5, Q = 1
                                            switch (p)
                                            {
                                                case 0: nn = GetWord(); asm.Append($"CALL ${nn:X4}"); break;
                                                case 1: break; // DD PREFIX
                                                case 2: break; // ED PREFIX
                                                case 3: break; // FD PREFIX
                                            }
                                            break;
                                    }
                                    break;
                                case 6: n = GetByte(); asm.Append($"{_tblALU[y]} ${n:X2}"); break;
                                case 7: asm.Append($"RST ${y * 8:X2}"); break;
                            }
                            break;
                    }
                    break;
            }
            
            return new Z80Instruction(address, [.. _instructionBytes], lowercase ? asm.ToString().ToLower() : asm.ToString(), lowercase);
        }

        private byte GetByte()
        {
            org++;
            var b = (byte)stream.ReadByte();
            _instructionBytes.Add(b);
            return b;
        }

        private sbyte GetSByte()
        {
            return (sbyte)GetByte();
        }

        private ushort GetWord()
        {
            return (ushort)(GetByte() | (GetByte() << 8));
        }

        private readonly string[,] _tblR =
        {
            { "B", "C", "D", "E", "H", "L", "(HL)", "A" },
            { "B", "C", "D", "E", "IXH", "IXL", "IX", "A" },
            { "B", "C", "D", "E", "IYH", "IYL", "IY", "A" },
        };
        private readonly string[,] _tblRP =
        {
            { "BC", "DE", "HL", "SP" },
            { "BC", "DE", "IX", "SP" },
            { "BC", "DE", "IY", "SP" },
        };

        private readonly string[,] _tblRP2 =
        {
            { "BC", "DE", "HL", "AF" },
            { "BC", "DE", "IX", "AF" },
            { "BC", "DE", "IY", "AF" },
        };


        private readonly string[] _tblCC = ["NZ", "Z", "NC", "C", "PO", "PE", "P", "M"];
        private readonly string[] _tblALU = ["ADD A,", "ADC A,", "SUB", "SBC A,", "AND", "XOR", "OR", "CP"];
        private readonly string[] _tblROT = ["RLC", "RRC", "RL", "RR", "SLA", "SRA", "SLL", "SRL"];
        private readonly string[] _tblIM = ["0", "0/1", "1", "2", "0", "0/1", "1", "2"];
        private readonly string[,] _matBLI = 
        { 
            { "LDI", "CPI", "INI", "OUTI" },
            { "LDD", "CPD", "IND", "OUTD" },
            { "LDIR", "CPIR", "INIR", "OTIR" },
            { "LDDR", "CPDR", "INDR", "OTDR" },
        };
    }
}
