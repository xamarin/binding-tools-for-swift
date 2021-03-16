using System;
using System.Linq;
using System.Xml;

namespace DylibBinder {
	public class XmlGenerator {
		public XmlGenerator (DBTopLevel dBTopLevel, string outputPath)
		{
			XmlWriter writer = CreateWriter (outputPath);
			WriteIntro (writer, dBTopLevel);
			WriteTypeDeclarations (writer, dBTopLevel);
			CloseWriter (writer);
		}

		XmlWriter CreateWriter (string outputPath)
		{
			XmlWriterSettings settings = new XmlWriterSettings ();
			settings.Indent = true;
			settings.IndentChars = "   ";
			return XmlWriter.Create (outputPath, settings);
		}

		void WriteIntro (XmlWriter writer, DBTopLevel dBTopLevel) {
			writer.WriteStartDocument ();
			writer.WriteStartElement ("xamreflect");
			writer.WriteAttributeString ("version", dBTopLevel.XmlVersion);

			writer.WriteStartElement ("modulelist");

			writer.WriteStartElement ("module");
			writer.WriteAttributeString ("name", dBTopLevel.ModuleName);
			writer.WriteAttributeString ("swiftVersion", dBTopLevel.SwiftVersion);
		}


		void WriteTypeDeclarations (XmlWriter writer, DBTopLevel dBTopLevel)
		{
			foreach (var typeDeclaration in dBTopLevel.DBTypeDeclarations.TypeDeclarations) {

				writer.WriteStartElement ("typedeclaration");
				WriteAttributeStrings (writer, ("kind", typeDeclaration.Kind),
				                      ("name", typeDeclaration.Name), ("accessibility", typeDeclaration.Accessibility),
				                      ("isDeprecated", typeDeclaration.IsDeprecated), ("isUnavailable", typeDeclaration.IsUnavailable),
				                      ("isObjC", typeDeclaration.IsObjC), ("isFinal", typeDeclaration.IsFinal));

				if (typeDeclaration.Kind == "protocol")
					WriteProtocolTypeDeclaration (writer, typeDeclaration);
				else
					WriteTypeDeclaration (writer, typeDeclaration);
				writer.WriteEndElement ();
			}
		}

		void WriteTypeDeclaration (XmlWriter writer, DBTypeDeclaration typeDeclaration)
		{
			WriteMembers (writer, typeDeclaration);
			WriteInnerTypes (writer, typeDeclaration);
			WriteGenericParameters (writer, typeDeclaration);
			WriteAssocTypes (writer, typeDeclaration);
			WriteElements (writer, typeDeclaration);
		}

		void WriteProtocolTypeDeclaration (XmlWriter writer, DBTypeDeclaration typeDeclaration)
		{
			WriteProtocolMembers (writer, typeDeclaration);
		}
		void WriteMembers (XmlWriter writer, DBTypeDeclaration typeDeclaration)
		{
			writer.WriteStartElement ("members");
			WriteFuncs (writer, typeDeclaration);
			WriteProperties (writer, typeDeclaration);
			writer.WriteEndElement ();
		}

		void WriteProtocolMembers (XmlWriter writer, DBTypeDeclaration typeDeclaration)
		{
			writer.WriteStartElement ("members");
			WriteFuncs (writer, typeDeclaration);
			writer.WriteEndElement ();
		}

		void WriteInnerTypes (XmlWriter writer, DBTypeDeclaration typeDeclaration)
		{
			foreach (var innerType in typeDeclaration.InnerTypes.InnerTypes) {
				writer.WriteStartElement ($"inner{innerType.Kind}");
				WriteTypeDeclaration (writer, innerType);
				writer.WriteEndElement ();
			}
		}

		void WriteGenericParameters (XmlWriter writer, DBTypeDeclaration typeDeclaration)
		{
			if (typeDeclaration.GenericParameters.GenericParameters.Count == 0)
				return;

			writer.WriteStartElement ("genericparameters");
			foreach (var gp in typeDeclaration.GenericParameters.GenericParameters) {
				WriteGenericParameter (writer, gp);
			}
			writer.WriteEndElement ();
		}

		void WriteGenericParameters (XmlWriter writer, DBGenericParameters genericParameters)
		{
			var validGenericParameters = from g in genericParameters.GenericParameters
										 where g.Depth > 0
										 select g;

			if (validGenericParameters.Count () == 0)
				return;

			writer.WriteStartElement ("genericparameters");
			foreach (var gp in validGenericParameters) {
				WriteGenericParameter (writer, gp);
			}
			writer.WriteEndElement ();
		}

		void WriteGenericParameter (XmlWriter writer, DBGenericParameter parameter)
		{
			writer.WriteStartElement ("param");
			writer.WriteAttributeString ("name", parameter.Name);
			writer.WriteEndElement ();
		}

		void WriteAssocTypes (XmlWriter writer, DBTypeDeclaration typeDeclaration)
		{
			if (typeDeclaration.AssociatedTypes.AssociatedTypes.Count == 0)
				return;

			writer.WriteStartElement ("associatedtypes");
			foreach (var associatedType in typeDeclaration.AssociatedTypes.AssociatedTypes) {
				WriteAssocType (writer, associatedType);
			}
			writer.WriteEndElement ();
		}

