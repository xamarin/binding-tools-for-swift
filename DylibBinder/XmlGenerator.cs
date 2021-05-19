using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using SwiftReflector.SwiftXmlReflection;
using SwiftRuntimeLibrary;

namespace DylibBinder {
	sealed internal class XmlGenerator : IDisposable {
		XmlWriter writer;
		private bool _disposed = false;
		readonly string OutputPath;

		public static void WriteDBToFile (DBTopLevel dBTopLevel, string outputPath)
		{
			var generator = new XmlGenerator (Exceptions.ThrowOnNull (outputPath, nameof (outputPath)));
			generator.WriteDBToFile (Exceptions.ThrowOnNull (dBTopLevel, nameof (dBTopLevel)));
		}

		XmlGenerator (string outputPath)
		{
			OutputPath = Exceptions.ThrowOnNull (outputPath, nameof (outputPath));
		}

		void WriteDBToFile (DBTopLevel dBTopLevel)
		{
			Exceptions.ThrowOnNull (dBTopLevel, nameof (dBTopLevel));
			using (writer = CreateWriter (OutputPath)) {
				WriteIntro ();
				WriteModules (dBTopLevel);
				CloseWriter ();
			}
		}

		XmlWriter CreateWriter (string outputPath)
		{
			Exceptions.ThrowOnNull (outputPath, nameof (outputPath));
			XmlWriterSettings settings = new XmlWriterSettings ();
			settings.Indent = true;
			settings.IndentChars = "   ";
			return XmlWriter.Create (outputPath, settings);
		}

		void WriteIntro () {
			writer.WriteStartDocument ();
			writer.WriteStartElement ("xamreflect");
			writer.WriteAttributeString ("version", "1.0");
			writer.WriteStartElement ("modulelist");
		}

		void WriteModules (DBTopLevel dBTopLevel)
		{
			Exceptions.ThrowOnNull (dBTopLevel, nameof (dBTopLevel));
			foreach (var module in dBTopLevel.DBTypeDeclarations.TypeDeclarationCollection.Keys) {
				writer.WriteStartElement ("module");
				writer.WriteAttributeString ("name", module);
				writer.WriteAttributeString ("swiftVersion", dBTopLevel.SwiftVersion);
				WriteTypeDeclarations (dBTopLevel.DBTypeDeclarations.TypeDeclarationCollection [module]);
			}
		}

		void WriteTypeDeclarations (List <DBTypeDeclaration> typeDeclarations)
		{
			Exceptions.ThrowOnNull (typeDeclarations, nameof (typeDeclarations));
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
			Exceptions.ThrowOnNull (typeDeclaration, nameof (typeDeclaration));
			writer.WriteStartElement ("typedeclaration");
			WriteAttributeStrings (("kind", typeDeclaration.Kind), ("name", typeDeclaration.Name),
								  ("accessibility", typeDeclaration.Accessibility), ("isDeprecated", typeDeclaration.IsDeprecated),
								  ("isUnavailable", typeDeclaration.IsUnavailable), ("isObjC", typeDeclaration.IsObjC),
								  ("isFinal", typeDeclaration.IsFinal));
		}

		void WriteTypeDeclaration (DBTypeDeclaration typeDeclaration)
		{
			Exceptions.ThrowOnNull (typeDeclaration, nameof (typeDeclaration));
			WriteMembers (typeDeclaration);
			WriteInnerTypes (typeDeclaration);
			WriteGenericParameters (typeDeclaration);
			WriteAssocTypes (typeDeclaration);
			WriteElements (typeDeclaration);
		}

		void WriteProtocolTypeDeclaration (DBTypeDeclaration typeDeclaration)
		{
			Exceptions.ThrowOnNull (typeDeclaration, nameof (typeDeclaration));
			WriteProtocolMembers (typeDeclaration);
		}

		void WriteMembers (DBTypeDeclaration typeDeclaration)
		{
			Exceptions.ThrowOnNull (typeDeclaration, nameof (typeDeclaration));
			writer.WriteStartElement ("members");
			WriteFuncs (typeDeclaration);
			WriteProperties (typeDeclaration);
			writer.WriteEndElement ();
		}

		void WriteProtocolMembers (DBTypeDeclaration typeDeclaration)
		{
			Exceptions.ThrowOnNull (typeDeclaration, nameof (typeDeclaration));
			writer.WriteStartElement ("members");
			WriteFuncs (typeDeclaration);
			writer.WriteEndElement ();
		}

		void WriteInnerTypes (DBTypeDeclaration typeDeclaration)
		{
			Exceptions.ThrowOnNull (typeDeclaration, nameof (typeDeclaration));
			foreach (var innerType in typeDeclaration.InnerTypes.InnerTypeCollection) {
				writer.WriteStartElement ($"inner{innerType.Kind}");
				WriteTypeDeclarationOpening (innerType);
				WriteTypeDeclaration (innerType);
				foreach (var innerInnerType in innerType.InnerTypes.InnerTypeCollection) {
					WriteInnerTypes (innerInnerType);
				}
				writer.WriteEndElement ();
				writer.WriteEndElement ();
			}
		}

