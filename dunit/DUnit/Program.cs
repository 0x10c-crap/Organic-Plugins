using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Tomato;
using System.Globalization;

namespace DUnit
{
    public class Program
    {
        public static readonly Version Version = new Version(0, 1);

        const string OrganicDownloadUrl = "https://github.com/downloads/SirCmpwn/organic/Organic.exe";

        static void Main(string[] args)
        {
            DisplaySplash();
            string testFile = null;
            List<string> TestCases = new List<string>();
            // Interpret arguments
            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];
                if (arg.StartsWith("-"))
                {
                    switch (arg)
                    {
                        case "--help":
                        case "-?":
                        case "-h":
                        case "/?":
                        case "/h":
                            DisplayHelp();
                            break;
                    }
                }
                else
                {
                    if (testFile == null)
                        testFile = arg;
                    else
                    {
                        switch (arg)
                        {
                            case "run":
                                TestCases.AddRange(args[++i].Split(','));
                                break;
                            case "debug":
                                TestCases.AddRange(args[++i].Split(','));
                                break;
                            default:
                                return;
                        }
                    }
                }
            }
            if (testFile == null)
            {
                Console.WriteLine("No test file specified.");
                return;
            }
            DCPU CPU = new DCPU();
            List<UnitTest> Tests = new List<UnitTest>();
            List<PreReq> PreReqs = new List<PreReq>();
            using (Stream stream = File.OpenRead(testFile))
            {
                byte[] lengthData = new byte[4];
                stream.Read(lengthData, 0, 4);
                int length = BitConverter.ToInt32(lengthData, 0);
                byte[] section1 = new byte[length];
                stream.Read(section1, 0, section1.Length);

                lengthData = new byte[4];
                stream.Read(lengthData, 0, 4);
                length = BitConverter.ToInt32(lengthData, 0);
                byte[] section2 = new byte[length];
                stream.Read(section2, 0, section2.Length);

                lengthData = new byte[4];
                stream.Read(lengthData, 0, 4);
                length = BitConverter.ToInt32(lengthData, 0);
                byte[] section3 = new byte[length];
                stream.Read(section3, 0, section3.Length);

                string testDefs = Encoding.ASCII.GetString(section1);
                for (int i = 0; i < section2.Length; i += 2)
                    CPU.Memory[i / 2] = (ushort)(section2[i] << 16 | section2[i + 1]);
                // TODO: Listing

                string[] defs = testDefs.Split('\n');
                foreach (var test in defs)
                {
                    string[] parts = test.Split(' ');
                    if (test.StartsWith("PREREQ "))
                    {
                        PreReq pre = new PreReq();
                        pre.Address = ushort.Parse(parts[1], NumberStyles.HexNumber);
                        pre.IncludedTests = parts[2].Split(',');
                        if (PreReqs.Count != 0)
                            PreReqs[PreReqs.Count - 1].EndAddress = (ushort)(pre.Address - 1);
                        PreReqs.Add(pre);
                    }
                    else if (test.StartsWith("TEST "))
                    {
                        string[] range = parts[1].Split('-');
                        UnitTest uTest = new UnitTest(parts[2], ushort.Parse(range[0], NumberStyles.HexNumber));
                        uTest.EndAddress = ushort.Parse(range[1], NumberStyles.HexNumber);
                        Tests.Add(uTest);
                    }
                }
            }
        }

        static void DisplaySplash()
        {
            Console.WriteLine("DUnit DCPU-16 Unit Testing Tool  Copyright Drew DeVault 2012");
        }

        static void DisplayHelp()
        {
            Console.WriteLine("\nDUnit is a unit testing framework for DCPU-16.\n" + 
                "This executable is both a standalone tool, and a plugin for the Organic\n" +
                "assembler.  If you don't already have Organic in the same directory, use\n" +
                "--organic to download it.\n" + 
                "It is also a plugin for Lettuce.  If you don't already have Lettuce in the same\n" +
                "directory, use --lettuce to download it.\n\n" +
                
                "===Organic Usage===\n" +
                "Run \"organic.exe --help\" for more information.\n" +
                "Using --export-tests testfile.unit will cause the plutin to output a unit test\n" +
                "file, which can be loaded with DUnit.\n\n" +

                "===Lettuce Usage===\n" + 
                "Run \"lettuce.exe --help\" for more information.\n" + 
                "A new menu option called \"Tests\" will be added to the debugger to work with\n" + 
                "unit tests.\n\n" +

                "===DUnit Usage===\n" +
                "dunit.exe testfile.unit [options] [command] [parameters]\n" +

                "---Options\n" +
                "--create-report [directory]: Creates an HTML-based report of the test results.\n" +
                "--help: Displays this message.\n" +

                "---Commands\n" +
                "run [test(s)|all]: Runs the specified tests (comma delimited).  Examples:\n" +
                    "\trun all: Runs all tests in the unit file\n" +
                    "\trun test1,test2: Runs test1 and test2.\n" +
                "debug [test(s)|all]: Runs the specified tests (comma delimited) with Lettuce.");
        }
    }
}
