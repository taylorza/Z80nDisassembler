using Z80nDisassembler;

namespace Z80nDisassembler_tests
{
    [TestClass]
    public class UnitTest1
    {
        private readonly Test[] tests =
        [
            new Test([0x00], "nop"),
            new Test([0x01, 0xef, 0xbe], "ld bc, $beef"),
            new Test([0x10, 0x05], "djnz $0007"),
            new Test([0x44], "ld b, h"),
            new Test([0x45], "ld b, l"),

            new Test([0xcb, 0x06], "rlc (hl)"),
            new Test([0xcb, 0x07], "rlc a"),
            new Test([0xcb, 0x0e], "rrc (hl)"),
            new Test([0xcb, 0x10], "rl b"),
            new Test([0xcb, 0x1b], "rr e"),
            new Test([0xcb, 0x20], "sla b"),
            new Test([0xcb, 0x30], "sll b"),
            new Test([0xcb, 0x36], "sll (hl)"),
            new Test([0xcb, 0x50], "bit 2, b"),
            new Test([0xcb, 0x90], "res 2, b"),
            new Test([0xcb, 0xd0], "set 2, b"),

            new Test([0xdd, 0x09], "add ix, bc"),
            new Test([0xdd, 0x21, 0xef, 0xbe], "ld ix, $beef"),
            new Test([0xdd, 0x22, 0xef, 0xbe], "ld ($beef), ix"),
            new Test([0xdd, 0x23], "inc ix"),
            new Test([0xdd, 0x24], "inc ixh"),
            new Test([0xdd, 0x25], "dec ixh"),

            new Test([0xed, 0x23], "swapnib"),
            new Test([0xed, 0x24], "mirror a"),
            new Test([0xed, 0x27, 0x05], "test $05"),
            new Test([0xed, 0x28], "bsla de, b"),
            new Test([0xed, 0x29], "bsra de, b"),
            new Test([0xed, 0x2a], "bsrl de, b"),
            new Test([0xed, 0x2b], "bsrf de, b"),
            new Test([0xed, 0x2c], "bslc de, b"),
            
            new Test([0xed, 0x30], "mul d, e"),
            new Test([0xed, 0x31], "add hl, a"),
            new Test([0xed, 0x32], "add de, a"),
            new Test([0xed, 0x33], "add bc, a"),
            new Test([0xed, 0x34, 0xef, 0xbe], "add hl, $beef"),
            new Test([0xed, 0x35, 0xef, 0xbe], "add de, $beef"),
            new Test([0xed, 0x36, 0xef, 0xbe], "add bc, $beef"),
            
            new Test([0xed, 0x8a, 0xef, 0xbe], "push $beef"),

            new Test([0xed, 0x90], "outinb"),
            new Test([0xed, 0x91, 0x56, 0x42], "nextreg $56, $42"),
            new Test([0xed, 0x92, 0x56], "nextreg $56, a"),
            new Test([0xed, 0x93], "pixeldn"),
            new Test([0xed, 0x94], "pixelad"),
            new Test([0xed, 0x95], "setae"),
            new Test([0xed, 0x98], "jp (c)"),

            new Test([0xed, 0xa4], "ldix"),
            new Test([0xed, 0xa5], "ldws"),
            new Test([0xed, 0xac], "lddx"),

            new Test([0xed, 0xb4], "ldirx"),
            new Test([0xed, 0xb7], "ldpirx"),
            new Test([0xed, 0xbc], "lddrx"),

            new Test([0xed, 0x54], "neg"),            
            new Test([0xed, 0x63, 0x34, 0x12], "ld ($1234), hl"),
            new Test([0xed, 0x69], "out (c), l"),
            new Test([0xed, 0x6b, 0x34, 0x12], "ld hl, ($1234)"),
            new Test([0xed, 0x70], "in (c)"),
            new Test([0xed, 0xb0], "ldir"),

            new Test([0xdd, 0xcb, 0x05, 0x06], "rlc (ix+5)"),
            new Test([0xdd, 0xcb, 0x05, 0x0e], "rrc (ix+5)"),
            new Test([0xdd, 0xcb, 0x05, 0x36], "sll (ix+5)"),
            new Test([0xdd, 0xcb, 0x05, 0x50], "bit 2, (ix+5)"),
            new Test([0xdd, 0xcb, 0x05, 0x90], "res 2, (ix+5), b"),
            new Test([0xdd, 0xcb, 0x05, 0xd0], "set 2, (ix+5), b"),

            new Test([0xfd, 0x09], "add iy, bc"),
            new Test([0xfd, 0x21, 0xef, 0xbe], "ld iy, $beef"),
            new Test([0xfd, 0x22, 0xef, 0xbe], "ld ($beef), iy"),
            new Test([0xfd, 0x23], "inc iy"),
            new Test([0xfd, 0x24], "inc iyh"),
            new Test([0xfd, 0x25], "dec iyh"),
            new Test([0xfd, 0xb4], "or iyh"),
            new Test([0xfd, 0xb5], "or iyl"),

            new Test([0xfd, 0xcb, 0x05, 0x06], "rlc (iy+5)"),
            new Test([0xfd, 0xcb, 0x05, 0x0e], "rrc (iy+5)"),
            new Test([0xfd, 0xcb, 0x05, 0x36], "sll (iy+5)"),
            new Test([0xfd, 0xcb, 0x05, 0x50], "bit 2, (iy+5)"),
            new Test([0xfd, 0xcb, 0x05, 0x90], "res 2, (iy+5), b"),
            new Test([0xfd, 0xcb, 0x05, 0xd0], "set 2, (iy+5), b"),
        ];

        [TestMethod]
        public void TestMethod1()
        {
            foreach (var test in tests)
            {
                var result = Disassemble(test.Mc, test.Expected);
                Assert.IsTrue(result.Item1, result.Item2);
            }
        }

        private static Tuple<bool, string> Disassemble(byte[] mc, string expected)
        {
            var i = new Disassembler(0, new MemoryStream(mc)).DisassembleInstruction();
            return new(i.Instruction == expected, i.ToString() + " != " + expected);
        }
    }

    record Test(byte[] Mc, string Expected);
}