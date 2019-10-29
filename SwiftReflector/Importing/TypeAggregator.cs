// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;

namespace SwiftReflector.Importing {
	public enum TypeType {
		None,
		Class,
		Interface,
		Struct,
		Enum,
		Delegate,
	}

	public partial class TypeAggregator {
		public static string kINativeObject = "ObjCRuntime.INativeObject";
		public static string kNSObject = "Foundation.NSObject";
		public static string kRegisterAttribute = "Foundation.RegisterAttribute";
		public static string kProtocolAttribute = "Foundation.ProtocolAttribute";
		public static string kModelAttribute = "Foundation.ModelAttribute";
		public static string kSkipRegistration = "SkipRegistration";

		string assemblyPath, assemblyDirectory;

		public TypeAggregator (PlatformName platform)
			: this (PathForPlatform (platform))
		{
		}

		public TypeAggregator (string assemblyPath)
		{
			this.assemblyPath = assemblyPath;
			Includes = new MatchCollection ();
			Excludes = new MatchCollection ();
			IsObjCChecker = (s, t) => { return false; };
		}

		public void Aggregate ()
		{
			if (!File.Exists (assemblyPath))
				throw new FileNotFoundException ("Imported assembly does not exist.", assemblyPath);
			assemblyDirectory = Path.GetDirectoryName (assemblyPath);
			AllTypes = LoadTypes ();
			PublicClasses = ApplyIncludeExcludeRules (AllTypes.Where (t => IsObjCClassCandidate (t)));
			PublicStructs = ApplyIncludeExcludeRules (AllTypes.Where (t => IsStructCandidate (t)));
			PublicProtocols = ApplyIncludeExcludeRules (AllTypes.Where (t => IsObjCProtocolCandidate (t)));
			PublicEnums = ApplyIncludeExcludeRules (AllTypes.Where (t => t.IsPublic && t.IsEnum));
		}

		public MatchCollection Includes { get; }
		public MatchCollection Excludes { get; }

		public TypeDefinition [] AllTypes { get; private set; }
		public List<TypeDefinition> PublicClasses { get; private set; }
		public List<TypeDefinition> PublicStructs { get; private set; }
		public List<TypeDefinition> PublicProtocols { get; private set; }
		public List<TypeDefinition> PublicEnums { get; private set; }
		public Func<string, string, bool> IsObjCChecker { get; set; }

		public static bool CacheModules { get; set; }
		static Dictionary<string, ModuleDefinition> module_cache = new Dictionary<string, ModuleDefinition> ();

		TypeDefinition [] LoadTypes ()
		{
			ModuleDefinition module;
			if (CacheModules) {
				lock (module_cache) {
					if (!module_cache.TryGetValue (assemblyPath, out module)) {
						// We're returning the same instances in multiple threads,
						// which means it can't be loaded as needed since that
						// will run into race conditions.
						var readerParameters = new ReaderParameters (ReadingMode.Immediate);
						module = ModuleDefinition.ReadModule (assemblyPath, readerParameters);
						module_cache [assemblyPath] = module;
					}
				}
			} else {
				module = ModuleDefinition.ReadModule (assemblyPath);
			}
			return module.Types.ToArray ();
		}

		bool IsObjCProtocolCandidate (TypeDefinition definition)
		{
			if (!definition.IsPublic)
				return false;
			if (!definition.IsInterface)
				return false;
			if (HasProtocolAttribute (definition))
				return true;
			return ImplementsINativeObject (definition);
		}
		bool IsStructCandidate (TypeDefinition definition)
		{
			return definition.IsPublic && definition.IsValueType && definition.IsClass && !definition.IsEnum && !IsNIntOrNUInt (definition);
		}

		static bool IsNIntOrNUInt (TypeDefinition def)
		{
			return def.FullName == "System.nint" || def.FullName == "System.nuint";
		}

		bool IsObjCClassCandidate (TypeDefinition definition)
		{
			if (!definition.IsPublic)
				return false;
			if (!definition.IsClass || definition.IsValueType)
				return false;
			if (HasSkipRegistration (definition) == true)
				return false;
			if (HasModelAttribute (definition))
				return false;
			return ImplementsINativeObject (definition) || IsNSObject (definition);
		}

		bool IsNSObject (TypeDefinition definition)
		{
			if (definition == null)
				return false;
			if (definition.FullName == kNSObject)
				return true;
			return IsNSObject (definition.BaseType);
		}

		bool IsNSObject (TypeReference reference)
		{
			if (reference == null)
				return false;

			if (reference.FullName == kNSObject)
				return true;

			if (reference.Module != null) {
				return IsNSObject (reference.Resolve ());
			}

			return IsObjCChecker != null && IsObjCChecker (reference.Namespace, reference.Name);
		}

