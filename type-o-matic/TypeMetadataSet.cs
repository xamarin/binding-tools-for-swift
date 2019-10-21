using System;
using System.Collections.Generic;
using System.Linq;
using SwiftReflector;
using System.Xml.Linq;

namespace typeomatic {
	public class TypeMetadataSet : List<TypeMetaMap> {
		public TypeMetadataSet (PlatformName platform)
			: base ()
		{
			Platform = platform;
		}

		public PlatformName Platform { get; set; }

		public static TypeMetadataSet FromXml (XDocument doc)
		{
			var metaset = doc.Descendants ("TypeMetadataSet").FirstOrDefault ();
			if (metaset == null)
				return null;
			var retval = new TypeMetadataSet (PlatformNameFromString ((string)metaset.Attribute ("platform")));

			var metaparts = metaset.Descendants ("TypeMetaMap").Select (TypeMetaMap.FromXml);
			retval.AddRange (metaparts);

			return retval;
		}

		static PlatformName PlatformNameFromString (string platform)
		{
			switch (platform) {
			case "iphoneos":
				return PlatformName.iOS;
			case "macosx":
				return PlatformName.macOS;
			case "appletvos":
				return PlatformName.tvOS;
			case "watchos":
				return PlatformName.watchOS;
			default:
				throw new ArgumentOutOfRangeException (nameof (platform));
			}
		}

		public string FileForSwiftType (string type)
		{
			var module = ModuleFromName (type);

			var map = MapForModule (module);
			if (map == null)
				return null;
			return map.File;
		}

		public FileSymbolPair MetadataSymbolForSwiftType (string type)
		{
			var module = ModuleFromName (type);

			var map = MapForModule (module);
			if (map == null) {
				Console.WriteLine ("namespace not found: " + type);
				return null;
			}
			string symbol = null;
			if (map.TryGetValue (type, out symbol)) {
				return new FileSymbolPair {
					File = map.File,
					Symbol = symbol
				};
			}
			Console.WriteLine ("not found: " + type);
			return null;
		}

		TypeMetaMap MapForModule (string module)
		{
			foreach (var map in this) {
				if (map.Module == module)
					return map;
			}
			return null;
		}

		static string ModuleFromName (string s)
		{
			int dotIndex = s.IndexOf ('.');
			if (dotIndex <= 0)
				return null;
			return s.Substring (0, dotIndex);
		}
	}

}
