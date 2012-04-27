using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using orgASM.Plugins;
using orgASM;
using System.Net;
using System.IO;
using System.Windows.Forms;

namespace _0x10co.de
{
    public class Plugin : IPlugin
    {
        public string Description
        {
            get { return "Uploads output to 0x10co.de"; }
        }

        private bool upload;

        public void Loaded(Assembler assembler)
        {
            assembler.AssemblyComplete += new EventHandler<AssemblyCompleteEventArgs>(assembler_AssemblyComplete);
            assembler.TryHandleParameter += new EventHandler<HandleParameterEventArgs>(assembler_TryHandleParameter);
            assembler.AddHelpEntry("0x10co.de:\n" +
                "\t--0x10co.de: Upload the output to 0x10co.de automatically.");
        }

        void assembler_TryHandleParameter(object sender, HandleParameterEventArgs e)
        {
            if (e.Parameter == "--0x10co.de")
                e.Handled = upload = true;
        }

        void assembler_AssemblyComplete(object sender, AssemblyCompleteEventArgs e)
        {
            // uploads a .dat file
            if (!upload || e.Output.Count == 0)
                return;
            Console.WriteLine("Uploading output to 0x10co.de...");
            string code = "";
            foreach (var entry in e.Output)
            {
                if (entry.Output == null)
                    continue;
                if (entry.Output.Length != 0)
                {
                    string dat = "dat ";
                    foreach (ushort value in entry.Output)
                    {
                        dat += "0x" + value.ToString("x") + ",";
                    }
                    dat = dat.Remove(dat.Length - 1) + "\t; " + entry.Code;
                    code += dat + "\n";
                }
            }
            HttpWebRequest hwr = (HttpWebRequest)WebRequest.Create(new Uri("http://0x10co.de"));
            hwr.ContentType = "application/x-www-form-urlencoded";
            hwr.Method = "POST";
            string encodedCode = "";
            while (code.Length != 0)
            {
                if (code.Length > 10000)
                {
                    encodedCode += Uri.EscapeDataString(code.Remove(10000));
                    code = code.Remove(10000);
                }
                else
                {
                    encodedCode += Uri.EscapeDataString(code);
                    code = "";
                }
            }
            using (StreamWriter writer = new StreamWriter("test.txt"))
                writer.Write(encodedCode);
            string postData = "title=" + Uri.EscapeDataString(e.Output.First().FileName) + "&author=&description=Created+by+the+0x10co.de+.orgASM+plugin&password=&code=" + encodedCode;
            byte[] data = Encoding.ASCII.GetBytes(postData);
            hwr.ContentLength = data.Length;
            Stream s = hwr.GetRequestStream();
            s.Write(data, 0, data.Length);
            s.Close();

            HttpWebResponse resp = (HttpWebResponse)hwr.GetResponse();
            s = resp.GetResponseStream();
            MemoryStream ms = new MemoryStream();
            int b = 0;
            while ((b = s.ReadByte()) != -1)
                ms.WriteByte((byte)b);

            string url = Encoding.ASCII.GetString(ms.GetBuffer()).Trim(' ', '\t', '\0');
            Console.WriteLine("Uploaded to http://0x10co.de" + url + ".  This URL has been copied to the clipboard.");
            Clipboard.SetText("http://0x10co.de" + url);
        }

        public string Name
        {
            get { return "0x10co.de"; }
        }

        public Version Version
        {
            get { return new Version(1, 0); }
        }
    }
}
