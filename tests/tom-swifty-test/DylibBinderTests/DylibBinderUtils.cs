using System;
using System.Collections.Generic;
using System.IO;
using DylibBinder;
using SwiftReflector.SwiftXmlReflection;
using SwiftReflector.TypeMapping;

namespace tomwiftytest.DylibBinderTests {
	internal static class DylibBinderUtils {
		public static List<ModuleDeclaration> DylibBinderToModule (string directory, string moduleName)
		{
			var typeDatabase = new TypeDatabase ();
			var dbPath = Path.Combine (Directory.GetCurrentDirectory (), "../../../../bindings");
			foreach (var dbFile in Directory.GetFiles (dbPath, "*.xml")) {
				typeDatabase.Read (dbFile);
			}

			var tempFile = Path.GetTempFileName ();
			DylibBinderReflector.Reflect (directory, tempFile);
			return Reflector.FromXmlFile (tempFile, typeDatabase);
		}

		public static BaseDeclaration GetClassDeclaration (List<ModuleDeclaration> moduleDeclarations)
		{
			foreach (var moduleDeclaration in moduleDeclarations) {
				foreach (var decl in moduleDeclaration.Declarations) {
					if (decl is ClassDeclaration classDecl) {
						foreach (var member in classDecl.Members) {
							return member;
						}
					}
				}
			}
			return null;
		}

		public static BaseDeclaration GetStructDeclaration (List<ModuleDeclaration> moduleDeclarations)
		{
			foreach (var moduleDeclaration in moduleDeclarations) {
				foreach (var decl in moduleDeclaration.Declarations) {
					if (decl is StructDeclaration structDecl) {
						foreach (var member in structDecl.Members) {
							return member;
						}
					}
				}
			}
			return null;
		}
	}
}
