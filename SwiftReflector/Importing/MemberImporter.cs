// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using Dynamo.CSLang;
using Mono.Cecil;
using ObjCRuntime;
using SwiftReflector.Exceptions;
using SwiftReflector.SwiftXmlReflection;
using SwiftReflector.TypeMapping;

namespace SwiftReflector.Importing {
	public class MemberImporter {
		const string kExportAttribute = "Foundation.ExportAttribute";
		TypeDatabase [] typeDatabases;
		ErrorHandling errors;

		public MemberImporter (ErrorHandling errors, params TypeDatabase [] typeDatabases)
		{
			this.typeDatabases = Ex.ThrowOnNull (typeDatabases, nameof (typeDatabases));
			this.errors = Ex.ThrowOnNull (errors, nameof (errors));
		}

		public void Import (TypeDefinition typeDefinition, ClassDeclaration typeDeclaration)
		{
			bool isProtocol = typeDeclaration is ProtocolDeclaration;

			if (typeDefinition.HasMethods)
				ImportMethods (typeDefinition, typeDeclaration, isProtocol);
			if (typeDefinition.HasProperties)
				ImportProperties (typeDefinition, typeDeclaration, isProtocol);
		}

		void ImportMethods (TypeDefinition typeDefinition, ClassDeclaration classDeclaration, bool isProtocol)
		{
			foreach (var method in typeDefinition.Methods) {
				if (IsCandidateMethod (method) && !IsProperty (typeDefinition, method)) {
					try {
						var csMethod = ImportMethod (method, classDeclaration, isProtocol, false);
						classDeclaration.CSharpMethods.Add (csMethod);
					} catch (RuntimeException err) {
						err = new RuntimeException (err.Code, false, err, err.Message);
						errors.Add (err);
					}
				}
			}
		}

		static bool IsProperty (TypeDefinition typeDefinition, MethodDefinition method)
		{
			if (!typeDefinition.HasProperties)
				return false;
			foreach (var property in typeDefinition.Properties)
				if (property.GetMethod == method || property.SetMethod == method)
					return true;
			return false;
		}

		bool IsCandidateMethod (MethodDefinition method)
		{
			if (!method.IsPublic || method.IsFinal || method.HasGenericParameters || ContainsGenericParameters (method))
				return false;

			return HasExportAttribute (method);
		}

		CSMethod ImportMethod (MethodDefinition method, TypeDeclaration typeDeclaration, bool isProtocol, bool isProperty)
		{
			var returnType = MapType (method.ReturnType);
			var parameters = method.HasParameters ? MapParameters (method.Parameters) : new CSParameterList ();
			var visibility = ToVisibility (method);
			var methodKind = ToMethodKind (method);
			var csMethod = new CSMethod (visibility, methodKind, returnType, new CSIdentifier (method.Name), parameters, new CSCodeBlock ());
			return csMethod;
		}

		void ImportProperties (TypeDefinition typeDefinition, ClassDeclaration classDeclaration, bool isProtocol)
		{
			foreach (var property in typeDefinition.Properties) {
				if (IsCandidateProperty (property)) {
					var csProperty = ImportProperty (property, classDeclaration, isProtocol);
					if (csProperty == null)
						continue;
					classDeclaration.CSharpProperties.Add (csProperty);
				}
			}
		}

		bool IsCandidateProperty (PropertyDefinition property)
		{
			var getter = property.GetMethod;
			var isCandidateGetter = getter != null && IsCandidateMethod (getter);
			var setter = property.SetMethod;
			var isCandidateSetter = setter != null && IsCandidateMethod (setter);

			return isCandidateGetter || isCandidateSetter;
		}

		CSProperty ImportProperty (PropertyDefinition property, ClassDeclaration typeDeclaration, bool isProtocol)
		{
			var hasGetter = property.GetMethod != null;
			var hasSetter = property.SetMethod != null;
			if (!hasGetter && !hasGetter)
				return null;
			// if there's a getter, it's the return value's type
			// if there's a setter, it's the 0th argument (value)
			var gettervisibility = ToVisibility (property.GetMethod ?? property.SetMethod);
			var settervisibility = ToVisibility (property.SetMethod ?? property.GetMethod);
			var propType = property.GetMethod != null ? MapType (property.GetMethod.ReturnType) :
					       MapType (property.SetMethod.Parameters [0].ParameterType);
			return new CSProperty (propType, ToMethodKind (property.GetMethod ?? property.SetMethod), new CSIdentifier (property.Name),
			                       gettervisibility, hasGetter ? new CSCodeBlock () : null,
			                       settervisibility, hasSetter ? new CSCodeBlock () : null);
		}

