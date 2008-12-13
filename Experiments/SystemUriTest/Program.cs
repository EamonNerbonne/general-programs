using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SystemUriTest
{
    class Program
    {
        static void Main(string[] args) {
            Uri a = new Uri("http://microsoft.com/dir/test.../file.txt");
            Uri d = new Uri("http://microsoft.com/dir/test%2E%2E%2E/file.txt");
            Uri e = new Uri("http://microsoft.com/dir/test/file.txt");


            Console.WriteLine("{0}", string.Join(", ", new[] { a,  d }.Select(uri => (uri == e).ToString()).ToArray()));
                

        }
    }
}
