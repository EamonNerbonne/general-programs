﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using hitmds;
using EmnExtensions.Collections;

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

            SymmetricDistanceMatrix distMat = new SymmetricDistanceMatrix();
            distMat.ElementCount = points.Length;
            for (int i = 0; i < points.Length; i++)
                for (int j = i + 1; j < points.Length; j++)
                    distMat[i, j] = (float)(points[i].DistanceTo(points[j]) + r.NextDouble());

            using (Hitmds mds = new Hitmds(2, distMat,r)) {
                mds.mds_train(points.Length * 5000, 1.0,0.5, (i, j,mdsP) => { },0);

                foreach(string line in Enumerable.Range(0,points.Length)
                    .Select(pi=>
                        ""+pi+" ("+string.Join(", ",Enumerable.Range(0,2).Select(dim=>mds.GetPoint(pi,dim).ToString()).ToArray())+")"))
                        Console.WriteLine(line);


            }
        }
    }
}
