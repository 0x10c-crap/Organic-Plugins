using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DUnit
{
    public class PreReq
    {
        public ushort Address { get; set; }
        public ushort EndAddress { get; set; }
        public string[] IncludedTests { get; set; }
    }
}
