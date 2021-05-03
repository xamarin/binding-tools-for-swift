using System;
using System.Collections.Generic;
using Dynamo.SwiftLang;
using SwiftReflector;
using SwiftReflector.TypeMapping;

namespace DylibBinder {
	internal static class SwiftTypeToString {
		public static List<string> TypeDatabasePaths = new List<string> ();

		public static string MapSwiftTypeToString (SwiftType swiftType, string moduleName = null)
		{
			var slType = MapSwiftTypeToSlType (swiftType);
			var slTypeString = slType.ToString ();
			slTypeString = slTypeString.AppendModuleToBit ();
			return slTypeString;
		}

		public static SLType MapSwiftTypeToSlType (SwiftType swiftType)
		{
			var typeMapper = new TypeMapper (TypeDatabasePaths, null);
			var swiftTypeToSLType = new SwiftTypeToSLType (typeMapper, true);
			var sLImportModules = new SLImportModules ();
			return swiftTypeToSLType.MapType (sLImportModules, swiftType);
		}
	}
}
