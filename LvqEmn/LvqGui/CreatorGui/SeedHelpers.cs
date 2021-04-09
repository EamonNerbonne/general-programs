// ReSharper disable UnusedMemberInSuper.Global
using System.Linq;
using EmnExtensions.MathHelpers;

namespace LvqGui {
    public interface IHasSeed {
        uint ParamsSeed { get; set; }
        uint InstanceSeed { get; set; }
    }
    public static class IHasSeedExtensions {
        public static void ReseedBoth(this IHasSeed seededObj) { seededObj.ReseedParam(); seededObj.ReseedInst(); }
        public static void ReseedParam(this IHasSeed seededObj) { seededObj.ParamsSeed = RndHelper.MakeSecureUInt(); }
        public static void ReseedInst(this IHasSeed seededObj) { seededObj.InstanceSeed = RndHelper.MakeSecureUInt(); }
    }
}
