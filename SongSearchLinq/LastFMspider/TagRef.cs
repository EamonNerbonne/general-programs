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

        //semantically handy overrides:
        public override bool Equals(object obj) {            return obj != null && obj is TagRef && tag == ((TagRef)obj).tag;        }
        public override int GetHashCode() {            return tag.GetHashCode();        }
        public override string ToString() {            return tag;        }
    }
}
