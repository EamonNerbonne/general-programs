using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LanuageTestSubclass
{

    interface IPairedWith<TA,TB> where TA:IPairedWith<TA,TB> where TB:IPairedWith<TB,TA>
    {
        void DoIt(TB b);
    }


    class A:IPairedWith<A,B>
    {

        public void DoIt(B b) {
            throw new NotImplementedException();
        }

    }

    class B : IPairedWith<B, A>
    {

        public void DoIt(A a) {

        }

    }

    class ASub:A
    {


    }
    class BSub : B
    {

    }



    class MutualSubs
    {
        static void Main(string[] args) {

        }

    }
}
