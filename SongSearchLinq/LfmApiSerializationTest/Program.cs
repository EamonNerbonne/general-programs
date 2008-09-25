﻿using System;
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
            XmlSerializer serializerTT = ApiTrackGetTopTags.MakeSerializer();
            XmlSerializer serializerST = ApiTrackGetSimilar.MakeSerializer();
            XmlSerializer serializerTI = ApiTrackGetInfo.MakeSerializer();
//            XmlDocument doc = new XmlDocument();
 //           doc.Load(getsimilarFI.OpenRead());
            object o;
            o= serializerTT.Deserialize(gettoptagsFI.OpenRead());
            serializerTT.Serialize(Console.Out, o);
            Console.ReadKey();

            o = serializerST.Deserialize(getsimilarFI.OpenRead());  
            serializerST.Serialize(Console.Out, o);
            Console.ReadKey();
            
            o = serializerTI.Deserialize(getinfoFI.OpenRead());
            serializerTI.Serialize(Console.Out, o);
            Console.ReadKey();
        }
    }
}
