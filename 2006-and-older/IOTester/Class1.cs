using System;
using System.IO;
namespace IOTester {
    class Class1 {
        [STAThread]
        static void Main(string[] args) {
            Stream stream=new MemoryStream();
            BinaryWriter writer=new BinaryWriter(stream);
            writer.Write(65536);
            writer.Flush();
            stream.Seek(0,SeekOrigin.Begin);
            int val;
            while ((val=stream.ReadByte())!=-1)
                Console.Out.Write(" "+val);
        }
    }
}
