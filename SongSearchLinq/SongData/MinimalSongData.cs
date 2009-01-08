using System;
using System.Web;
using System.Xml.Linq;
using EmnExtensions.Filesystem;
using System.Text;

namespace SongDataLib
{

    public class MinimalSongData : ISongData
    {
        protected string songuri;
        protected bool isLocal;
        public virtual string FullInfo { get { return HttpUtility.UrlDecode(songuri); } }

        protected XAttribute makeUriAttribute(Func<string, string> urlTranslator) {
            if (!IsLocal) {
                return new XAttribute("songuri", songuri);
            } else if (urlTranslator == null) {
                return new XAttribute("uriUtfB64", Convert.ToBase64String(Encoding.UTF8.GetBytes(songuri)));
            } else {
                return new XAttribute("songuri", urlTranslator(songuri));
            }
        }

        public virtual XElement ConvertToXml(Func<string, string> urlTranslator) {
            return new XElement("songref", makeUriAttribute(urlTranslator));
        }

        public virtual int Length { get { return 0; } }

        public virtual string SongPath { get { return songuri; } }

        public virtual bool IsLocal { get { return isLocal; } }

        public virtual string HumanLabel {
            get {
                Uri uri = new Uri(songuri, UriKind.Absolute);
                return HttpUtility.UrlDecode(uri.Host + uri.AbsolutePath);//adhoc best guess.//TODO improve: goes wrong on things like http://whee/boom.mp3#testtest
            }
        }

        public MinimalSongData(string songuri, bool? isLocal) {
            if (songuri == null || songuri.Length == 0) throw new ArgumentNullException(songuri);
            bool pathSeemsLocal = FSUtil.IsValidAbsolutePath(songuri) == true;
            if (isLocal == null) this.isLocal = pathSeemsLocal;
            else if (isLocal != pathSeemsLocal)
                throw new Exception("Supposedly " + (pathSeemsLocal ? "" : "non-") + "local song isn't: " + songuri);
            else this.isLocal = isLocal.Value;

            this.songuri = songuri;
        }

        static string loadUri(XElement from, bool isLocal) {
            if (isLocal == true) {
                string tmp = (string)from.Attribute("uriUtfB64");//preferred place to put base64 data
                if (tmp != null)
                    return Encoding.UTF8.GetString(Convert.FromBase64String(tmp));
                string retval = (string)from.Attribute("songuri");//old versions stuck base64 data in songuri
                try {
                    return Encoding.UTF8.GetString(Convert.FromBase64String(retval));
                } catch (FormatException) {
                    return retval;
                }//if this isn't a base64string, just assume it's a normal string.
            } else {
                return (string)from.Attribute("songuri") ?? (string)from.Attribute("filepath");//songuri is preferred non-base64 data, but old versions sometimes used filepath
            }
        }

        public MinimalSongData(XElement xEl, bool? isLocal) : this(loadUri(xEl, isLocal == true), isLocal) { }
    }

}