		void WriteGenericParameters (DBTypeDeclaration typeDeclaration)
		{
			Exceptions.ThrowOnNull (typeDeclaration, nameof (typeDeclaration));
			if (typeDeclaration.GenericParameters.GenericParameterCollection.Count == 0)
				return;

			writer.WriteStartElement ("GenericParameterCollection");
			foreach (var gp in typeDeclaration.GenericParameters.GenericParameterCollection) {
				WriteGenericParameter (gp);
			}
			writer.WriteEndElement ();
		}

		void WriteGenericParameters (DBGenericParameters GenericParameterCollection)
		{
			Exceptions.ThrowOnNull (GenericParameterCollection, nameof (GenericParameterCollection));
			var validGenericParameters = from g in GenericParameterCollection.GenericParameterCollection
										 where g.Depth > 0
										 select g;

			if (!validGenericParameters.Any ())
				return;

			writer.WriteStartElement ("GenericParameterCollection");
			foreach (var gp in validGenericParameters) {
				WriteGenericParameter (gp);
			}
			writer.WriteEndElement ();
		}

		void WriteGenericParameter (DBGenericParameter parameter)
		{
			Exceptions.ThrowOnNull (parameter, nameof (parameter));
			writer.WriteStartElement ("param");
			writer.WriteAttributeString ("name", parameter.Name);
			writer.WriteEndElement ();
		}

		void WriteAssocTypes (DBTypeDeclaration typeDeclaration)
		{
			Exceptions.ThrowOnNull (typeDeclaration, nameof (typeDeclaration));
			if (typeDeclaration.AssociatedTypes.AssociatedTypeCollection.Count == 0)
				return;

			writer.WriteStartElement ("AssociatedTypeCollection");
			foreach (var associatedType in typeDeclaration.AssociatedTypes.AssociatedTypeCollection) {
				WriteAssocType (associatedType);
			}
			writer.WriteEndElement ();
		}

		void WriteAssocType (DBAssociatedType associatedType)
		{
			Exceptions.ThrowOnNull (associatedType, nameof (associatedType));
			writer.WriteStartElement ("associatedtype");
			writer.WriteAttributeString ("name", associatedType.Name);
			writer.WriteEndElement ();
		}

		void WriteElements (DBTypeDeclaration typeDeclaration)
		{
			Exceptions.ThrowOnNull (typeDeclaration, nameof (typeDeclaration));
			if (typeDeclaration.Kind != TypeKind.Enum)
				return;

			writer.WriteStartElement ("elements");
			writer.WriteEndElement ();
		}

		void WriteFuncs (DBTypeDeclaration typeDeclaration)
		{
			Exceptions.ThrowOnNull (typeDeclaration, nameof (typeDeclaration));
			foreach (var func in typeDeclaration.Funcs.FuncCollection) {
				WriteFunc (func);
			}
		}

		void WriteFunc (DBFunc func)
		{
			Exceptions.ThrowOnNull (func, nameof (func));
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
			Exceptions.ThrowOnNull (parameterLists, nameof (parameterLists));
			writer.WriteStartElement ("parameterlists");
			foreach (var parameterList in parameterLists.ParameterListCollection) {
				WriteParameterList (parameterList);
			}
			writer.WriteEndElement ();
		}

		void WriteParameterList (DBParameterList parameterList)
		{
			Exceptions.ThrowOnNull (parameterList, nameof (parameterList));
			writer.WriteStartElement ("parameterlist");
			WriteAttributeStrings (("index", parameterList.Index));
			foreach (var parameter in parameterList.ParameterCollection) {
				WriteParameter (parameter);
			}
			writer.WriteEndElement ();
		}

		void WriteParameter (DBParameter parameter)
		{
			Exceptions.ThrowOnNull (parameter, nameof (parameter));
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
			Exceptions.ThrowOnNull (typeDeclaration, nameof (typeDeclaration));
			foreach (var property in typeDeclaration.Properties.PropertyCollection) {
				WriteProp (property);
				WriteFunc (property.Getter);
				if (property.Setter != null)
					WriteFunc (property.Setter);
			}
		}

		void WriteProp (DBProperty property)
		{
			Exceptions.ThrowOnNull (property, nameof (property));
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
			foreach (var (name, value) in attributes) {
				if (value is string s)
					writer.WriteAttributeString (name, s);
				else if (value is TypeKind kind)
					writer.WriteAttributeString (name, kind.ToString ().ToLower ());
				else if (value is Enum e)
					writer.WriteAttributeString (name, e.ToString ());
				else {
					writer.WriteStartAttribute (name);
					writer.WriteValue (value);
					writer.WriteEndAttribute ();
				}
			}
		}

		public void Dispose ()
		{
			writer.Dispose ();

			if (_disposed)
				return;

			_disposed = true;
		}
	}
}
