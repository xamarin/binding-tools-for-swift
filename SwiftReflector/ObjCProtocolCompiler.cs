// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using SwiftReflector.IOUtils;
using SwiftReflector.SwiftXmlReflection;
using System.Linq;
using SwiftReflector.TypeMapping;
using SwiftReflector.ExceptionTools;
using Dynamo.CSLang;
using System.IO;
using System.Text;
using Dynamo;
using System.Reflection;
using ObjCRuntime;

namespace SwiftReflector {
	public class ObjCProtocolCompiler {
		List<ProtocolDeclaration> protocols = new List<ProtocolDeclaration> ();
		TempDirectoryFilenameProvider provider;
		ModuleDeclaration module;
		string swiftLibPath;
		string outputDirectory;
		TypeMapper typeMapper;
		WrappingResult wrapper;
		ErrorHandling errors;
		bool verbose;
		PlatformName targetPlatform;

		TopLevelFunctionCompiler topLevelFunctionCompiler;

		static CSAttribute kProtocolModelAttribute = new CSAttribute (new CSIdentifier ("Protocol, Model"), null, true);
		static CSAttribute kProtocolAttribute = new CSAttribute (new CSIdentifier ("Protocol"), null, true);
		static CSAttribute kAbstractAttribute = new CSAttribute (new CSIdentifier ("Abstract"), null, true);
		static CSAttribute kBaseTypeNSObject = null;
		const string kBgenPath = "/Library/Frameworks/Xamarin.Mac.framework/Versions/Current/bin/bgen";
		const string kBgeniOSPath = "/Library/Frameworks/Xamarin.iOS.framework/Versions/Current/bin/bgen";
		const string kManifestFile = "BgenManifestOutput.txt";
		const string kBgenOutput = "bgenout";

		static ObjCProtocolCompiler ()
		{
			var arg = new CSSimpleType ("NSObject").Typeof ();
			var argList = new CSArgumentList ();
			argList.Add (new CSArgument (arg));
			kBaseTypeNSObject = new CSAttribute (new CSIdentifier ("BaseType"), argList, true);
		}

		public ObjCProtocolCompiler (PlatformName targetPlatform, IEnumerable<ProtocolDeclaration> protocols, TempDirectoryFilenameProvider provider,
		                             ModuleDeclaration module, string swiftLibPath, string outputDirectory, TypeMapper typeMapper,
		                             WrappingResult wrapper, ErrorHandling errors, bool verbose)
		{
			this.targetPlatform = targetPlatform;
			this.protocols = protocols.Where (p => p.IsObjC && p.IsPublicOrOpen && !p.IsUnavailable && !p.IsDeprecated).ToList ();
			this.provider = provider;
			this.module = module;
			this.swiftLibPath = swiftLibPath;
			this.outputDirectory = outputDirectory;
			this.typeMapper = typeMapper;
			this.wrapper = wrapper;
			this.errors = errors;
			this.verbose = verbose;

			topLevelFunctionCompiler = new TopLevelFunctionCompiler (this.typeMapper);
		}

		public void Compile()
		{
			if (verbose)
				NewClassCompiler.ReportCompileStatus(protocols, "ObjC protocols");

			if (!protocols.Any ())
				return;
			
			var use = new CSUsingPackages ("System", "ObjCRuntime", "Foundation");
			string nameSpace = typeMapper.MapModuleToNamespace (module.Name);
			var nm = new CSNamespace (nameSpace);

			var objCClasses = ObjCClasses ();

			foreach (var cl in objCClasses) {
				var csIface = BuildCSIface (cl, use);
				nm.Block.Add (csIface);
			}

			foreach (var proto in protocols) {
				if (proto.IsDeprecated || proto.IsUnavailable)
					continue;
				try {
					var iface = CompileProtocol (proto, use);
					nm.Block.Add (iface);

				} catch (Exception e) {
					errors.Add (e);
				}
			}
			var csfile = new CSFile (use, new CSNamespace [] { nm });

			string csOutputFileName = $"{nameSpace}ObjCProtocol.cs";
			NewClassCompiler.WriteCSFile (csOutputFileName, outputDirectory, csfile);

			var needsSwiftRuntimeLibrary = use.Elements.Any (elem => (elem as CSUsing).Contents.Contains ("SwiftRuntimeLibrary"));

			YouCantBtouchThis (csOutputFileName, needsSwiftRuntimeLibrary);
			CleanUpExtraneousFiles (protocols, csOutputFileName);
		}

