using System;
using Tidy;

    public class TidyItUp
    {
        /// <summary>
        /// Takes a string and cleans it up, converting it to xml in the process!
        /// </summary>
        /// <param name="input">The html document to conver</param>
        /// <returns>the cleaned up and xml-parsable resultant file.</returns>
        public static string CleanupToXml(string input)
        {
            int status;
            Document tdoc = new Document();
            tdoc.SetOptBool(TidyOptionId.TidyForceOutput, 1);
            tdoc.SetOptBool(TidyOptionId.TidyXmlOut, 1);
            tdoc.SetOptBool(TidyOptionId.TidyXmlDecl, 1);
            tdoc.SetOptBool(TidyOptionId.TidyNumEntities, 1);
            tdoc.SetOptBool(TidyOptionId.TidyMakeClean, 1);
            tdoc.SetOptBool(TidyOptionId.TidyMark, 0);
            tdoc.SetOptBool(TidyOptionId.TidyUpperCaseTags, 0);
            tdoc.SetOptBool(TidyOptionId.TidyUpperCaseAttrs, 0);
            tdoc.SetOptBool(TidyOptionId.TidyDoctype, 0);
            tdoc.SetOptBool(TidyOptionId.TidyDoctypeMode, 0);



            //tdoc.SetOptInt(TidyOptionId.TidyNewline, 2);
            //tdoc.SetOptValue(TidyOptionId.TidyDoctype, "omit");
            if (tdoc.ParseString(input) < 0) return null;//unrecoverable error;
            if (tdoc.CleanAndRepair() < 0) return null; //unrecoverable error;
            return tdoc.SaveString();
        }
    }
