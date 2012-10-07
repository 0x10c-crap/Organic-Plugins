using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DUnit
{
    public class UnitTest
    {
        public UnitTest(string Name, ushort Address)
        {
            this.Name = Name;
            this.Address = Address;
            Assersions = new List<Assertion>();
        }

        public string Name { get; set; }
        public ushort Address { get; set; }
        public ushort EndAddress { get; set; }
        public List<Assertion> Assersions { get; set; }
    }
}