		void CleanUpExtraneousFiles (List <ProtocolDeclaration> protos, string csOutputFileName)
		{
			var targetDirectory = Path.Combine (outputDirectory, kBgenOutput);

			var initialCsFileName = Path.Combine (outputDirectory, csOutputFileName);
			if (File.Exists (initialCsFileName))
				File.Delete (initialCsFileName);

			var dllFileName = Path.ChangeExtension (initialCsFileName, "dll");
			if (File.Exists (dllFileName))
				File.Delete (dllFileName);

			var outputFiles = ReadOutputFileList ();

			foreach (var file in outputFiles) {
				var sourcePath = Path.Combine (outputDirectory, file);
				var fileName = Path.GetFileName (sourcePath);
				if (fileName == "Messaging.g.cs")
					fileName = module.Name + fileName;
				var destPath = Path.Combine (outputDirectory, fileName);
				if (File.Exists (destPath))
					File.Delete (destPath);
				if (File.Exists (sourcePath))
					File.Copy (sourcePath, destPath);
				PostProcessFile (destPath);
			}

			File.Delete (Path.Combine (outputDirectory, kManifestFile));

			if (Directory.Exists (targetDirectory))
				Directory.Delete (targetDirectory, true);
			var objCRuntimeDir = Path.Combine (outputDirectory, "ObjCRuntime");
			if (Directory.Exists (objCRuntimeDir))
				Directory.Delete (objCRuntimeDir, true);
		}

		static void PostProcessFile(string pathToFile)
		{
			var temp = Path.GetTempFileName ();

			var keepLines = File.ReadLines (pathToFile).Where (line => !line.StartsWith ("using QTKit"));
			File.WriteAllLines (temp, keepLines);
			File.Delete (pathToFile);
			File.Move (temp, pathToFile);			
		}

		void YouCantBtouchThis (string csOutputFileName, bool needsSwiftRuntimeLibrary)
		{
			var btoucher = BtouchPath ();
			if (!File.Exists (btoucher)) {
				errors.Add (new FileNotFoundException ($"Unable to find bgen at {kBgenPath}"));
				return;
			}

			var args = new StringBuilder ();

			BuildBtouchArgs (csOutputFileName, args, needsSwiftRuntimeLibrary);

			try {
				ExecAndCollect.Run (btoucher, args.ToString (), workingDirectory: outputDirectory, verbose: false);
			} catch (Exception e) {
				errors.Add (e);
			}
		}


		CSInterface BuildCSIface (ClassDeclaration cl, CSUsingPackages use)
		{
			var csName = NewClassCompiler.StubbedClassName (cl.ToFullyQualifiedName (true), typeMapper);
			var iface = new CSInterface (CSVisibility.Public, csName);
			return iface;
		}

		IEnumerable<ClassDeclaration> ObjCClasses ()
		{
			return module.Classes.Where (cl =>
					      cl.IsPublicOrOpen && cl.IsObjC && !cl.IsDeprecated && !cl.IsUnavailable);
		}

