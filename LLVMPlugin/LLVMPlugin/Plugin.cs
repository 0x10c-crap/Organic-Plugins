using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Organic.Plugins;
using Organic;

namespace LLVMPlugin
{
    public class Plugin : IPlugin
    {
        public string Description
        {
            get { return "Adds support for llvm-dcpu16 directives and assembly."; }
        }

        public void Loaded(Assembler assembler)
        {
            assembler.HandleCodeLine += new EventHandler<HandleCodeEventArgs>(assembler_HandleCodeLine);
        }

        void assembler_HandleCodeLine(object sender, HandleCodeEventArgs e)
        {
            // These things aren't really relevant to a DCPU-16 program, so
            // we just discard them so it doesn't throw an error.
            if (e.Code == ".text")
                e.Handled = true;
            if (e.Code.StartsWith(".globl"))
                e.Handled = true;
        }

        public string Name
        {
            get { return "llvm"; }
        }

        public Version Version
        {
            get { return new Version(1, 0); }
        }
    }
}
