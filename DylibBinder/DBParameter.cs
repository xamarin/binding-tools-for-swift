using System;
using System.Collections.Generic;
using SwiftReflector;
using SwiftRuntimeLibrary;

namespace DylibBinder {
	internal class DBParameter {
		public string Type { get; set; }
		public string PublicName { get; }
		public string PrivateName { get; }
		public bool IsVariadic { get; }
		public int Index { get; }
		public bool HasInstance { get; }
		public bool IsConstructor { get; }
		public bool IsEmptyParameter { get; }

		public DBParameter (string type, string publicName, string privateName, int index, ParameterOptions parameterOptions)
		{
			Type = type;
			PublicName = publicName;
			PrivateName = privateName;
			Index = index;
			IsVariadic = parameterOptions.HasFlag (ParameterOptions.IsVariadic);
			HasInstance = parameterOptions.HasFlag (ParameterOptions.HasInstance);
			IsConstructor = parameterOptions.HasFlag (ParameterOptions.IsConstructor);
			IsEmptyParameter = parameterOptions.HasFlag (ParameterOptions.IsEmptyParameter);
		}

		public static DBParameter CreateInstanceParameter () => new DBParameter (string.Empty, string.Empty, "self", 0, ParameterOptions.HasInstance);
		public static DBParameter CreateInstanceParameter (int index) => new DBParameter (string.Empty, string.Empty, "self", index, ParameterOptions.HasInstance);
		public static DBParameter CreateConstructorParameter (int index) => new DBParameter (".Type", string.Empty, "self", index, ParameterOptions.IsConstructor);
		public static DBParameter CreateGetterParameter () => new DBParameter (null, null, null, 0, ParameterOptions.IsEmptyParameter);
		public static DBParameter CreateSetterParameter () => new DBParameter (string.Empty, string.Empty, "self", 0, ParameterOptions.HasInstance);
	}


	internal class DBParameterList : IAssociatedTypes {
		public int Index { get; }
		public List<DBParameter> ParameterCollection { get; } = new List<DBParameter> ();
		public DBAssociatedTypes AssociatedTypes { get; } = new DBAssociatedTypes ();

		public DBParameterList (SwiftBaseFunctionType signature, bool hasInstance, bool isConstructor, int index)
		{
			Exceptions.ThrowOnNull (signature, nameof (signature));
			Index = index;
			int parameterIndex = 0;
			AssociatedTypes.AssociatedTypeCollection.UnionWith (signature.GetAssociatedTypes ());

			if (hasInstance || isConstructor) {
				ParameterCollection.Add (hasInstance ? DBParameter.CreateInstanceParameter (parameterIndex) : DBParameter.CreateConstructorParameter (parameterIndex));
				return;
			}

			foreach (var parameter in signature.EachParameter) {
				var type = SwiftTypeToString.MapSwiftTypeToString (parameter);
				var publicName = parameter.Name?.Name ?? "_";
				var privateName = parameter.Name?.Name ?? "privateName";

				ParameterCollection.Add (new DBParameter (type, publicName, privateName, parameterIndex, parameter.IsVariadic ? ParameterOptions.IsVariadic : ParameterOptions.None));
				parameterIndex++;
			}
		}

		public DBParameterList (string propertyType, bool hasInstance, int index)
		{
			Exceptions.ThrowOnNull (propertyType, nameof (propertyType));
			Index = index;

			if (hasInstance) {
				ParameterCollection.Add (DBParameter.CreateInstanceParameter ());
				return;
			}

			if (propertyType == "Getter") {
				ParameterCollection.Add (DBParameter.CreateGetterParameter ());
			} else {
				ParameterCollection.Add (DBParameter.CreateSetterParameter ());
			}
		}
	}

	internal class DBParameterLists : IAssociatedTypes {
		public List<DBParameterList> ParameterListCollection { get; } = new List<DBParameterList> ();
		public DBAssociatedTypes AssociatedTypes { get; } = new DBAssociatedTypes ();

		public DBParameterLists (SwiftBaseFunctionType signature, bool hasInstance)
		{
			Exceptions.ThrowOnNull (signature, nameof (signature));
			var parameterListIndex = 0;
			if (hasInstance) {
				ParameterListCollection.Add (new DBParameterList (signature, true, false, parameterListIndex));
				parameterListIndex++;
			} else if (signature.IsConstructor) {
				ParameterListCollection.Add (new DBParameterList (signature, false, true, parameterListIndex));
				parameterListIndex++;
			}

			ParameterListCollection.Add (new DBParameterList (signature, false, false, parameterListIndex));
			AssociatedTypes.AssociatedTypeCollection.UnionWith (ParameterListCollection.GetChildrenAssociatedTypes ());
		}

		public DBParameterLists (bool hasInstance, string propertyType)
		{
			Exceptions.ThrowOnNull (propertyType, nameof (propertyType));
			var parameterListIndex = 0;
			if (hasInstance) {
				ParameterListCollection.Add (new DBParameterList (propertyType, true, parameterListIndex));
				parameterListIndex++;
			}
			ParameterListCollection.Add (new DBParameterList (propertyType, false, parameterListIndex));
		}
	}
}
