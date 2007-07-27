using System;
using System.Collections.Generic;
using System.Text;
using System.Globalization;
using EamonExtensionsLinq;

namespace SongDataLib {
    public static class SongUtil {


        public static int? StringToNullableInt(string num) {
            return FuncUtil.Swallow<int?>(()=> int.Parse(num), ()=>null);
        }


        public static bool Contains(byte[] elem, byte[] substring) {
            for (int i = 0; i <= elem.Length - substring.Length; i++) {
                bool match = true;
                for (int j = 0; j < substring.Length; j++) {
                    if (elem[i + j] != substring[j]) {
                        match = false;
                        break;
                    }
                }
                if (match)
                    return true;
            }
            return false;
        }
    }
}
