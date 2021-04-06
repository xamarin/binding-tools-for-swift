using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using SwiftReflector;
using SwiftReflector.SwiftXmlReflection;

namespace DylibBinder {
	sealed internal class XmlGenerator : IDisposable {
		public XmlGenerator (DBTopLevel dBTopLevel, string outputPath)
		{
			writer = CreateWriter (outputPath);
			WriteIntro (dBTopLevel);
			WriteModules (dBTopLevel);
			CloseWriter ();
		}

		XmlWriter writer;

		XmlWriter CreateWriter (string outputPath)
		{
			XmlWriterSettings settings = new XmlWriterSettings ();
			settings.Indent = true;
			settings.IndentChars = "   ";
			return XmlWriter.Create (outputPath, settings);
		}

		void WriteIntro (DBTopLevel dBTopLevel) {
			writer.WriteStartDocument ();
			writer.WriteStartElement ("xamreflect");
			writer.WriteAttributeString ("version", dBTopLevel.XmlVersion);

			writer.WriteStartElement ("modulelist");
		}

		void WriteModules (DBTopLevel dBTopLevel)
		{
			foreach (var module in dBTopLevel.DBTypeDeclarations.TypeDeclarations.Keys) {
				writer.WriteStartElement ("module");
				writer.WriteAttributeString ("name", module);
				writer.WriteAttributeString ("swiftVersion", dBTopLevel.SwiftVersion);
				WriteTypeDeclarations (dBTopLevel.DBTypeDeclarations.TypeDeclarations [module]);
			}
		}


		void WriteTypeDeclarations (List <DBTypeDeclaration> typeDeclarations)
		{
			foreach (var typeDeclaration in typeDeclarations) {

				if (InnerX.IsInnerType (typeDeclaration))
					continue;

				WriteTypeDeclarationOpening (typeDeclaration);

				if (typeDeclaration.Kind == TypeKind.Protocol)
					WriteProtocolTypeDeclaration (typeDeclaration);
				else
					WriteTypeDeclaration (typeDeclaration);
				writer.WriteEndElement ();
			}
		}

		void WriteTypeDeclarationOpening (DBTypeDeclaration typeDeclaration)
		{
			writer.WriteStartElement ("typedeclaration");
			WriteAttributeStrings (("kind", typeDeclaration.Kind), ("name", typeDeclaration.Name),
								  ("accessibility", typeDeclaration.Accessibility), ("isDeprecated", typeDeclaration.IsDeprecated),
								  ("isUnavailable", typeDeclaration.IsUnavailable), ("isObjC", typeDeclaration.IsObjC),
								  ("isFinal", typeDeclaration.IsFinal));
		}

		void WriteTypeDeclaration (DBTypeDeclaration typeDeclaration)
		{
			WriteMembers (typeDeclaration);
			WriteInnerTypes (typeDeclaration);
			WriteGenericParameters (typeDeclaration);
			WriteAssocTypes (typeDeclaration);
			WriteElements (typeDeclaration);
		}

		void WriteProtocolTypeDeclaration (DBTypeDeclaration typeDeclaration)
		{
			WriteProtocolMembers (typeDeclaration);
		}

		void WriteMembers (DBTypeDeclaration typeDeclaration)
		{
			writer.WriteStartElement ("members");
			WriteFuncs (typeDeclaration);
			WriteProperties (typeDeclaration);
			writer.WriteEndElement ();
		}

		void WriteProtocolMembers (DBTypeDeclaration typeDeclaration)
		{
			writer.WriteStartElement ("members");
			WriteFuncs (typeDeclaration);
			writer.WriteEndElement ();
		}

		void WriteInnerTypes (DBTypeDeclaration typeDeclaration)
		{
			foreach (var innerType in typeDeclaration.InnerTypes.InnerTypes) {
				writer.WriteStartElement ($"inner{innerType.Kind}");
				WriteTypeDeclarationOpening (innerType);
				WriteTypeDeclaration (innerType);
				foreach (var innerInnerType in innerType.InnerTypes.InnerTypes) {
					WriteInnerTypes (innerInnerType);
				}
				writer.WriteEndElement ();
				writer.WriteEndElement ();
			}
		}

		void WriteGenericParameters (DBTypeDeclaration typeDeclaration)
		{
			if (typeDeclaration.GenericParameters.GenericParameters.Count == 0)
				return;

			writer.WriteStartElement ("genericparameters");
			foreach (var gp in typeDeclaration.GenericParameters.GenericParameters) {
				WriteGenericParameter (gp);
			}
			writer.WriteEndElement ();
		}

		void WriteGenericParameters (DBGenericParameters genericParameters)
		{
			var validGenericParameters = from g in genericParameters.GenericParameters
										 where g.Depth > 0
										 select g;

			if (validGenericParameters.Count () == 0)
				return;

			writer.WriteStartElement ("genericparameters");
			foreach (var gp in validGenericParameters) {
				WriteGenericParameter (gp);
			}
			writer.WriteEndElement ();
		}

		void WriteGenericParameter (DBGenericParameter parameter)
		{
			writer.WriteStartElement ("param");
			writer.WriteAttributeString ("name", parameter.Name);
			writer.WriteEndElement ();
		}

