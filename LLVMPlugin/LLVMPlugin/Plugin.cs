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

        private Assembler assembler;

        public void Loaded(Assembler assembler)
        {
            assembler.HandleCodeLine += new EventHandler<HandleCodeEventArgs>(assembler_HandleCodeLine);
            this.assembler = assembler;
        }

        void assembler_HandleCodeLine(object sender, HandleCodeEventArgs e)
        {
            // These things aren't really relevant to a DCPU-16 program, so
            // we just discard them so it doesn't throw an error.
            if (e.Code == ".text")
            {
                e.Handled = true;
                e.Output.CodeType = CodeType.Directive;
            }
            if (e.Code.StartsWith(".globl"))
            {
                e.Handled = true;
                e.Output.CodeType = CodeType.Directive;
            }
            if (e.Code == ".data")
            {
                e.Handled = true;
                e.Output.CodeType = CodeType.Directive;
            }
            if (e.Code.StartsWith(".short "))
            {
                var expression = assembler.ParseExpression(e.Code.Substring(7)); // TODO: Postpone evalulation?
                e.Output.Output = new[] { expression.Value };
                e.Output.CodeType = CodeType.Directive;
            }
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
