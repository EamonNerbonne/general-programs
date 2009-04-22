using System;
using System.Web;
using System.Web.SessionState;
using System.Collections;
using System.Collections.Specialized;
using System.Xml;
using System.Reflection;
using System.IO;
using System.Xml.Xsl;

using progress.webframework.backend;

namespace progress.webframework 
{
	/// <summary>
	/// The <c>UserWebContext</c> is the heart of each session.  It stores and generates such information as id's for each <see cref="AbstractWebModule"/>,
	/// their <see cref="AbstractWebModule.Name"/>s, calls <see cref="IRequiresOnPageLoad.OnPageLoad"/> functions requested by the webmodules and
    /// more.  It should be constructed in the <c>global.asax</c> file's <c>OnSessionStart</c> event (or a similar construct), and should be notified when the
    /// session ends via <c>global.asax</c>'s <c>OnSessionEnd</c> event.
	/// </summary>
	public class UserWebContext 
	{
		internal ArrayList reqOnPageLoad=new ArrayList(), reqHandleEvents=new ArrayList(),reqOnDispose=new ArrayList();
		private AbstractWebModule rootModule;
		Hashtable modules=new Hashtable(),globals=new Hashtable();
		internal Hashtable modulesByName=new Hashtable();
		internal PersistanceBackend pb;
		HttpContext context;
		bool endsession=false,sessiondead=false;

		/// <summary>
		/// Calling this function enables the persistance features of the webframework, <see cref="IPersistanceHandler"/>.
		/// After this function is called, all calls to <see cref="AbstractWebModule.RegisterName"/> cause a lookup in the Hashtable initially returned by
		/// <see cref="IPersistanceHandler.LoadFromDB"/>, after which the framework will set all variables of the <see cref="AbstractWebModule"/> in question
		/// marked with <see cref="PersistAttribute"/>.  If no fields are marked persistant, then this function has no noticeable effect.
		/// This Hashtable of Hashtables format used indexes by module-name and then by field-name to have a value of int or string.
		/// </summary>
		public void SetPersistance(IPersistanceHandler persistanceHandler) 
		{
			if(pb!=null) throw new Exception("SetPersistance Already Called once this session...");
			else pb=new PersistanceBackend(persistanceHandler);
		}

		public void EndSession() 
		{
			if(context==null) 
				OnSessionEnd();
			else
				endsession=true;
		}
		/// <summary>
		/// Constructs a <c>UserWebContext</c> which will initially display a module constructed by the factory provided.
		/// </summary>
		/// <param name="rootFactory">A factory which should return an <see cref="AbstractWebModule"/> suitable for use as root element.</param>
		public UserWebContext(IModuleFactory rootFactory) { 
			HttpContext.Current.Session["UWC"]=this;
			rootModule=rootFactory.CreateModule();
		}

		private void setPostBackFrom(NameValueCollection req) 
		{
			foreach(string key in req.Keys) 
			{
				int indexof = key.IndexOf('*');
				if (indexof == -1) continue;
				AbstractWebModule target =(AbstractWebModule)modules[key.Substring(0,indexof)];
				if( target==null) continue;
				if(target.Postback[key.Substring(indexof + 1)]!=null) throw new Exception("Postback variable duplicated; perhaps both POSTed and in the query-string.");
				target.Postback[key.Substring(indexof + 1)] = req[key];
			}
		}
		internal void OnPageLoad(HttpContext current) 
		{
			context = current;
			setPostBackFrom(context.Request.QueryString);
			setPostBackFrom(context.Request.Form);
			foreach(IRequiresOnPageLoad irol in new Queue(reqOnPageLoad)) irol.OnPageLoad();
		}

		internal void OnPageUnload() 
		{
			foreach(AbstractWebModule awm in modules.Values) { awm.postbackVars.Clear();}
			if(endsession) 
			{
				try
				{
					OnSessionEnd();
				}
				finally {context.Session.Abandon();}
			}
			context=null;
		}
		/// <value>The <see cref="HttpContext"/> of this request.  Generally this should not be required by users of the framework.</value>
		public HttpContext Context{get{return context;}}
		/// <value>A session-wide Hashtable usuable to exchange cross-<see cref="AbstractWebModule"/> information.</value>
		public Hashtable Global{get {return globals;}}

		/// <summary>Looks up the <see cref="AbstractWebModule"/> corresponding to the given id.  This should generally not be necessary;
		/// preferrably all modules should use standard .NET variables to reference each other.</summary>
		/// <param name="idstr">The id of the module to find</param>
		/// <returns>The module with given id or null if none is found.</returns>
		public AbstractWebModule GetModuleById(string idstr) {return (AbstractWebModule) modules[idstr]; }

		
		/// <summary>Looks up the <see cref="AbstractWebModule"/> corresponding to the given name.  This should generally not be necessary;
		/// preferrably all modules should use standard .NET variables to reference each other.</summary>
		/// <param name="name">The name of the module to find</param>
		/// <returns>The module with given name or null if none is found.</returns>
		public AbstractWebModule GetModuleByName(string name) {return (AbstractWebModule) modulesByName[name]; }