		void WriteAssocTypes (DBTypeDeclaration typeDeclaration)
		{
			if (typeDeclaration.AssociatedTypes.AssociatedTypes.Count == 0)
				return;

			writer.WriteStartElement ("associatedtypes");
			foreach (var associatedType in typeDeclaration.AssociatedTypes.AssociatedTypes) {
				WriteAssocType (associatedType);
			}
			writer.WriteEndElement ();
		}

		void WriteAssocType (DBAssociatedType associatedType)
		{
			writer.WriteStartElement ("associatedtype");
			writer.WriteAttributeString ("name", associatedType.Name);
			writer.WriteEndElement ();
		}

		void WriteElements (DBTypeDeclaration typeDeclaration)
		{
			if (typeDeclaration.Kind != TypeKind.Enum)
				return;

			writer.WriteStartElement ("elements");
			foreach (var element in typeDeclaration.Elements.Elements) {
				WriteElement (element);
			}
			writer.WriteEndElement ();
		}

		void WriteElement (DBElement element)
		{
			writer.WriteStartElement ("element");
			WriteAttributeStrings (("name", element.Name), ("intValue", element.IntValue));
			writer.WriteEndElement ();
		}

		void WriteFuncs (DBTypeDeclaration typeDeclaration)
		{
			foreach (var func in typeDeclaration.Funcs.Funcs) {
				WriteFunc (func);
			}
		}

		void WriteFunc (DBFunc func)
		{
			writer.WriteStartElement ("func");
			WriteAttributeStrings (("name", func.Name), ("returnType", func.ReturnType));

			if (!string.IsNullOrEmpty(func.PropertyType))
				writer.WriteAttributeString ("propertyType", func.PropertyType);

			WriteAttributeStrings (("isProperty", func.IsProperty), ("hasThrows", func.HasThrows),
			                      ("isStatic", func.IsStatic), ("isMutating", func.IsMutating),
			                      ("operatorKind", func.OperatorKind), ("accessibility", func.Accessibility),
			                      ("isDeprecated", func.IsDeprecated), ("isUnavailable", func.IsUnavailable),
			                      ("isFinal", func.IsFinal), ("isOptional", func.IsOptional),
			                      ("isRequired", func.IsRequired), ("isConvenienceInit", func.IsConvenienceInit),
			                      ("objcSelector", func.ObjcSelector), ("isPossiblyIncomplete", func.IsPossiblyIncomplete));

			WriteParameterLists (func.ParameterLists);
			WriteGenericParameters (func.GenericParameters);
			writer.WriteEndElement ();
		}

		void WriteParameterLists (DBParameterLists parameterLists)
		{
			writer.WriteStartElement ("parameterlists");
			foreach (var parameterList in parameterLists.ParameterLists) {
				WriteParameterList (parameterList);
			}
			writer.WriteEndElement ();
		}

		void WriteParameterList (DBParameterList parameterList)
		{
			writer.WriteStartElement ("parameterlist");
			WriteAttributeStrings (("index", parameterList.Index));
			foreach (var parameter in parameterList.Parameters) {
				WriteParameter (parameter);
			}
			writer.WriteEndElement ();
		}

		void WriteParameter (DBParameter parameter)
		{
			if (parameter.IsEmptyParameter)
				return;

			writer.WriteStartElement ("parameter");
			WriteAttributeStrings (("index", parameter.Index), ("type", parameter.Type),
			                      ("publicName", parameter.PublicName), ("privateName", parameter.PrivateName),
			                      ("isVariadic", parameter.IsVariadic));
			writer.WriteEndElement ();
		}

		void WriteProperties (DBTypeDeclaration typeDeclaration)
		{
			foreach (var property in typeDeclaration.Properties.Properties) {
				WriteProp (property);
				WriteFunc (property.Getter);
				if (property.Setter != null)
					WriteFunc (property.Setter);
			}
		}

		void WriteProp (DBProperty property)
		{
			writer.WriteStartElement ("property");
			WriteAttributeStrings (("name", property.Name), ("type", property.Type),
			                      ("isStatic", property.IsStatic), ("accessibility", property.Accessibility),
			                      ("isDeprecated", property.IsDeprecated), ("isUnavailable", property.IsUnavailable),
			                      ("isOptional", property.IsOptional), ("storage", property.Storage),
			                      ("isPossiblyIncomplete", property.IsPossiblyIncomplete));
			writer.WriteEndElement ();
		}

		void CloseWriter () {
			writer.WriteEndDocument ();
			writer.Close ();
		}

		void WriteAttributeStrings (params (string name, object value) [] attributes)
		{
			foreach (var attribute in attributes) {
				if (attribute.value is string s)
					writer.WriteAttributeString (attribute.name, s);
				else if (attribute.value is TypeKind kind)
					writer.WriteAttributeString (attribute.name, kind.ToString ().ToLower ());
				else if (attribute.value is Enum e)
					writer.WriteAttributeString (attribute.name, e.ToString ());
				else {
					writer.WriteStartAttribute (attribute.name);
					writer.WriteValue (attribute.value);
					writer.WriteEndAttribute ();
				}
			}
		}

		private bool _disposed = false;

		public void Dispose ()
		{
			writer.Dispose ();

			if (_disposed)
				return;

			_disposed = true;
		}
	}
}
