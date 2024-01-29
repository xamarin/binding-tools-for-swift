// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Linq;
using NUnit.Framework;
using SwiftReflector.SwiftXmlReflection;
using System.Collections.Generic;
using NUnit.Framework.Legacy;

namespace XmlReflectionTests {
	[TestFixture]
	[Parallelizable (ParallelScope.All)]
	public class StaticXmlTests {
		[Test]
		public void SingleFunctionInModule ()
		{
			string xmlText = "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
								"<xamreflect version=\"1.0\">" +
								"   <modulelist>" +
								"      <module name=\"None1\">" +
								"         <func name=\"returns5\" isProperty=\"false\" returnType=\"Swift.Int\" accessibility=\"Public\" isStatic=\"false\" isFinal=\"false\">" +
								"            <parameterlists>" +
								"               <parameterlist index=\"0\">" +
								"               </parameterlist>" +
								"            </parameterlists>" +
								"         </func>" +
								"      </module>" +
								"   </modulelist>" +
								"</xamreflect>";
			List<ModuleDeclaration> modules = Reflector.FromXml (xmlText, typeDatabase: null);
			ClassicAssert.NotNull (modules);
			ClassicAssert.AreEqual (1, modules.Count);
			ClassicAssert.AreEqual ("None1", modules [0].Name);
			List<FunctionDeclaration> functions = modules [0].Functions.ToList ();
			ClassicAssert.AreEqual (1, functions.Count);
			ClassicAssert.AreEqual ("returns5", functions [0].Name);
			ClassicAssert.AreEqual (1, functions [0].ParameterLists.Count);
			ClassicAssert.AreEqual (0, functions [0].ParameterLists [0].Count);
			ClassicAssert.AreEqual ("Swift.Int", functions [0].ReturnTypeName);
			ClassicAssert.AreEqual (Accessibility.Public, functions [0].Access);
			ClassicAssert.AreEqual (false, functions [0].IsStatic);
			ClassicAssert.AreEqual (false, functions [0].IsProperty);
			ClassicAssert.AreEqual (false, functions [0].IsFinal);
			ClassicAssert.AreEqual ("None1.returns5", functions [0].ToFullyQualifiedName (true));
			ClassicAssert.AreEqual ("returns5", functions [0].ToFullyQualifiedName (false));
		}


		[Test]
		public void SingleFunctionThrowsInModule ()
		{
			string xmlText = "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
								"<xamreflect version=\"1.0\">" +
								"   <modulelist>" +
								"      <module name=\"None1\">" +
								"         <func name=\"returns5\" hasThrows=\"true\" isProperty=\"false\" returnType=\"Swift.Int\" accessibility=\"Public\" isStatic=\"false\" isFinal=\"false\">" +
								"            <parameterlists>" +
								"               <parameterlist index=\"0\">" +
								"               </parameterlist>" +
								"            </parameterlists>" +
								"         </func>" +
								"      </module>" +
								"   </modulelist>" +
								"</xamreflect>";
			List<ModuleDeclaration> modules = Reflector.FromXml (xmlText, typeDatabase: null);
			ClassicAssert.NotNull (modules);
			ClassicAssert.AreEqual (1, modules.Count);
			ClassicAssert.AreEqual ("None1", modules [0].Name);
			List<FunctionDeclaration> functions = modules [0].Functions.ToList ();
			ClassicAssert.AreEqual (1, functions.Count);
			ClassicAssert.AreEqual ("returns5", functions [0].Name);
			ClassicAssert.AreEqual (1, functions [0].ParameterLists.Count);
			ClassicAssert.AreEqual (0, functions [0].ParameterLists [0].Count);
			ClassicAssert.AreEqual ("Swift.Int", functions [0].ReturnTypeName);
			ClassicAssert.AreEqual (Accessibility.Public, functions [0].Access);
			ClassicAssert.AreEqual (false, functions [0].IsStatic);
			ClassicAssert.AreEqual (false, functions [0].IsProperty);
			ClassicAssert.AreEqual (false, functions [0].IsFinal);
			ClassicAssert.AreEqual ("None1.returns5", functions [0].ToFullyQualifiedName (true));
			ClassicAssert.AreEqual (true, functions [0].HasThrows);
			ClassicAssert.AreEqual ("returns5", functions [0].ToFullyQualifiedName (false));
		}


