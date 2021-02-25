// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Xml.Linq;
using Dynamo;
using ObjCRuntime;
using SwiftReflector.ExceptionTools;

namespace SwiftReflector.SwiftXmlReflection {
	public class TypeAliasDeclaration {
		public TypeAliasDeclaration ()
		{
		}

		public Accessibility Access { get; private set; }

		string typeName;
		public string TypeName {
			get { return typeName; }
			set {
				typeName = Exceptions.ThrowOnNull (value, nameof (value));
				if (typeName.IndexOf (':') >= 0)
					throw ErrorHelper.CreateError (ReflectorError.kReflectionErrorBase + 12, $"typealias {value} has a generic constraint which is not supported");
				try {
					typeSpec = TypeSpecParser.Parse (typeName);
				} catch (RuntimeException ex) {
					throw ErrorHelper.CreateError (ReflectorError.kReflectionErrorBase + 11, $"Unable to parse typealias name '{value}': {ex.Message}");
				}
			}
		}

		TypeSpec typeSpec;
		public TypeSpec TypeSpec {
			get { return typeSpec; }
			set {
				Exceptions.ThrowOnNull (value, nameof (value));
				typeSpec = value;
				typeName = value.ToString ();
			}
		}

		string targetTypeName;
		public string TargetTypeName {
			get { return targetTypeName; }
			set {
				targetTypeName = Exceptions.ThrowOnNull (value, nameof (value));
				try {
					targetTypeSpec = TypeSpecParser.Parse (targetTypeName);
				} catch (RuntimeException ex) {
					throw ErrorHelper.CreateError (ReflectorError.kReflectionErrorBase + 11, $"Unable to parse typealias target name '{value}': {ex.Message}");
				}
			}
		}

		TypeSpec targetTypeSpec;
		public TypeSpec TargetTypeSpec {
			get { return targetTypeSpec; }
			set {
				Exceptions.ThrowOnNull (value, nameof (value));
				targetTypeSpec = value;
				targetTypeName = value.ToString ();
			}
		}

		public static TypeAliasDeclaration FromXElement (XElement element)
		{
			return new TypeAliasDeclaration () {
				Access = TypeDeclaration.AccessibilityFromString ((string)element.Attribute ("accessibility")),
				TypeName = element.Attribute ("name").Value,
				TargetTypeName = element.Attribute ("type").Value
			};
		}
	}
}