		bool ImplementsINativeObject (TypeDefinition definition)
		{
			if (definition == null)
				return false;

			if (definition.FullName == kINativeObject)
				return true;

			if (!definition.HasInterfaces)
				return false;

			return definition.Interfaces.Any (iface => IsINativeObject (iface.InterfaceType));
		}

		bool IsINativeObject (TypeReference reference)
		{
			if (reference == null)
				return false;

			if (reference.FullName == kINativeObject)
				return true;

			if (reference.Module != null) {
				var resolvedReference = reference.Resolve ();
				return ImplementsINativeObject (resolvedReference);
			}
			return false;
		}

		public static bool? HasSkipRegistration (TypeDefinition type)
		{
			if (!type.HasCustomAttributes)
				return null;
			var attr = type.CustomAttributes.FirstOrDefault (attribute => attribute.AttributeType.FullName == kRegisterAttribute);
			if (attr == null)
				return null;
			if (!attr.HasProperties)
				return null;
			var prop = attr.Properties.FirstOrDefault (property => property.Name == kSkipRegistration);
			if (prop.Argument.Value is bool propValue)
				return propValue;
			return null;
		}

		public static bool HasRegisterAttribute (TypeDefinition type)
		{
			if (!type.HasCustomAttributes)
				return false;
			return type.CustomAttributes.Any (attribute => RegisterAttributeName (attribute) != null);
		}

		public static bool HasModelAttribute (TypeDefinition type)
		{
			if (!type.HasCustomAttributes)
				return false;
			return type.CustomAttributes.Any (attribute => attribute.AttributeType.FullName == kModelAttribute);
		}

		public static string RegisterAttributeName (TypeDefinition type)
		{
			foreach (var attr in type.CustomAttributes) {
				var registerName = RegisterAttributeName (attr);
				if (registerName != null)
					return registerName;
			}
			return null;
		}

		static string RegisterAttributeName (CustomAttribute attribute)
		{
			return FindAttributeValueByName (attribute, kRegisterAttribute);
		}

		public static bool HasProtocolAttribute (TypeDefinition type)
		{
			if (!type.HasCustomAttributes)
				return false;
			return type.CustomAttributes.Any (attribute => ProtocolAttributeName (attribute) != null);
		}

		public static string ProtocolAttributeName (TypeDefinition type)
		{
			if (!type.HasCustomAttributes)
				return null;
			foreach (var attr in type.CustomAttributes) {
				var registerName = ProtocolAttributeName (attr);
				if (registerName != null)
					return registerName;
			}
			return null;
		}

		static string ProtocolAttributeName (CustomAttribute attribute)
		{
			return FindAttributeValueByName (attribute, kProtocolAttribute);
		}


		static string FindAttributeValueByName (CustomAttribute attribute, string typeName)
		{
			if (attribute.AttributeType.FullName != typeName)
				return null;
			return ConstructorArgValueAsString (attribute, "name") ?? PropertyNameValueAsString (attribute, "Name");
		}

		static string PropertyNameValueAsString (CustomAttribute attribute, string propName)
		{
			if (!attribute.HasProperties)
				return null;
			foreach (var prop in attribute.Properties) {
				if (prop.Name == propName) {
					return prop.Argument.Value as string;
				}
			}
			return null;
		}


		List<TypeDefinition> ApplyIncludeExcludeRules (IEnumerable<TypeDefinition> typeList)
		{
			var types = new HashSet<TypeDefinition> ();
			var notExcluded = Excludes.Excluding (typeList, typeDefinition => typeDefinition.FullName);
			foreach (var type in notExcluded)
				types.Add (type);
			var reallyInclude = Includes.Including (typeList, typeDefinition => typeDefinition.FullName);
			foreach (var type in reallyInclude)
				types.Add (type);
			return types.ToList ();
		}

		public static string PathForPlatform (PlatformName platform)
		{
			switch (platform) {
			case PlatformName.iOS:
				return "/Library/Frameworks/Xamarin.iOS.framework/Versions/Current/lib/mono/Xamarin.iOS/Xamarin.iOS.dll";
			case PlatformName.macOS:
				return "/Library/Frameworks/Xamarin.Mac.framework/Versions/Current/lib/mono/Xamarin.Mac/Xamarin.Mac.dll";
			case PlatformName.tvOS:
				return "/Library/Frameworks/Xamarin.iOS.framework/Versions/Current/lib/mono/Xamarin.TVOS/Xamarin.TVOS.dll";
			case PlatformName.watchOS:
				return "/Library/Frameworks/Xamarin.iOS.framework/Versions/Current/lib/mono/Xamarin.WatchOS/Xamarin.WatchOS.dll";
			default:
				throw new NotSupportedException ($"Unsupporting platform for binding import: {platform}");
			}
		}

