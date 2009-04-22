using System;
using System.Collections;
using System.Collections.Specialized;
using progress.webframework.backend;
using System.Reflection;

namespace progress.webframework {
	/// <summary><para>
	/// The <c>AbstractWebModule</c> is the base class for all web modules that a web application using this framework uses should implement.
	/// It provides a skeleton for XML generation which is intended to be transformed with XSLT, persistancy in case an application requests this,
	/// separate postback variables for each module, and global session variables.  An <c>AbstractWebModule</c> is not recreated each pageview but 
	/// created only once; as such any code in a constructor will be executed whenever that module is instantiated.  The entire user-context consisting of all web
	/// modules and the framework related class-instances for that session remain in server memory for the entire session; as such large data sets should not be 
	/// retained within an abstractwebmodule; but only enough information to create the user interface.
	/// </para><para>
	/// An <c>AbstractWebModule</c> may choose to request additional functionality beyond simple rendering from the framework.  In this case, a module
	/// should implement a number of "marker" interfaces for each function/feature required:
	/// </para>
	///  <list type="bullet"><item><description>
	///  <see cref="IRequiresOnPageLoad"/>
	///  </description></item>
	///  <item><description><see cref="IRequiresHandleEvents"/></description></item>
	///  </list>
	/// </summary>
	public abstract class WebModule 
	{
		internal NameValueCollection postbackVars = new NameValueCollection();
		internal WebContext uwc;
		internal string idstr;
		/// <summary>
		/// Constructs an abstract web module and uses the ASP.NET session to obtain a reference to the <see cref="UserWebContext"/> required for most
		/// framework functionality.  Because this reference must exist prior to <c>AbstractWebModule</c> construction, no module can be created before the
		/// web framework is initialized.
		/// </summary>
		public WebModule()
		{
			uwc=(UserWebContext)System.Web.HttpContext.Current.Session["UWC"];
			if(uwc==null) throw new Exception("UserWebContext not set correctly, must be in the session variables under key 'UWC'");
			uwc.RegisterModule(this);
			
			object[] attrs;
			if((attrs=GetType().GetCustomAttributes(typeof(ModuleNameAttribute),false)).Length!=0) 
				RegisterName(((ModuleNameAttribute)attrs[0]).name);
		}
		/// <summary>
		/// Registers a persistant name for this web module; if this name is recognized by the persistancy provider; fills those fields marked with the
		/// <see cref="PersistAttribute"/> with the values of the most recently terminated session.  A name must be unique for that user, and a module can have
		/// only one name.  A module without a name does not persist it's values.  Registering names for modules before a persistancy provider is registered with
		/// <see cref="UserWebContext.SetPersistance"/> is (normally) useless as that data will be stored but never read (as the provider wasn't registered at the
		/// time the module name was set).
		/// </summary>
		/// <param name="name">The unique name for the module under which to persist it.</param>
		public void RegisterName(string name) 
		{
			if(name==null) throw new NullReferenceException("You cannot register null as a name.");
			if(persistID!=null) throw new Exception("This Module already has a name ('"+persistID+"'), you can't set it to '"+name+"'.");
			if(uwc.modulesByName.ContainsKey(name)) throw new Exception("Another module already has this name ('"+name+"')...");
			
			persistID=name;
			uwc.modulesByName[name]=this;
			if(uwc.pb!=null) uwc.pb.LoadAWM(this);
		}

		/// <value>Those postback variables targeted to this module.</value>
		/// <summary>
		///  the HTTP POST or GET variables are parsed by the framework.  All variables starting with the <see cref="ID"/> of this module, followed by a
		///  <c>'*'</c> delimiter, followed by an event name are stored in this <see cref="NameValueCollection"/>.  For instance, if a module had ID "id3" then
		///  the query <c>?id2*foo=bar&amp;id3*hello=world&amp;something=different"</c> would result in that module's <c>Postback</c> to contain exactly one
		///  key-value pair, such that <c>Postback["hello"]=="world"</c> would be true.
		/// </summary>
		public NameValueCollection Postback { get { return postbackVars; } }
		
		/// <summary>
		/// A refence to the central point of reference of a particular user's session.
		/// </summary>
		/// <value>The <see cref="UserWebContext"/> of this session.</value>
		public UserWebContext UWC { get { return uwc; } }
		/// <summary>
		/// Global session variables.  Can contain as keys or values anything; not just strings.
		/// </summary>
		/// <value>A session-wide global <see cref="Hashtable"/>.</value>
		public Hashtable Global { get { return uwc.Global; } }
		/// <value>The framework-assigned ID for this module.</value>
		public string ID { get { return idstr; } }
		
		/// <value>This module's name.</value>
		/// <summary>
		/// The name to store <see cref="PersistAttribute"/> marked fields under.
		/// <seealso cref="UserWebContext.SetPersistance"/>
		/// </summary>
		public string Name { get { return persistID; } }

		/// <summary>
		/// This function generates an xml representation of this module, and writes it to the xmlwriter provided.  It is called by the framework for one
		/// (<see cref="UserWebContext.Root"/>) module.  It generates xml in the form of 
		/// &lt;obj id="<i>[ID]</i>" type="<i>[Full class-name]</i>" <i>[a list of all <see cref="RenderAttribute"/> marked fields]</i>&gt; <i>[The xml generated by
		/// <see cref="InternalRender"/>]</i>&lt;/obj&gt;.  It is the obligation of the web module to call <c>GenerateXML</c> on any children that should be
		/// displayed.  Using the type and id attributes on the obj tab generated makes it possible to make a simple xsl:template to match only the appropriate
		/// type and include the correct ID for that module on any form variables the xsl template outputs.
		/// </summary>
		/// <param name="xmlw">The writer to write to.</param>
		public void GenerateXML(System.Xml.XmlWriter xmlw) {
			if(uwc==null) throw new Exception("This module is unregistered!");
			xmlw.WriteStartElement("obj");
			xmlw.WriteAttributeString("type", this.GetType().FullName);
			xmlw.WriteAttributeString("id", ID);
			foreach(FieldInfo fi in GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
				if(fi.GetCustomAttributes(typeof(RenderAttribute),true).Length!=0)
				{
					object val=fi.GetValue(this);
					if(val!=null)	xmlw.WriteAttributeString(fi.Name,val.ToString());
				}


			InternalRender(xmlw);
			xmlw.WriteEndElement();
		}
		/// <summary>
		/// This function if called by <see cref="GenerateXML"/> once the standard obj tag is written.  Subclasses should render any required content for their
		/// XSL templates here.
		/// </summary>
		/// <param name="xmlw">The writer to write to, with the obj-tag for this module already written.</param>
		protected abstract void InternalRender(System.Xml.XmlWriter xmlw);
	}
}