		int dynGen=0;
		internal void RegisterModule(AbstractWebModule awm)
		{
			awm.idstr="id"+dynGen++;
			modules[awm.idstr]=awm;

			if(awm is IRequiresOnPageLoad) reqOnPageLoad.Add(awm);
			if(awm is IRequiresHandleEvents) reqHandleEvents.Add(awm);
		}

		/// <value>The root module which is called by the framework each pageview to generate the xml representation</value>
		/// <remarks><seealso cref="AbstractWebModule.GenerateXML"/></remarks>
		public AbstractWebModule Root { get { return rootModule; } set { rootModule=value; } }

		internal void HandleEvents() 
		{
			foreach(IRequiresHandleEvents irhe in new Queue(reqHandleEvents)) 
				if(((AbstractWebModule)irhe).postbackVars.Count>0)
					irhe.HandleEvents();
		}

		class ReqTimer 
		{
			DateTime startTime,startPhaseTime;
			TimeSpan[] phaseDuration=new TimeSpan[10];
			string[] phaseName=new string[10];
			int phaseCount=0;
			public ReqTimer() {startTime=startPhaseTime=DateTime.Now;}
			public void PhaseEnd(string name) 
			{
				DateTime end=DateTime.Now;
                phaseName[phaseCount]=name;
				phaseDuration[phaseCount]=end.Subtract(startPhaseTime);
				phaseCount++;
				startPhaseTime=end;
			}
			public void WriteTo(TextWriter tw) 
			{

				tw.WriteLine("<!--");

				for(int i=0;i<phaseCount;i++) tw.WriteLine(phaseName[i]+": "+phaseDuration[i].TotalMilliseconds+" ms");
				tw.WriteLine("==Total==: "+startPhaseTime.Subtract(startTime).TotalMilliseconds+" ms");

				tw.WriteLine("-->");

			}
		}


		int xsltLoc=0;//0=serverside;1=client;2=none;
		bool showDebug=false;
		/// <summary>
		/// Should be called by the web server to render each page. Ideally the user of the framework would implement an <see cref="IHttpHandler"/> or a
		/// <c>.aspx</c> page which is empty except for a pass through to the this function.
		/// </summary>
		/// <param name="context">The ASP.NET <see cref="System.Web.HttpContext"/> of the current request.</param>
		public void PageRequest(HttpContext context) 
		{
			try 
			{
				ReqTimer r=new ReqTimer();
				switch(context.Request.Params["xslt"]) 
				{
					case "none": xsltLoc=2;break;
					case "client": xsltLoc=1;break;
					case "server": xsltLoc=0;break;
				}
				switch(context.Request.Params["showDebug"]) 
				{
					case "yes": showDebug=true;break;
					case "no": showDebug=false;break;
				}
			
				OnPageLoad(context);
				r.PhaseEnd("OnLoad");
				HandleEvents();
				r.PhaseEnd("HandleEvents");

				XmlTextWriter xtw;
				switch(xsltLoc) 
				{
					case 2: case 1:
						context.Response.ContentType = "text/xml";
						xtw = new XmlTextWriter(context.Response.Output);
						xtw.Formatting = Formatting.Indented;
						xtw.WriteStartDocument();
						if(xsltLoc==1)xtw.WriteProcessingInstruction("xml-stylesheet","type=\"text/xsl\" href=\"xsl/global.xsl\"");
						rootModule.GenerateXML(xtw);
						r.PhaseEnd("GenXML");
						break;
					case 0:					
						StringWriter sw = new StringWriter();						
						XslTransform xt = new XslTransform();
						context.Response.ContentType = "text/html";
						xtw = new XmlTextWriter(sw);
						xtw.WriteStartDocument();
						rootModule.GenerateXML(xtw);
						r.PhaseEnd("GenXML");
						xtw.Close();
						XmlDocument xmldoc = new XmlDocument();
						xmldoc.LoadXml(sw.ToString()); 					
						xt.Load(context.Server.MapPath("xsl/global.xsl"));
						xtw=new XmlTextWriter(context.Response.Output);
						xtw.Formatting=showDebug?Formatting.Indented:Formatting.None;
						xtw.WriteStartDocument();
						xtw.WriteDocType("html","-//W3C//DTD XHTML 1.0 Strict//EN","http://www.w3.org/TR/xhtml1/DTD/xhtml1-strict.dtd",null);
						xt.Transform(xmldoc, null, xtw, null);
						r.PhaseEnd("XSLT");
						r.PhaseEnd("SaveXML");
						break;
		
					default: throw new Exception("Unrecognized xsltLoc value: "+xsltLoc);
				}
				r.PhaseEnd("UnLoad");
				if(showDebug) r.WriteTo(context.Response.Output);
			} 
			finally 
			{
				OnPageUnload();
			}
		}

		/// <summary>
		/// This function should be called by the user when the session ends.  It tells the framework to clean up any remaining trash and persist all data
		/// (if necessary).  For example, this function could be called in the global.asax <p>OnSessionEnd</p> function.
		/// </summary>
		public void OnSessionEnd() //ok, persist what's necessary...
		{
			if(!sessiondead)
				try
				{
					if(pb!=null)
					{
						foreach(AbstractWebModule awm in modules.Values) pb.PersistAWM(awm);
						pb.StoreNow();
					}

				}
				finally {sessiondead=true;}
		}
	}
}