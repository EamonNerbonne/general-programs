using System;
using System.Collections.Generic;
using System.Text;

namespace EamonExtensionsLinq {
    public class NiceTimer {
//#if DEBUG
        DateTime dt;
        string oldmsg;
//#endif
        public void TimeMark(string msg) {
//#if DEBUG
            if (oldmsg != null) {
                Console.WriteLine(oldmsg + " TOOK " + ((TimeSpan)(DateTime.Now - dt)).TotalSeconds + " secs.");
            }
//#if DEBUG
            Console.WriteLine("MB alloc'd: {0}", System.GC.GetTotalMemory(false) >> 20);
//#endif
            if (msg != null)
                Console.WriteLine("TIMING: " + msg);
            dt = DateTime.Now;
            oldmsg = msg;
//#endif
        }

        public TimeSpan ElapsedSinceMark{get{return DateTime.Now-dt;}}

        public NiceTimer(string msg) {
//#if DEBUG
            dt = default(DateTime);
            oldmsg=null;
            if(msg!=null) TimeMark(msg);
//#endif
        }
    }
}
