using System.IO;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace EmnExtensions
{
    public class XmlSerializableBase<T>
        where T : XmlSerializableBase<T>
    {
        static readonly XmlSerializer serializer = new(typeof(T));

        public static T Deserialize(XmlReader from)
            => (T)serializer.Deserialize(from);

        public static T Deserialize(XDocument from)
        {
            using (var reader = from.CreateReader()) {
                return Deserialize(reader);
            }
        }

        public void SerializeTo(Stream s)
            => serializer.Serialize(s, this);

        public void SerializeTo(TextWriter w)
            => serializer.Serialize(w, this);

        public void SerializeTo(XmlWriter xw)
            => serializer.Serialize(xw, this);
    }
}
