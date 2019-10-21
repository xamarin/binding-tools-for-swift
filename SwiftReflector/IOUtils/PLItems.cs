using System.Xml;
using System.Xml.Linq;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;

namespace SwiftReflector.IOUtils {
	public interface IPLItem {
		void AppendToXml (XElement parentElement);
	}

	public interface IPLScalar : IPLItem {
		string Type { get; }
	}

	public abstract class PLBaseScalar : IPLScalar {
		protected PLBaseScalar (string type)
		{
			Type = type;
		}
		public string Type { get; private set; }
		public void AppendToXml (XElement parentElement)
		{
			XElement key = new XElement (Type, GetValue ());
			parentElement.Add (key);
		}

		protected abstract object GetValue ();
	}

	public class PLKey : PLBaseScalar {
		public PLKey (string key)
			: base ("key")
		{
			Key = key;
		}
		public string Key { get; private set; }
		protected override object GetValue ()
		{
			return Key;
		}
		public override bool Equals (object obj)
		{
			PLKey other = obj as PLKey;
			if (other == null)
				return false;
			return Key == other.Key;
		}
		public override int GetHashCode ()
		{
			return Key.GetHashCode ();
		}
	}

	public class PLValue<T> : PLBaseScalar {
		public PLValue (T value)
			: base ("string")
		{
			Value = value;
		}
		public T Value { get; private set; }
		protected override object GetValue () { return Value; }
	}

	public class PLString : PLValue<string> {
		public PLString (string value) : base (value) { }
	}

	public class PLInteger : PLBaseScalar {
		public PLInteger (int value)
			: base ("integer")
		{
			Value = value;
		}
		public int Value { get; private set; }
		protected override object GetValue ()
		{
			return Value.ToString ();
		}
	}

	public class PLArray : List<IPLItem>, IPLItem {
		public void AppendToXml (XElement parentElement)
		{
			XElement arrElement = new XElement ("array");
			foreach (var elem in this) {
				elem.AppendToXml (arrElement);
			}
			parentElement.Add (arrElement);
		}
	}

	public class PLKeyValuePair : IPLItem {
		public PLKeyValuePair (PLKey key, IPLItem value)
		{
			Key = key;
			Value = value;
		}
		public PLKeyValuePair (string key, IPLItem value)
			: this (new PLKey (key), value)
		{
		}

		public PLKey Key { get; private set; }
		public IPLItem Value { get; private set; }
		public void AppendToXml (XElement parentElement)
		{
			Key.AppendToXml (parentElement);
			Value.AppendToXml (parentElement);
		}
	}

	public class PLDict : Dictionary<PLKey, IPLItem>, IPLItem {
		public IPLItem this [string index] {
			get {
				return this [new PLKey (index)];
			}
			set {
				this [new PLKey (index)] = value;
			}
		}

		public void Add (string key, IPLItem value)
		{
			this [key] = value;
		}

		public bool ContainsKey (string key)
		{
			return ContainsKey (new PLKey (key));
		}

		public void AppendToXml (XElement parentElement)
		{
			XElement dictElement = new XElement ("dict");
			foreach (var kvp in this.Select (kvp => new PLKeyValuePair (kvp.Key, kvp.Value))) {
				kvp.AppendToXml (dictElement);
			}
			parentElement.Add (dictElement);
		}

		public void ToXml (Stream stm)
		{
			XDocumentType docType = new XDocumentType ("plist", "-//Apple//DTD PLIST 1.0//EN", "http://www.apple.com/DTDs/PropertyList-1.0.dtd", null);

			XDocument doc = new XDocument (docType);

			XElement root = new XElement ("plist");
			root.Add (new XAttribute ("version", "1.0"));
			doc.Add (root);
			AppendToXml (root);
			if (doc.DocumentType != null) {
				doc.DocumentType.InternalSubset = null;
			}
			XmlWriterSettings xws = new XmlWriterSettings ();
			xws.OmitXmlDeclaration = false;
			xws.NewLineHandling = NewLineHandling.None;
			xws.Indent = true;
			xws.Encoding = new System.Text.UTF8Encoding (false);
			xws.ConformanceLevel = ConformanceLevel.Document;

			using (XmlWriter writer = XmlWriter.Create (stm, xws)) {
				doc.Save (writer);
			}
		}

		public byte [] ToXml ()
		{
			using (MemoryStream ms = new MemoryStream ()) {
				ToXml (ms);
				return ms.ToArray ();
			}
		}

		public string ToXmlString ()
		{
			return Encoding.UTF8.GetString (ToXml ());
		}
	}
}