		[Test]
		public void SinglePrivateFunctionInModule ()
		{
			string xmlText = "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
				"<xamreflect version=\"1.0\">" +
				"   <modulelist>" +
				"      <module name=\"None1\">" +
				"         <func name=\"returns5\" isProperty=\"false\" returnType=\"Swift.Int\" accessibility=\"Private\" isStatic=\"false\" isFinal=\"false\">" +
				"            <parameterlists>" +
				"               <parameterlist index=\"0\">" +
				"               </parameterlist>" +
				"            </parameterlists>" +
				"         </func>" +
				"      </module>" +
				"   </modulelist>" +
				"</xamreflect>";
			List<ModuleDeclaration> modules = Reflector.FromXml (xmlText, typeDatabase: null);
			ClassicAssert.NotNull (modules);
			ClassicAssert.AreEqual (1, modules.Count);
			ClassicAssert.AreEqual ("None1", modules [0].Name);
			List<FunctionDeclaration> functions = modules [0].Functions.ToList ();
			ClassicAssert.AreEqual (1, functions.Count);
			ClassicAssert.AreEqual ("returns5", functions [0].Name);
			ClassicAssert.AreEqual (1, functions [0].ParameterLists.Count);
			ClassicAssert.AreEqual (0, functions [0].ParameterLists [0].Count);
			ClassicAssert.AreEqual ("Swift.Int", functions [0].ReturnTypeName);
			ClassicAssert.AreEqual (Accessibility.Private, functions [0].Access);
			ClassicAssert.AreEqual (false, functions [0].IsStatic);
			ClassicAssert.AreEqual (false, functions [0].IsProperty);
			ClassicAssert.AreEqual (false, functions [0].IsFinal);
		}


		[Test]
		public void SingleInternalFunctionInModule ()
		{
			string xmlText = "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
				"<xamreflect version=\"1.0\">" +
				"   <modulelist>" +
				"      <module name=\"None1\">" +
				"         <func name=\"returns5\" isProperty=\"false\" returnType=\"Swift.Int\" accessibility=\"Internal\" isStatic=\"false\" isFinal=\"false\">" +
				"            <parameterlists>" +
				"               <parameterlist index=\"0\">" +
				"               </parameterlist>" +
				"            </parameterlists>" +
				"         </func>" +
				"      </module>" +
				"   </modulelist>" +
				"</xamreflect>";
			List<ModuleDeclaration> modules = Reflector.FromXml (xmlText, typeDatabase: null);
			ClassicAssert.NotNull (modules);
			ClassicAssert.AreEqual (1, modules.Count);
			ClassicAssert.AreEqual ("None1", modules [0].Name);
			List<FunctionDeclaration> functions = modules [0].Functions.ToList ();
			ClassicAssert.AreEqual (1, functions.Count);
			ClassicAssert.AreEqual ("returns5", functions [0].Name);
			ClassicAssert.AreEqual (1, functions [0].ParameterLists.Count);
			ClassicAssert.AreEqual (0, functions [0].ParameterLists [0].Count);
			ClassicAssert.AreEqual ("Swift.Int", functions [0].ReturnTypeName);
			ClassicAssert.AreEqual (Accessibility.Internal, functions [0].Access);
			ClassicAssert.AreEqual (false, functions [0].IsStatic);
			ClassicAssert.AreEqual (false, functions [0].IsProperty);
			ClassicAssert.AreEqual (false, functions [0].IsFinal);
		}

		[Test]
		public void SinglePropertyInModule ()
		{
			string xmlText = "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
					 "<xamreflect version=\"1.0\">" +
					 "   <modulelist>" +
					 "      <module name=\"None1\">" +
					 "         <property name=\"topLevelVar\" type=\"Swift.Int\" storage=\"Stored\" accessibility=\"Public\" isStatic=\"false\" />" +
					 "      </module>" +
					 "   </modulelist>" +
					 "</xamreflect>";
			List<ModuleDeclaration> modules = Reflector.FromXml (xmlText, typeDatabase: null);
			ClassicAssert.NotNull (modules);
			ClassicAssert.AreEqual (1, modules.Count);
			ClassicAssert.AreEqual ("None1", modules [0].Name);
			List<PropertyDeclaration> props = modules [0].Properties.ToList ();
			ClassicAssert.AreEqual (1, props.Count);
			ClassicAssert.AreEqual ("topLevelVar", props [0].Name);
			ClassicAssert.AreEqual ("Swift.Int", props [0].TypeName);
			ClassicAssert.AreEqual (false, props [0].IsStatic);
			ClassicAssert.AreEqual (StorageKind.Stored, props [0].Storage);
			ClassicAssert.AreEqual (Accessibility.Public, props [0].Access);
		}

