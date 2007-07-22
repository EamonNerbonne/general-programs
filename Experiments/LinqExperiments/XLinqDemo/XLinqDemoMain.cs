using System;
using System.Collections.Generic;
using System.Text;
//using System.Query;
using System.Xml.Linq;
using System.Data;
using EamonExtensions.DebugTools;
using System.Linq;
using System.IO;

namespace XLinqDemo {
    class XLinqDemoMain {
        static void Main(string[] args) {
            DateTime start = System.DateTime.Now;

            var rootElementsXml = 
                from xmlfile in new DirectoryInfo(args[0]).GetFiles()
                where xmlfile.Extension.ToLower() == ".xml"
                select XElement.Load(xmlfile.FullName);

            var textNodesXml = rootElementsXml.Elements("Message").Elements("Text").Nodes();

            var textToMeFreq = 
                from text in textNodesXml
                where text.Parent.Parent.Element("From").Element("User")
                          .Attribute("FriendlyName").Value != "Eamon"
                let textsaid = text.ToString()
                group text by textsaid into textOccurances
                let count = textOccurances.Count()
                select new {Count = count, Text = textOccurances.Key};

            var msnByFrequency =
                from textAndCount in textToMeFreq
                where textAndCount.Count >= 10
                orderby textAndCount.Count descending 
                select textAndCount;

            foreach(var textset in msnByFrequency) {
                Console.WriteLine("\"{0}\" was said {1} times.",textset.Text,textset.Count);
            }

            DateTime end = System.DateTime.Now;
            Console.WriteLine("Time Taken: " + (end - start));
            Console.WriteLine("==============Finished Executing================");
            Console.ReadLine();

        }
    }
}
