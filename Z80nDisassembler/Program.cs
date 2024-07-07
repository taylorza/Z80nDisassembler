using Microsoft.Extensions.Configuration;
using System.Globalization;

namespace Z80nDisassembler
{
    internal class Program
    {
        const string Version = "0.1a";
        static void Main(string[] args)
        {
            int org = 0;
            if (args.Length < 1)
            {
                ShowUsage();
            }
            
            var switchMapping = new Dictionary<string, string>()
            {
                {"-org", "org"}
            };

            IConfiguration config = new ConfigurationBuilder().AddCommandLine(args, switchMapping).Build();

            var sOrg = config["org"];
            if (sOrg != null)
            {
                ReadOnlySpan<char> span = sOrg.AsSpan();
                NumberStyles numberStyles = NumberStyles.Integer;

                if (sOrg.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                {
                    span = sOrg.AsSpan(2);
                    numberStyles = NumberStyles.HexNumber;
                }
                if (!int.TryParse(span, numberStyles, CultureInfo.CurrentCulture, out org))
                {
                    Console.Error.WriteLine("Error: Invalid address");
                    ShowUsage();
                }
            }

            string binfile = string.Empty;
            foreach (var arg in args)
            {
                if (!arg.StartsWith('-'))
                {
                    binfile = arg;
                    break;
                }
            }
            
            if (string.IsNullOrEmpty(binfile))
            {
                Console.Error.WriteLine("Error: Missing binary file input");
                ShowUsage();
            }
            var asmfile = Path.ChangeExtension(Path.GetFileName(binfile), "asm");

            try
            {
                using var fs = new FileStream(binfile, FileMode.Open, FileAccess.Read);
                using var os = new FileStream(asmfile, FileMode.Create, FileAccess.Write);
                using var wr = new StreamWriter(os);
                var d = new Disassembler(org, fs, true);
                wr.WriteLine($"; Disassembly of \"{binfile}\"");
                wr.WriteLine($"; Created with Z80n Disassembler {Version}");
                wr.WriteLine($"; On {DateTime.Now:f}");
                wr.WriteLine();
                while (fs.Position < fs.Length)
                {
                    wr.WriteLine($"{d.DisassembleInstruction()}");
                }
            }
            catch (FileNotFoundException ex)
            {
                Console.Error.WriteLine("Error: {0}", ex.Message);   
            }
        }

        static void ShowUsage()
        {
            Console.WriteLine("Z80n Disassembler v0.1");
            Console.WriteLine("Usage:");
            Console.WriteLine("  Z80nDisassembler [-org=<baseaddress>] <binfile>");
            Console.WriteLine();
            Console.WriteLine("org : Sets the base address used for the disassembly");
            Console.WriteLine();
            Console.WriteLine("Examples:");
            Console.WriteLine("  Z80nDisassembler app.rom");
            Console.WriteLine("  Z80nDisassembler -org=0xC000 game.rom ");
            Environment.Exit(-1);
        }
    }
}
