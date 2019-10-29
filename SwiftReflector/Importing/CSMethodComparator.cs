// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Dynamo.CSLang;
using SwiftReflector.SwiftXmlReflection;
using SwiftReflector.TypeMapping;

namespace SwiftReflector.Importing {
	public static class CSMethodComparator {

		public static bool IsEqualTo(this CSMethod one, CSMethod other)
		{
			if (other == null)
				return false;
			if (one.Name.Name != other.Name.Name)
				return false;

			if (one.Type == null && other.Type != null)
				return false;

			if (one.Type != null && !one.Type.IsEqualTo (other.Type))
				return false;
				
			if (one.Parameters.Count != other.Parameters.Count)
				return false;

			for (int i = 0; i < one.Parameters.Count; i++) {
				var oneParm = one.Parameters [i];
				var otherParm = other.Parameters [i];
				if (!oneParm.CSType.IsEqualTo (otherParm.CSType))
					return false;
				if (oneParm.ParameterKind != otherParm.ParameterKind)
					return false;
			}
			return true;
		}


		static bool IsEqualTo (this CSType one, CSType other)
		{
			if (other == null)
				return false;

			if (one is CSSimpleType simpleOne && other is CSSimpleType simpleOther) {
				return simpleOne.IsEqualTo (simpleOther);
			} else if (one is CSGenericReferenceType genrefOne && other is CSGenericReferenceType genrefOther) {
				return genrefOne.IsEqualTo (genrefOther);
			}
			return false;
		}

		static bool IsEqualTo (this CSSimpleType one, CSSimpleType other)
		{
			if (other == null)
				return false;
			// in theory, this should work. The Name property in a CSSimpleType is
			// a serialization of the type as a string. Two equal types should serialize
			// to the same string.
			return one.Name == other.Name;
		}

		static bool IsEqualTo (this CSGenericReferenceType one, CSGenericReferenceType other)
		{
			if (other == null)
				return false;
			return one.Depth == other.Depth && one.Index == other.Index;
		}

		public static bool HasImportedOverride (this ClassDeclaration classDeclaration, CSMethod oneMethod, TypeMapper typeMapper)
		{
			if (!classDeclaration.IsObjC)
				return false;
			foreach (var otherMethod in classDeclaration.CSharpMethods) {
				if (oneMethod.IsEqualTo (otherMethod))
					return true;
			}

			foreach (var inherit in classDeclaration.Inheritance) {
				if (inherit.InheritanceKind == InheritanceKind.Class) {
					var superEntity = typeMapper.GetEntityForTypeSpec (inherit.InheritedTypeSpec);
					if (superEntity != null && superEntity.Type is ClassDeclaration superClass && superClass.HasImportedOverride (oneMethod, typeMapper))
						return true;
				} else if (inherit.InheritanceKind == InheritanceKind.Protocol) {
					var superEntity = typeMapper.GetEntityForTypeSpec (inherit.InheritedTypeSpec);
					if (superEntity != null && superEntity.Type is ProtocolDeclaration superProto && superProto.HasImportedOverride (oneMethod, typeMapper))
						return true;
				}
			}

			return false;
		}
	}
}