		[Test]
		public void SinglePrivatePropertyInModule ()
		{
			string xmlText = "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
				"<xamreflect version=\"1.0\">" +
				"   <modulelist>" +
				"      <module name=\"None1\">" +
				"         <property name=\"topLevelVar\" type=\"Swift.Int\" storage=\"Stored\" accessibility=\"Private\" isStatic=\"false\" />" +
				"      </module>" +
				"   </modulelist>" +
				"</xamreflect>";
			List<ModuleDeclaration> modules = Reflector.FromXml (xmlText, typeDatabase: null);
			ClassicAssert.NotNull (modules);
			ClassicAssert.AreEqual (1, modules.Count);
			ClassicAssert.AreEqual ("None1", modules [0].Name);
			List<PropertyDeclaration> props = modules [0].Properties.ToList ();
			ClassicAssert.AreEqual (1, props.Count);
			ClassicAssert.AreEqual ("topLevelVar", props [0].Name);
			ClassicAssert.AreEqual ("Swift.Int", props [0].TypeName);
			ClassicAssert.AreEqual (false, props [0].IsStatic);
			ClassicAssert.AreEqual (StorageKind.Stored, props [0].Storage);
			ClassicAssert.AreEqual (Accessibility.Private, props [0].Access);
		}
		[Test]
		public void SingleInternalPropertyInModule ()
		{
			string xmlText = "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
				"<xamreflect version=\"1.0\">" +
				"   <modulelist>" +
				"      <module name=\"None1\">" +
				"         <property name=\"topLevelVar\" type=\"Swift.Int\" storage=\"Stored\" accessibility=\"Internal\" isStatic=\"false\" />" +
				"      </module>" +
				"   </modulelist>" +
				"</xamreflect>";
			List<ModuleDeclaration> modules = Reflector.FromXml (xmlText, typeDatabase: null);
			ClassicAssert.NotNull (modules);
			ClassicAssert.AreEqual (1, modules.Count);
			ClassicAssert.AreEqual ("None1", modules [0].Name);
			List<PropertyDeclaration> props = modules [0].Properties.ToList ();
			ClassicAssert.AreEqual (1, props.Count);
			ClassicAssert.AreEqual ("topLevelVar", props [0].Name);
			ClassicAssert.AreEqual ("Swift.Int", props [0].TypeName);
			ClassicAssert.AreEqual (false, props [0].IsStatic);
			ClassicAssert.AreEqual (StorageKind.Stored, props [0].Storage);
			ClassicAssert.AreEqual (Accessibility.Internal, props [0].Access);
		}

		[Test]
		public void SingleClassInModule ()
		{
			string xmlText = "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
				"				<xamreflect version=\"1.0\">" +
				"				<modulelist>" +
				"				<module name=\"None1\">" +
				"				<typedeclaration kind=\"class\" name=\"Foo\" accessibility=\"Public\" isObjC=\"false\" isFinal=\"false\">" +
				"				<members>" +
				"				<func name=\".dtor\" isProperty=\"false\" returnType=\"()\" accessibility=\"Public\" isStatic=\"false\" isFinal=\"false\">" +
				"				<parameterlists>" +
				"				<parameterlist index=\"0\">" +
				"				<parameter index=\"0\" type=\"None1.Foo\" name=\"self\" nameRequired=\"true\" />" +
				"				</parameterlist>" +
				"				</parameterlists>" +
				"				</func>" +
				"				<func name=\".ctor\" isProperty=\"false\" returnType=\"None1.Foo\" accessibility=\"Internal\" isStatic=\"false\" isFinal=\"false\">" +
				"				<parameterlists>" +
				"				<parameterlist index=\"0\">" +
				"				<parameter index=\"0\" type=\"None1.Foo\" name=\"self\" nameRequired=\"true\" />" +
				"				</parameterlist>" +
				"				<parameterlist index=\"1\">" +
				"				</parameterlist>" +
				"				</parameterlists>" +
				"				</func>" +
				"				</members>" +
				"				</typedeclaration>" +
				"				</module>" +
				"				</modulelist>" +
				"				</xamreflect>";
			List<ModuleDeclaration> modules = Reflector.FromXml (xmlText, typeDatabase: null);
			ClassicAssert.NotNull (modules);
			ClassicAssert.AreEqual (1, modules.Count);
			ClassicAssert.AreEqual ("None1", modules [0].Name);
			List<ClassDeclaration> classes = modules [0].Classes.ToList ();
			ClassicAssert.AreEqual (1, classes.Count);
			ClassicAssert.AreEqual ("Foo", classes [0].Name);
			ClassicAssert.AreEqual (2, classes [0].Members.Count);
			ClassicAssert.AreEqual (0, classes [0].InnerClasses.Count);
			ClassicAssert.AreEqual (0, classes [0].InnerStructs.Count);
			ClassicAssert.AreEqual (TypeKind.Class, classes [0].Kind);
			ClassicAssert.AreEqual (Accessibility.Public, classes [0].Access);
			ClassicAssert.AreEqual ("None1.Foo", classes [0].ToFullyQualifiedName (true));
			ClassicAssert.AreEqual ("Foo", classes [0].ToFullyQualifiedName (false));
		}

