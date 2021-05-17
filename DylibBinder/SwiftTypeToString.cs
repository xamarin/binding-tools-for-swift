using System;
using System.Collections.Generic;
using Dynamo.SwiftLang;
using SwiftReflector;
using SwiftReflector.TypeMapping;
using SwiftRuntimeLibrary;

namespace DylibBinder {
	internal static class SwiftTypeToString {
		public static List<string> TypeDatabasePaths = new List<string> ();

		public static string MapSwiftTypeToString (SwiftType swiftType, string moduleName = null)
		{
			var slType = MapSwiftTypeToSlType (Exceptions.ThrowOnNull (swiftType, nameof (swiftType)));
			var slTypeString = slType.ToString ();
			slTypeString = slTypeString.AppendModuleToBit ();
			return slTypeString;
		}

		public static SLType MapSwiftTypeToSlType (SwiftType swiftType)
		{
			var typeMapper = new TypeMapper (TypeDatabasePaths, null);
			var swiftTypeToSLType = new SwiftTypeToSLType (typeMapper, true);
			var sLImportModules = new SLImportModules ();
			return swiftTypeToSLType.MapType (sLImportModules, Exceptions.ThrowOnNull (swiftType, nameof (swiftType)));
		}
	}
}
