using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BongTagger
{
    public class ViterbiObject
    {
        public Tag Tag1 { get; set; }
        public Tag Tag2 { get; set; }
        public Tag Tag3 { get; set; }
        public double Pi { get; set; }
    }
}