		[Test]
		public void SinglePrivateClassInModule ()
		{
			string xmlText = "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
				"				<xamreflect version=\"1.0\">" +
				"				<modulelist>" +
				"				<module name=\"None1\">" +
				"				<typedeclaration kind=\"class\" name=\"Foo\" accessibility=\"Private\" isObjC=\"false\" isFinal=\"false\">" +
				"				<members>" +
				"				<func name=\".dtor\" isProperty=\"false\" returnType=\"()\" accessibility=\"Public\" isStatic=\"false\" isFinal=\"false\">" +
				"				<parameterlists>" +
				"				<parameterlist index=\"0\">" +
				"				<parameter index=\"0\" type=\"None1.Foo\" name=\"self\" nameRequired=\"true\" />" +
				"				</parameterlist>" +
				"				</parameterlists>" +
				"				</func>" +
				"				<func name=\".ctor\" isProperty=\"false\" returnType=\"None1.Foo\" accessibility=\"Internal\" isStatic=\"false\" isFinal=\"false\">" +
				"				<parameterlists>" +
				"				<parameterlist index=\"0\">" +
				"				<parameter index=\"0\" type=\"None1.Foo\" name=\"self\" nameRequired=\"true\" />" +
				"				</parameterlist>" +
				"				<parameterlist index=\"1\">" +
				"				</parameterlist>" +
				"				</parameterlists>" +
				"				</func>" +
				"				</members>" +
				"				</typedeclaration>" +
				"				</module>" +
				"				</modulelist>" +
				"				</xamreflect>";
			List<ModuleDeclaration> modules = Reflector.FromXml (xmlText, typeDatabase: null);
			ClassicAssert.NotNull (modules);
			ClassicAssert.AreEqual (1, modules.Count);
			ClassicAssert.AreEqual ("None1", modules [0].Name);
			List<ClassDeclaration> classes = modules [0].Classes.ToList ();
			ClassicAssert.AreEqual (1, classes.Count);
			ClassicAssert.AreEqual ("Foo", classes [0].Name);
			ClassicAssert.AreEqual (2, classes [0].Members.Count);
			ClassicAssert.AreEqual (0, classes [0].InnerClasses.Count);
			ClassicAssert.AreEqual (0, classes [0].InnerStructs.Count);
			ClassicAssert.AreEqual (TypeKind.Class, classes [0].Kind);
			ClassicAssert.AreEqual (Accessibility.Private, classes [0].Access);
		}

		[Test]
		public void SingleInternalClassInModule ()
		{
			string xmlText = "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
				"				<xamreflect version=\"1.0\">" +
				"				<modulelist>" +
				"				<module name=\"None1\">" +
				"				<typedeclaration kind=\"class\" name=\"Foo\" accessibility=\"Internal\" isObjC=\"false\" isFinal=\"false\">" +
				"				<members>" +
				"				<func name=\".dtor\" isProperty=\"false\" returnType=\"()\" accessibility=\"Public\" isStatic=\"false\" isFinal=\"false\">" +
				"				<parameterlists>" +
				"				<parameterlist index=\"0\">" +
				"				<parameter index=\"0\" type=\"None1.Foo\" name=\"self\" nameRequired=\"true\" />" +
				"				</parameterlist>" +
				"				</parameterlists>" +
				"				</func>" +
				"				<func name=\".ctor\" isProperty=\"false\" returnType=\"None1.Foo\" accessibility=\"Internal\" isStatic=\"false\" isFinal=\"false\">" +
				"				<parameterlists>" +
				"				<parameterlist index=\"0\">" +
				"				<parameter index=\"0\" type=\"None1.Foo\" name=\"self\" nameRequired=\"true\" />" +
				"				</parameterlist>" +
				"				<parameterlist index=\"1\">" +
				"				</parameterlist>" +
				"				</parameterlists>" +
				"				</func>" +
				"				</members>" +
				"				</typedeclaration>" +
				"				</module>" +
				"				</modulelist>" +
				"				</xamreflect>";
			List<ModuleDeclaration> modules = Reflector.FromXml (xmlText, typeDatabase: null);
			ClassicAssert.NotNull (modules);
			ClassicAssert.AreEqual (1, modules.Count);
			ClassicAssert.AreEqual ("None1", modules [0].Name);
			List<ClassDeclaration> classes = modules [0].Classes.ToList ();
			ClassicAssert.AreEqual (1, classes.Count);
			ClassicAssert.AreEqual ("Foo", classes [0].Name);
			ClassicAssert.AreEqual (2, classes [0].Members.Count);
			ClassicAssert.AreEqual (0, classes [0].InnerClasses.Count);
			ClassicAssert.AreEqual (0, classes [0].InnerStructs.Count);
			ClassicAssert.AreEqual (TypeKind.Class, classes [0].Kind);
			ClassicAssert.AreEqual (Accessibility.Internal, classes [0].Access);
		}
	}
}

