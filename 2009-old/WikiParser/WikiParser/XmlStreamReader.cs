using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Linq;

namespace WikiParser
{
        public static class XmlStreamReader
        {
            public struct XmlElementInfo
            {
                private readonly XmlReader r;
                public string QualifiedName { get { return r.Name; } }
                public string LocalName { get { return r.LocalName; } }
                public string NamespaceURI { get { return r.NamespaceURI; } }
                public string NamespacePrefix { get { return r.Prefix; } }
                public bool IsEmpty { get { return r.IsEmptyElement; } }
                public int AttributeCount { get { return r.AttributeCount; } }
                public XmlElementInfo(XmlReader r) { this.r = r;}
            }
            /// <summary>
            /// This method reads all xml elements from the xmlreader of the given name.  It ignores others. 
            /// Each element is read in its entirety, but the separate elements are read in a streaming, 
            /// sequential fashion; so that makes this method usuable for parsing very large xml documents
            /// which consist of managebly-sized xml elements.
            /// 
            /// The code snippet is inspired by:
            /// http://blogs.msdn.com/xmlteam/archive/2007/03/24/streaming-with-linq-to-xml-part-2.aspx
            /// </summary>
            public static IEnumerable<XElement> StreamElements(this XmlReader reader, Func<XmlElementInfo,bool> elementFilter) {
                while (reader.Read())
                    if(reader.NodeType == XmlNodeType.Element
                        && elementFilter(new XmlElementInfo(reader)))
                    yield return (XElement)XElement.ReadFrom(reader);
            }
        }
}
