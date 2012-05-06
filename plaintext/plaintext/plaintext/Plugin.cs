using System;
using System.Text;
using Organic.Plugins;
using Organic;
using System.IO;

namespace _plaintext
{
    public class Plugin : IPlugin
    {
        public string Description
        {
            get { return "Outputs a plain text result of preproccessing"; }
        }
        
        private bool output;
        private string plaintextFile;

        public void Loaded(Assembler assembler)
        {
            assembler.AssemblyComplete += new EventHandler<AssemblyCompleteEventArgs>(assembler_AssemblyComplete);
            assembler.TryHandleParameter += new EventHandler<HandleParameterEventArgs>(assembler_TryHandleParameter);
            assembler.AddHelpEntry("plaintext\n" +
                "\t--plaintext: Outputs a plain text result of preproccessing");
        }

        void assembler_TryHandleParameter(object sender, HandleParameterEventArgs e)
        {
            if (e.Parameter == "--plaintext")
            {
                plaintextFile = e.Arguments[--e.Index];
                Console.WriteLine(plaintextFile);
                e.Handled = output = true;
            }
        }

        void assembler_AssemblyComplete(object sender, AssemblyCompleteEventArgs e)
        {
            // Creates a .dat file
            if (!output || e.Output.Count == 0)
                return;
            Console.WriteLine("Outputing to text file");
            string oldFile = e.Output[0].FileName;
            string code = "; =====Begin file: " + oldFile + "\n";

            // Get longest dat entry
            int maxLength = 0;
            foreach (var entry in e.Output)
            {
                if (!entry.Listed)
                    continue;
                if (entry.Output != null)
                {
                    if (entry.Output.Length != 0)
                    {
                        string dat = "dat ";
                        foreach (ushort value in entry.Output)
                            dat += "0x" + value.ToString("x") + ",";
                        dat = dat.Remove(dat.Length - 1);
                        if (dat.Length > maxLength && dat.Length < 30)
                            maxLength = dat.Length;
                    }
                }
            }

            foreach (var entry in e.Output)
            {
                if (!entry.Listed)
                    continue;
                if (entry.FileName != oldFile)
                {
                    code += "; =====Begin file: " + entry.FileName + "\n";
                    oldFile = entry.FileName;
                }
                if (entry.ErrorCode != ErrorCode.Success)
                    code += "; ERROR: " + ListEntry.GetFriendlyErrorMessage(entry.ErrorCode) + "\n";
                if (entry.WarningCode != WarningCode.None)
                    code += "; WARNING: " + ListEntry.GetFriendlyWarningMessage(entry.WarningCode) + "\n";

                if (entry.Output == null)
                    code += "; " + entry.Code + " (line " + entry.LineNumber + ")" + "\n";
                else
                {
                    if (entry.Output.Length != 0)
                    {
                        TabifiedStringBuilder tsb = new TabifiedStringBuilder();
                        string dat = "dat ";
                        foreach (ushort value in entry.Output)
                            dat += "0x" + value.ToString("x") + ",";
                        dat = dat.Remove(dat.Length - 1);
                        tsb.WriteAt(0, dat);
                        tsb.WriteAt(maxLength, "; " + entry.Code);
                        code += tsb.Value + "\n";
                    }
                }
            }
            StreamWriter file = new StreamWriter(plaintextFile);
            file.Write(code);
            file.Close();
        }

        public string Name
        {
            get { return "plaintext"; }
        }

        public Version Version
        {
            get { return new Version(1, 0); }
        }
    }
}
