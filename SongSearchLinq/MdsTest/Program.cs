using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using hitmds;

namespace MdsTest
{
    class Program
    {
        struct MdsPoint2D
        {
            public double x, y;
            public double DistanceTo(MdsPoint2D other) {
                return Math.Sqrt((x - other.x) * (x - other.x) + (y - other.y) * (y - other.y));
            }
        }
        static void Main(string[] args) {

            var points = (
                from i in Enumerable.Range(0,10)
                from j in Enumerable.Range(0,10)
                select  new MdsPoint2D{x=i,y=j}
                ).ToArray();

            Random r = new Random();

            using (Hitmds mds = new Hitmds(points.Length,2, (i,j) => (float)(points[i].DistanceTo(points[j]) + r.NextDouble())) ) {
                mds.mds_train(points.Length * 5000, 1.0, (i, j) => { });

                foreach(string line in Enumerable.Range(0,points.Length)
                    .Select(pi=>
                        ""+pi+" ("+string.Join(", ",Enumerable.Range(0,2).Select(dim=>mds.GetPoint(pi,dim).ToString()).ToArray())+")"))
                        Console.WriteLine(line);


            }
        }
    }
}
