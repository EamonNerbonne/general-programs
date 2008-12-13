using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LanuageTestSubclass
{
    

    public class ManageP
    {
        private object key=new object();
        private static Func<P> trustedMaker;
        public static void SetPMaker(P proof, Func<P> suggestedMaker) {
            if (proof != null && trustedMaker == null) trustedMaker = suggestedMaker;
        }
        static ManageP() { P.Initialize(); }
        /*static void Main(string[] args)
        {
            foreach (var i in Enumerable.Range(0,10))
                Console.WriteLine(trustedMaker());
        }*/
    }

    public sealed class P
    {
        static int count=0;
        private P() { count++; }
        public override string ToString() {return "#" + count;}
        public static void Initialize() { ManageP.SetPMaker(new P(), () => new P()); }
    }

    public class Item
    {
        public Container Parent {get;private set;}
        public void AddTo(Container c)
        {
            if (Parent != null)
                throw new Exception("Already in a container");
            Parent = c;
            c.AddIt(this);
        }
    }

    public class Container:Item {
        private HashSet<Item> items = new HashSet<Item>();
        public void AddIt(Item i) {
            if (i.Parent == this)
                items.Add(i);
            else
                throw new Exception("Can't add an involuntary parent");
        }
    }
    
}
