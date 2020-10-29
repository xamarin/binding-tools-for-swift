// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Dynamo;
using SwiftReflector.Inventory;
using tomwiftytest;
using SwiftReflector.IOUtils;
using SwiftReflector.TypeMapping;
using Dynamo.CSLang;

namespace SwiftReflector {
	[TestFixture]
	[Parallelizable (ParallelScope.All)]
	[RunWithLeaks]
	public class PropertyWrapSmokeTests {
		void WrapSinglePropertyWithCompiler (string type, string returnVal)
		{
			string simpleClass = $@"public final class Monty {{ public init() {{ }}
				public var val:{type} = {returnVal}; 
			}}";

			PropertyTestCore (simpleClass);
		}

		void WrapPropertyWithDifferentVisibleSet (string visibility)
		{
			string simpleClass = $@"public class Monty {{
    public init() {{ }}
    open {visibility}(set) weak var parent: Monty?
}}";

			PropertyTestCore (simpleClass);
		}

		static void PropertyTestCore (string simpleClass)
		{
			using (TempDirectoryFilenameProvider provider = new TempDirectoryFilenameProvider ()) {
				Utils.CompileSwift (simpleClass, provider);

				string libFileName = Path.Combine (provider.DirectoryPath, "libXython.dylib");
				var errors = new ErrorHandling ();
				ModuleInventory inventory = ModuleInventory.FromFile (libFileName, errors);
				Utils.CheckErrors (errors);

				Utils.CompileToCSharp (provider);
			}
		}

		[Test]
		public void WrapPropertyInt ()
		{
			WrapSinglePropertyWithCompiler ("Int", "42");
		}

		[Test]
		public void WrapPropertyUInt ()
		{
			WrapSinglePropertyWithCompiler ("UInt", "42");
		}

		[Test]
		public void WrapPropertyBool ()
		{
			WrapSinglePropertyWithCompiler ("Bool", "true");
		}

		[Test]
		public void WrapPropertyFloat ()
		{
			WrapSinglePropertyWithCompiler ("Float", "42.5");
		}

		[Test]
		public void WrapPropertyDouble ()
		{
			WrapSinglePropertyWithCompiler ("Double", "42.5");
		}

		[Test]
		public void WrapPropertyString ()
		{
			WrapSinglePropertyWithCompiler ("String", "\"nothing\"");
		}

		[Test]
		public void WrapPropertyOpen ()
		{
			WrapPropertyWithDifferentVisibleSet ("open");
		}

		[Test]
		public void WrapPropertyInternal ()
		{
			WrapPropertyWithDifferentVisibleSet ("internal");
		}

		[Test]
		public void WrapPropertyPrivate ()
		{
			WrapPropertyWithDifferentVisibleSet ("private");
		}
	}

	[TestFixture]
	[Parallelizable (ParallelScope.All)]
	[RunWithLeaks]
	public class PropertyWrapTests {
		[Test]
		public void IgnoresPrivateClassProperty ()
		{
			var swiftCode = @"public class Pluralize {
				public init() { }
					class var hidden: Int { return 3; }
					public func nothing() { }
				}
";

			var pluralDecl = CSVariableDeclaration.VarLine ((CSSimpleType)"Pluralize", "plural", CSFunctionCall.Ctor ("Pluralize"));
			var noop = CSFunctionCall.FunctionLine ("plural.Nothing");

			var output = CSFunctionCall.ConsoleWriteLine (CSConstant.Val ("Success"));

			var callingCode = CSCodeBlock.Create (pluralDecl, noop, output);

			TestRunning.TestAndExecute (swiftCode, callingCode, "Success\n");
		}

		[Test]
		public void PropertyWithPrivateSet ()
		{
			string swiftCode = $@"public class Node {{
    public init() {{ }}
    public init(parent: Node) {{ Parent = parent }}

    open private(set) weak var Parent: Node?
}}";

			var parentDecl = CSVariableDeclaration.VarLine ((CSSimpleType)"Node", "parent", CSFunctionCall.Ctor ("Node"));
			var childDecl = CSVariableDeclaration.VarLine ((CSSimpleType)"Node", "child", CSFunctionCall.Ctor ("Node", (CSIdentifier)"parent"));

			var callingCode = CSCodeBlock.Create (parentDecl, childDecl);

			TestRunning.TestAndExecute (swiftCode, callingCode, "");
		}

		[Test]
		public void ComputedPropertyTestInt ()
		{
			string swiftCode = @"public var X: Int = 0

public var DoubleX: Int {
    get {
        return X * 2
    }
    set {
        X = newValue / 2
    }
}
";
			var setterCall = CSAssignment.Assign (new CSIdentifier ("TopLevelEntities").Dot (new CSIdentifier ("DoubleX")), CSConstant.Val (8));
			var printer = CSFunctionCall.ConsoleWriteLine (new CSIdentifier ("TopLevelEntities").Dot (new CSIdentifier ("X")));
			var callingCode = CSCodeBlock.Create (setterCall, printer);

			TestRunning.TestAndExecute (swiftCode, callingCode, "4\n");
		}
		[Test]
		public void ComputedPropertyTestString ()
		{
			string swiftCode = @"private var _backingField: String = """"

public var DoubleStr: String {
    get {
        return _backingField
    }
    set {
        _backingField = newValue + newValue
    }
}
";
			var setterCall = CSAssignment.Assign (new CSIdentifier ("TopLevelEntities").Dot (new CSIdentifier ("DoubleStr")), new CSFunctionCall ("SwiftString.FromString", false, CSConstant.Val ("nothing")));
			var printer = CSFunctionCall.ConsoleWriteLine (new CSIdentifier ("TopLevelEntities").Dot (new CSIdentifier ("DoubleStr")));
			var callingCode = CSCodeBlock.Create (setterCall, printer);

			TestRunning.TestAndExecute (swiftCode, callingCode, "nothingnothing\n");
		}

		[Test]
		[Ignore ("Ignore due to https://bugs.swift.org/browse/SR-13790")]
		public void ClosureTLProp ()
		{
			var swiftCode = @"
import Foundation
public typealias EasingFunction = (CGFloat)->CGFloat

public let EasingFunctionLinear: EasingFunction = { t in
    return t
}
";

			var easingID = new CSIdentifier ("ease");
			var easingDecl = CSVariableDeclaration.VarLine (CSSimpleType.Var, easingID, new CSIdentifier ("TopLevelEntities.EasingFunctionLinear"));
			var valExpr = new CSFunctionCall (easingID.Name, false, CSConstant.ValNFloat (43.2));
			var printer = CSFunctionCall.ConsoleWriteLine (valExpr);
			var callingCode = CSCodeBlock.Create (easingDecl, printer);

			TestRunning.TestAndExecute (swiftCode, callingCode, "43.2\n", platform: PlatformName.macOS);
		}
	}
}