		CSInterface CompileProtocol (ProtocolDeclaration proto, CSUsingPackages use)
		{
			var iface = new CSInterface (CSVisibility.Public, proto.Name);
			kProtocolAttribute.AttachBefore (iface);
			var filteredInheritance = proto.Inheritance.FindAll (inh => !TypeSpecIsAnyOrAnyObject (inh.InheritedTypeSpec));
			iface.Inheritance.AddRange (filteredInheritance.Select (inh => {
				var netIface = typeMapper.GetDotNetNameForTypeSpec (inh.InheritedTypeSpec);
				use.AddIfNotPresent (netIface.Namespace);
				return new CSIdentifier (netIface.TypeName);
			}));

			foreach (var funcDecl in proto.AllMethodsNoCDTor ()) {
				if (funcDecl.IsProperty && !funcDecl.IsSubscript)
					continue;
				try {
					CompileFunc (funcDecl, proto, iface, use);
				} catch (Exception e) {
					errors.Add (ErrorHelper.CreateWarning (ReflectorError.kWrappingBase + 14, e, $"Error compiling ObjC protocol method {proto.ToFullyQualifiedName ()}.{funcDecl.Name}"));
				}
			}

			foreach (var propDecl in proto.AllProperties ()) {
				try {
					CompileProp (propDecl, proto, iface, use);
				} catch (Exception e) {
					errors.Add (ErrorHelper.CreateWarning (ReflectorError.kWrappingBase + 14, e, $"Error compiling ObjC protocol property {proto.ToFullyQualifiedName ()}.{propDecl.Name}"));
				}
			}
			var anyRequired = proto.AllMethodsNoCDTor ().Any (func => !func.IsOptional);
			if (anyRequired)
				kAbstractAttribute.AttachBefore (iface);

			return iface;
		}

		void CompileFunc (FunctionDeclaration funcDecl, ProtocolDeclaration proto, CSInterface iface, CSUsingPackages use)
		{
			var homonymSuffix = Homonyms.HomonymSuffix (funcDecl, proto.Members.OfType<FunctionDeclaration> (), typeMapper);

			var publicMethod = topLevelFunctionCompiler.CompileMethod (funcDecl, use, swiftLibPath, null, null, false, false, false);
			// recast with no visibility and with the homonym suffix, if any
			publicMethod = new CSMethod (CSVisibility.None, CSMethodKind.Interface, publicMethod.Type,
						     new CSIdentifier (publicMethod.Name.Name + homonymSuffix), publicMethod.Parameters, null);
			ExportAttribute (funcDecl.ObjCSelector).AttachBefore (publicMethod);
			if (!funcDecl.IsOptional)
				kAbstractAttribute.AttachBefore (publicMethod);
			iface.Methods.Add (publicMethod);
		}

		void CompileProp (PropertyDeclaration propDecl, ProtocolDeclaration proto, CSInterface iface, CSUsingPackages use)
		{
			var getter = propDecl.GetGetter ();
			var setter = propDecl.GetSetter ();
			var publicProp = topLevelFunctionCompiler.CompileProperty (use, null, getter, setter, CSMethodKind.None);
			publicProp = new CSProperty (publicProp.PropType, CSMethodKind.None, publicProp.Name,
			                             CSVisibility.None, new CSCodeBlock (), CSVisibility.None, setter != null ? new CSCodeBlock () : null);
			ExportAttribute (getter.ObjCSelector).AttachBefore (publicProp);
			if (!propDecl.IsOptional)
				kAbstractAttribute.AttachBefore (publicProp);
			iface.Properties.Add (publicProp);
		}



		static CSAttribute ExportAttribute (string objcSelector)
		{
			var paramList = new CSArgumentList ();
			paramList.Add (CSConstant.Val (objcSelector));
			return new CSAttribute (new CSIdentifier ("Export"), paramList, true);
		}

		string BtouchPath ()
		{
			switch (targetPlatform)
			{
			case PlatformName.macOS:
				return kBgenPath;
			case PlatformName.iOS:
			case PlatformName.tvOS:
			case PlatformName.watchOS:
				return kBgeniOSPath;
			default:
				throw new NotImplementedException ($"Need bgen path for {targetPlatform}");
			}
		}

