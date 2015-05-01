using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NayuTerm.Models
{
    public class Alias
    {
        public string Before { get; private set; }
        public string After { get; private set; }

        public Alias(string before, string after)
        {
            Before = before;
            After = after;
        }
    }
}
