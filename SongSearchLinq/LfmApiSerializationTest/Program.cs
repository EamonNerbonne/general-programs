using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EamonExtensionsLinq;
using EamonExtensionsLinq.Filesystem;
using System.IO;
using System.Xml.Serialization;
using LastFMspider.LfmApi;
using System.Xml;
namespace LfmApiSerializationTest
{
    class Program
    {
        static void Main(string[] args) {
            FileInfo getsimilarFI = new FileInfo(@"C:\Users\Eamon\eamonhome\docs-trunk\VS.NET2008\SongSpider\LastFMspider\track.getsimilar.xml");
            FileInfo getinfoFI = new FileInfo(@"C:\Users\Eamon\eamonhome\docs-trunk\VS.NET2008\SongSpider\LastFMspider\track.getinfo.xml");
            FileInfo gettoptagsFI = new FileInfo(@"C:\Users\Eamon\eamonhome\docs-trunk\VS.NET2008\SongSpider\LastFMspider\track.gettoptags.xml");
            XmlSerializer serializer = ApiTrackGetSimilar.MakeSerializer();
            XmlDocument doc = new XmlDocument();
            doc.Load(getsimilarFI.OpenRead());
            object o= serializer.Deserialize(getsimilarFI.OpenRead());
            serializer.Serialize(Console.Out, o);
            Console.ReadKey();
        }
    }
}
