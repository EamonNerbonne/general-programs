using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LastFMspider
{
    public class TagRef
    {
        private string tag;
        public string Tag { get { return tag; } }

        public TagRef(string tag)
        {
            this.tag = tag;
        }
    }
}
