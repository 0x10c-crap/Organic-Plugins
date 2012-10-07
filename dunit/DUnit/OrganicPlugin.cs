using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Organic.Plugins;
using Organic;
using System.IO;

namespace DUnit
{
    public class OrganicPlugin : IPlugin
    {
        string TestFile;
        string CurrentSection;
        string CurrentTest;
        Assembler Assembler;
        List<UnitTest> Tests;
        bool IsInTest;

        public string Description
        {
            get { return "DUnit is a unit test framework for DCPU-16."; }
        }

        public void Loaded(Assembler assembler)
        {
            assembler.AddHelpEntry("DUnit Unit Test Tool:\n" +
                "Use --export-tests [file] to create a test file.\n" +
                "DUnit adds a number of directives for your use.  They are:\n" +
                ".prereq [tests]: Defines a section of code as being required before executing\n" +
                    "\tthe specified tests (ALL, NONE, or a comma-delimited list of names.\n" +
                ".test [name]: Creates a unit test with the given name.\n" +
                ".endtest: Closes the test block from the matching .test directive.\n" +
                ".assert: Only valid within a .test block.  You may use register names and\n" +
                    "\tmemory locations to check values at test-time.  For instance:\n" +
                    "\t.assert [0x8012+A]==[screen_buffer]\n" +
                "All tests will be removed from the normal Organic output.");
            assembler.TryHandleParameter += new EventHandler<HandleParameterEventArgs>(assembler_TryHandleParameter);
            assembler.AssemblyComplete += new EventHandler<AssemblyCompleteEventArgs>(assembler_AssemblyComplete);
            assembler.HandleCodeLine += new EventHandler<HandleCodeEventArgs>(assembler_HandleCodeLine);
            CurrentSection = "NONE";
            Assembler = assembler;
            Tests = new List<UnitTest>();
        }

        void assembler_HandleCodeLine(object sender, HandleCodeEventArgs e)
        {
            // Handle custom directives
            if (e.Code.ToLower().StartsWith(".prereq"))
            {
                CurrentSection = e.Code.Substring(7).Trim();
                e.Handled = true;
            }
            else if (e.Code.ToLower().StartsWith(".test"))
            {
                if (IsInTest)
                {
                    e.Handled = true;
                    // Error
                }
                string code = e.Code;
                if (TestFile == null)
                    Assembler.noList = true;
                else
                    e.Code = "SET PC, end_test_" + e.Code.Substring(5).Trim(); // change the code to jump past the test under normal conditions
                Tests.Add(new UnitTest(code.Substring(5).Trim(), Assembler.currentAddress));
                IsInTest = true;
            }
            else if (e.Code.ToLower().StartsWith(".endtest"))
            {
                if (!IsInTest)
                    e.Output.ErrorCode = ErrorCode.UncoupledStatement;
                else
                    Tests[Tests.Count - 1].EndAddress = Assembler.currentAddress;
                e.Code = "end_test_" + Tests[Tests.Count - 1].Name + ":";
                IsInTest = false;
            }
            else if (e.Code.ToLower().StartsWith(".assert"))
            {
                Tests[Tests.Count - 1].Assersions.Add(new Assertion()
                {
                    Address = Assembler.currentAddress,
                    Expression = e.Code.Substring(7).Trim()
                });
                e.Handled = true;
            }
            else if (e.Code.ToLower().StartsWith(".dump"))
            {
                // TODO
            }
            else if (e.Code.ToLower().StartsWith(".log"))
            {
                // TODO
            }

            e.Output.Tags["dunit-section"] = CurrentSection;
        }

        void assembler_AssemblyComplete(object sender, AssemblyCompleteEventArgs e)
        {
            // Create a test file
            if (TestFile == null)
                return;
            using (StreamWriter writer = new StreamWriter(File.Open(TestFile, FileMode.Open), Encoding.ASCII))
            {
                CurrentSection = "NONE";
                string unitOut = "ASSEMBLER ORGANIC\n";
                foreach (var entry in e.Output)
                {
                    if (entry.Tags.ContainsKey("dunit-section"))
                    {
                        if (entry.Tags["dunit-section"].ToString() != CurrentSection)
                        {
                            unitOut += "PREREQ " + entry.Address.ToString("x").ToUpper() + " " + entry.Tags["dunit-section"].ToString() + "\n";
                            CurrentSection = entry.Tags["dunit-section"].ToString();
                        }
                    }
                }
                foreach (var test in Tests)
                {
                    unitOut += "TEST " + test.Address.ToString("x").ToUpper() + "-" + test.EndAddress.ToString("x") + " " + test.Name + "\n";
                    foreach (var assert in test.Assersions)
                        unitOut += "ASSERT " + assert.Address.ToString("x").ToUpper() + " " + assert.Expression + "\n";
                }
                unitOut = unitOut.Remove(unitOut.Length - 1); // Remove trailing \n
                writer.BaseStream.Write(BitConverter.GetBytes(Encoding.ASCII.GetByteCount(unitOut)), 0, sizeof(int));
                writer.Flush();
                writer.Write(unitOut);
                ushort[] binOutput = new ushort[0];
                foreach (var entry in e.Output)
                {
                    if (entry.Output != null)
                        binOutput = binOutput.Concat(entry.Output).ToArray();
                }
                writer.Flush();
                writer.BaseStream.Write(BitConverter.GetBytes(binOutput.Length * 2), 0, sizeof(int));
                foreach (var value in binOutput)
                {
                    byte[] buffer = BitConverter.GetBytes(value);
                    Array.Reverse(buffer);
                    writer.BaseStream.Write(buffer, 0, buffer.Length);
                }
                string listing = Assembler.CreateListing(e.Output);
                writer.BaseStream.Write(BitConverter.GetBytes(Encoding.ASCII.GetByteCount(listing)), 0, sizeof(int));
                writer.Write(listing);
            }
        }

        void assembler_TryHandleParameter(object sender, HandleParameterEventArgs e)
        {
            if (e.Parameter.StartsWith("--export-tests"))
            {
                TestFile = e.Arguments[++e.Index];
                e.Handled = true;
            }
        }

        public string Name
        {
            get { return "DUnit"; }
        }

        public Version Version
        {
            get { return Program.Version; }
        }
    }
}