		void BuildBtouchArgs (string csFile, StringBuilder args, bool needsSwiftRuntimeLibrary)
		{
			string framework = null;
			string baselib = null;
			string lib = null;
			string swiftRuntimeLibrary = needsSwiftRuntimeLibrary ? GetSwiftRuntimeLibrary () : "";
			if (targetPlatform == PlatformName.macOS) {
				lib = "/lib:/Library/Frameworks/Xamarin.Mac.framework/Versions/Current/lib/mono/4.5";
				baselib = "/baselib:/Library/Frameworks/Xamarin.Mac.framework/Versions/Current/lib/mono/4.5/Xamarin.Mac.dll";
				framework = "/target-framework=Xamarin.Mac,Version=v4.5,Profile=Full";

			} else if (targetPlatform == PlatformName.iOS) {
				lib = "/lib:/Library/Frameworks/Xamarin.iOS.framework/Versions/Current/lib/mono/Xamarin.iOS";
				baselib = "/baselib:/Library/Frameworks/Xamarin.iOS.framework/Versions/Current/lib/mono/Xamarin.iOS/Xamarin.iOS.dll";
				framework = "/target-framework=Xamarin.iOS,v1.0";
			} else if (targetPlatform == PlatformName.tvOS) {
				lib = "/lib:/Library/Frameworks/Xamarin.iOS.framework/Versions/Current/lib/mono/Xamarin.TVOS";
				baselib = "/baselib:/Library/Frameworks/Xamarin.iOS.framework/Versions/Current/lib/mono/Xamarin.TVOS/Xamarin.TVOS.dll";
				framework = "/target-framework=Xamarin.TVOS,v1.0";
			} else if (targetPlatform == PlatformName.watchOS) {
				lib = "/lib:/Library/Frameworks/Xamarin.iOS.framework/Versions/Current/lib/mono/Xamarin.watchOS";
				baselib = "/baselib:/Library/Frameworks/Xamarin.iOS.framework/Versions/Current/lib/mono/Xamarin.watchOS/Xamarin.watchOS.dll";
				framework = "/target-framework=Xamarin.WatchOS,v1.0";
			} else {
				throw new NotImplementedException ();
			}
			args.Append ("/v").Append (" ")
			    .Append (baselib).Append (" ")
			    .Append (lib).Append (" ")
			    .Append (swiftRuntimeLibrary).Append (" ")
			    .Append ("/unsafe").Append (" ")
			    .Append (csFile).Append (" ")
			    .Append (framework).Append (" ")
			    .Append ("-outdir=").Append("bgenout").Append (" ")
			    .Append ($"--sourceonly={kManifestFile}");
		}

		string GetSwiftRuntimeLibrary ()
		{
			var codeBase = Assembly.GetExecutingAssembly ().CodeBase;
			var uri = new UriBuilder (codeBase);
			var basepath = Path.GetDirectoryName (Uri.UnescapeDataString (uri.Path));

			switch (targetPlatform) {
			case PlatformName.macOS:
				basepath = Path.Combine (basepath, "../../../SwiftRuntimeLibrary.iOS/bin/Debug/SwiftRuntimeLibrary.macOS.dll");
				break;
			case PlatformName.iOS:
				basepath = Path.Combine (basepath, "../../../SwiftRuntimeLibrary.iOS/bin/Debug/SwiftRuntimeLibrary.iOS.dll");
				break;
			case PlatformName.tvOS:
				basepath = Path.Combine (basepath, "../../../SwiftRuntimeLibrary.iOS/bin/Debug/SwiftRuntimeLibrary.tvOS.dll");
				break;
			case PlatformName.watchOS:
				basepath = Path.Combine (basepath, "../../../SwiftRuntimeLibrary.iOS/bin/Debug/SwiftRuntimeLibrary.watchOS.dll");
				break;
			default:
				basepath = Path.Combine (basepath, $"../../../SwiftRuntimeLibrary.iOS/bin/Debug/SwiftRuntimeLibrary.{targetPlatform.ToString()}.dll");
				break;
			}

			if (!File.Exists (basepath)) {
				throw new NotImplementedException ($"Unable to find path to SwiftRuntimeLibrary for platform {targetPlatform} at location {basepath}.");
			}

			return "/r:" + basepath;
		}

		IEnumerable<string> ReadOutputFileList ()
		{
			var outputManifest = Path.Combine (outputDirectory, kManifestFile);
			return File.ReadLines (outputManifest);
		}

		static bool TypeSpecIsAnyOrAnyObject (TypeSpec spec)
		{
			if (spec is NamedTypeSpec ns) {
				return ns.Name == "Swift.Any" || ns.Name == "Swift.AnyObject";
			} else {
				return false;
			}
		}
	}
}