		static string ConstructorArgValueAsString (CustomAttribute attribute, string argumentName)
		{
			var index = ConstructorIndexOf (attribute, argumentName);
			if (index < 0)
				return null;
			return attribute.ConstructorArguments [index].Value as string;
		}

		static int ConstructorIndexOf (CustomAttribute attr, string name)
		{
			if (!attr.HasConstructorArguments)
				return -1;
			for (int i = 0; i < attr.ConstructorArguments.Count; i++) {
				if (attr.Constructor.Parameters [i].Name == name)
					return i;
			}
			return -1;
		}

		// Always call filter before remap
		public static bool FilterModuleAndName (PlatformName platform, string moduleName, ref string name)
		{
			// return false to skip
			// return true to use.

			if (ModuleSkipForPlatform (platform).Contains (moduleName))
				return false;
			if (moduleName == "System" && name == "nfloat") {
				return false;
			}
			if (TypeSkipForPlatform (platform).Contains ($"{moduleName}.{name}"))
				return false;
			return true;
		}

		public static void RemapModuleAndName (PlatformName platform, ref string moduleName, ref string name, TypeType type)
		{
			if (moduleName == "UIKit" && name == "UIControlEvent")
				name = "UIControlEvents";
			if (moduleName == "AudioToolboc")
				moduleName = "CoreAudio";
			else if (moduleName == "PdfKit")
				moduleName = "PDFKit";


			// TODO: https://github.com/xamarin/maccore/issues/1281
			// There is the cases where a protocol name conflicts with a class name
			// and swift has a different name for the protocol
    			if (name == "NFCReaderSession" && type == TypeType.Interface)
				name = "NFCReaderSessionProtocol";
			else if (name == "SCNAnimation" && type == TypeType.Interface)
				name = "SCNAnimationProtocol";
			else if (name == "NSObject" && type == TypeType.Interface)
				name = "NSObjectProtocol";
			else if (name == "FIFinderSync" && type == TypeType.Interface)
				name = "FIFinderSyncProtocol";
			else if (name == "NSAccessibilityElement" && type == TypeType.Interface)
				name = "NSAccessibilityElementProtocol";
			else {
				string result;
				if (TypeMapForPlatform (platform).TryGetValue ($"{moduleName}.{name}", out result))
					name = result;
			}

		}

		static partial void AvailableMapIOS (ref Dictionary<string, string> result);
		static partial void AvailableMapMacOS (ref Dictionary<string, string> result);

		public static Dictionary<string, string> AvailableMapForPlatform (PlatformName platform)
		{
			var result = new Dictionary<string, string> ();
			switch (platform) {
			case PlatformName.iOS:
				AvailableMapIOS (ref result);
				break;
			case PlatformName.macOS:
				AvailableMapMacOS (ref result);
				break;
			default:
				break;
			}
			return result;
		}

		static partial void ModulesToSkipIOS (ref HashSet<string> result);
		static partial void ModulesToSkipMacOS (ref HashSet<string> result);

		static HashSet<string> ModuleSkipForPlatform (PlatformName platform)
		{
			var result = new HashSet<string> ();
			switch (platform) {
			case PlatformName.iOS:
				ModulesToSkipIOS (ref result);
				break;
			case PlatformName.macOS:
				ModulesToSkipMacOS (ref result);
				break;
			default:
				break;
			}
			return result;
		}


		static partial void TypesToSkipIOS (ref HashSet<string> result);
		static partial void TypesToSkipMacOS (ref HashSet<string> result);

		static HashSet<string> TypeSkipForPlatform (PlatformName platform)
		{
			var result = new HashSet<string> ();
			switch (platform) {
			case PlatformName.iOS:
				TypesToSkipIOS (ref result);
				break;
			case PlatformName.macOS:
				TypesToSkipMacOS (ref result);
				break;
			default:
				break;
			}
			return result;
		}

		static partial void TypeNamesToMapIOS (ref Dictionary<string, string> result);
		static partial void TypeNamesToMapMacOS (ref Dictionary<string, string> result);

		static Dictionary <string, string> TypeMapForPlatform (PlatformName platform)
		{
			var result = new Dictionary<string, string> ();
			switch (platform) {
			case PlatformName.iOS:
				TypeNamesToMapIOS (ref result);
				break;
			case PlatformName.macOS:
				TypeNamesToMapMacOS (ref result);
				break;
			default:
				break;
			}
			return result;
		}


	}
}