		static bool ContainsGenericParameters (MethodDefinition method)
		{
			if (!method.HasParameters)
				return false;
			foreach (var parameter in method.Parameters) {
				if (parameter.ParameterType.HasGenericParameters || parameter.ParameterType.IsGenericInstance)
					return true;
			}
			return false;
		}

		static bool HasExportAttribute (MethodDefinition method)
		{
			if (!method.HasCustomAttributes)
				return false;
			return method.CustomAttributes.Any (attribute => attribute.AttributeType.FullName == kExportAttribute);
		}

		static CSVisibility ToVisibility (MethodDefinition method)
		{
			if (method.IsPublic)
				return CSVisibility.Public;
			if (method.IsPrivate)
				return CSVisibility.Private;
			if (method.IsInternalCall)
				return CSVisibility.Internal;
			if (method.IsFamily)
				return CSVisibility.Protected;
			throw ErrorHelper.CreateError (ReflectorError.kImportingBase + 0, $"Unable to determine visibility of method {method.FullName}");
		}

		static CSParameterKind ToParameterKind (ParameterDefinition parameterDefinition)
		{
			if (parameterDefinition.IsOut)
				return CSParameterKind.Ref;
			return CSParameterKind.None;
		}

		static CSMethodKind ToMethodKind (MethodDefinition method)
		{
			if (method.IsStatic)
				return CSMethodKind.Static;
			if (method.HasOverrides)
				return CSMethodKind.Override;
			if (method.IsNative)
				return CSMethodKind.Extern;
			if (method.IsAbstract)
				return CSMethodKind.Abstract;
			return CSMethodKind.None;
		}

		CSParameterList MapParameters (ICollection <ParameterDefinition> methodParameters)
		{
			CSParameterList parameters = new CSParameterList ();
			foreach (var methodParameter in methodParameters) {
				var parameterType = MapType (methodParameter.ParameterType);
				var parameterKind = ToParameterKind (methodParameter);
				var parameter = new CSParameter (parameterType, methodParameter.Name, parameterKind);
				parameters.Add (parameter);
			}

			return parameters;
		}

		CSType MapType (TypeReference typeReference)
		{
			CSType commonType = null;
			if (TryGetCommonName (typeReference.FullName, out commonType))
				return commonType;
			if (typeReference.HasGenericParameters) {
				var generics = typeReference.GenericParameters.Select (gen => MapType (gen)).ToArray ();
				var genericType = new CSSimpleType (typeReference.Name, false, generics);
				return genericType;
			} else {
				var theType = new CSSimpleType (typeReference.Name);
				return theType;
			}
		}



		static Dictionary<string, CSType> nativeTypeNameMap = new Dictionary<string, CSType> () {
			{ "System.String", CSSimpleType.String },
			{ "System.Boolean", CSSimpleType.Bool },
			{ "System.nint", new CSSimpleType ("nint") },
			{ "System.nuint", new CSSimpleType ("nuint") },
			{ "System.Int32", CSSimpleType.Int },
			{ "System.UInt32", CSSimpleType.UInt },
			{ "System.Int64", CSSimpleType.Long },
			{ "System.UInt64", CSSimpleType.ULong },
			{ "System.Int16", CSSimpleType.Short },
			{ "System.UInt16", CSSimpleType.UShort },
			{ "System.Byte", CSSimpleType.Byte },
			{ "System.SByte", CSSimpleType.SByte },
			{ "System.Char", CSSimpleType.Char },
			{ "System.Single", CSSimpleType.Float },
			{ "System.Double", CSSimpleType.Double },
			{ "System.Void", CSSimpleType.Void },
			{ "System.nfloat", new CSSimpleType ("nfloat") }
		};

		static bool TryGetCommonName (string fullName, out CSType result)
		{
			return nativeTypeNameMap.TryGetValue (fullName, out result);
		}
	}
}