		void WriteAssocType (XmlWriter writer, DBAssociatedType associatedType)
		{
			writer.WriteStartElement ("associatedtype");
			writer.WriteAttributeString ("name", associatedType.Name);
			writer.WriteEndElement ();
		}

		void WriteElements (XmlWriter writer, DBTypeDeclaration typeDeclaration)
		{
			if (typeDeclaration.Kind != "enum")
				return;

			writer.WriteStartElement ("elements");
			foreach (var element in typeDeclaration.Elements.Elements) {
				WriteElement (writer, element);
			}
			writer.WriteEndElement ();
		}

		void WriteElement (XmlWriter writer, DBElement element)
		{
			writer.WriteStartElement ("element");
			WriteAttributeStrings (writer, ("name", element.Name), ("intValue", element.IntValue));
			writer.WriteEndElement ();
		}

		void WriteFuncs (XmlWriter writer, DBTypeDeclaration typeDeclaration)
		{
			foreach (var func in typeDeclaration.Funcs.Funcs) {
				WriteFunc (writer, func);
			}
		}

		void WriteFunc (XmlWriter writer, DBFunc func)
		{
			writer.WriteStartElement ("func");
			WriteAttributeStrings (writer, ("name", func.Name),
			                      ("returnType", func.ReturnType));
			if (!string.IsNullOrEmpty(func.PropertyType))
				writer.WriteAttributeString ("propertyType", func.PropertyType);

			WriteAttributeStrings (writer, ("isProperty", func.IsProperty),
			                      ("hasThrows", func.HasThrows), ("isStatic", func.IsStatic),
			                      ("isMutating", func.IsMutating), ("operatorKind", func.OperatorKind),
			                      ("accessibility", func.Accessibility), ("isDeprecated", func.IsDeprecated),
			                      ("isUnavailable", func.IsUnavailable), ("isFinal", func.IsFinal),
			                      ("isOptional", func.IsOptional), ("isRequired", func.IsRequired),
			                      ("isConvenienceInit", func.IsConvenienceInit), ("objcSelector", func.ObjcSelector),
			                      ("isPossiblyIncomplete", func.IsPossiblyIncomplete));

			WriteParameterLists (writer, func.ParameterLists);
			WriteGenericParameters (writer, func.GenericParameters);
			writer.WriteEndElement ();
		}

		void WriteParameterLists (XmlWriter writer, DBParameterLists parameterLists)
		{
			writer.WriteStartElement ("parameterlists");
			foreach (var parameterList in parameterLists.ParameterLists) {
				WriteParameterList (writer, parameterList);
			}
			writer.WriteEndElement ();
		}

		void WriteParameterList (XmlWriter writer, DBParameterList parameterList)
		{
			writer.WriteStartElement ("parameterlist");
			WriteAttributeStrings (writer, ("index", parameterList.Index));
			foreach (var parameter in parameterList.Parameters) {
				WriteParameter (writer, parameter);
			}
			writer.WriteEndElement ();
		}

		void WriteParameter (XmlWriter writer, DBParameter parameter)
		{
			if (parameter.IsEmptyParameter)
				return;

			writer.WriteStartElement ("parameter");
			WriteAttributeStrings (writer, ("index", parameter.Index),
			                      ("type", parameter.Type), ("publicName", parameter.PublicName),
			                      ("privateName", parameter.PrivateName), ("isVariadic", parameter.IsVariadic));
			writer.WriteEndElement ();
		}

		void WriteProperties (XmlWriter writer, DBTypeDeclaration typeDeclaration)
		{
			foreach (var property in typeDeclaration.Properties.Properties) {
				WriteProp (writer, property);
				WriteFunc (writer, property.Getter);
				if (property.Setter != null)
					WriteFunc (writer, property.Setter);
			}
		}

		void WriteProp (XmlWriter writer, DBProperty propery)
		{
			writer.WriteStartElement ("property");
			WriteAttributeStrings (writer, ("name", propery.Name),
			                      ("type", propery.Type), ("isStatic", propery.IsStatic),
			                      ("accessibility", propery.Accessibility), ("isDeprecated", propery.IsDeprecated),
			                      ("isUnavailable", propery.IsUnavailable), ("isOptional", propery.IsOptional),
			                      ("storage", propery.Storage), ("isPossiblyIncomplete", propery.IsPossiblyIncomplete));
			writer.WriteEndElement ();
		}

		void CloseWriter (XmlWriter writer) {
			writer.WriteEndDocument ();
			writer.Close ();
			writer.Dispose ();
		}

		void WriteAttributeStrings (XmlWriter writer, params (string name, object value) [] attributes)
		{
			foreach (var attribute in attributes) {
				if (attribute.value is string s)
					writer.WriteAttributeString (attribute.name, s);
				else {
					writer.WriteStartAttribute (attribute.name);
					writer.WriteValue (attribute.value);
					writer.WriteEndAttribute ();
				}
			}
		}
	}
}
