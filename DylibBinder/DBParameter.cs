using System;
using System.Collections.Generic;
using SwiftReflector;

namespace DylibBinder {
	internal class DBParameter {
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

		public string Type { get; set; }
		public string PublicName { get; }
		public string PrivateName { get; }
		public bool IsVariadic { get; }
		public int Index { get; }

		public bool HasInstance { get; }
		public bool IsConstructor { get; }
		public bool IsEmptyParameter { get; }

		public static DBParameter CreateInstanceParameter () => new ("", "", "self", 0, ParameterOptions.HasInstance);
		public static DBParameter CreateInstanceParameter (int index) => new ("", "", "self", index, ParameterOptions.HasInstance);
		public static DBParameter CreateConstructorParameter (int index) => new (".Type", "", "self", index, ParameterOptions.IsConstructor);
		public static DBParameter CreateGetterParameter () => new  (null, null, null, 0, ParameterOptions.IsEmptyParameter);
		public static DBParameter CreateSetterParameter () => new ("", "", "self", 0, ParameterOptions.HasInstance);
	}


	internal class DBParameterList : IAssociatedTypes {
		public DBParameterList (SwiftBaseFunctionType signature, bool hasInstance, bool isConstructor, int index)
		{
			Index = index;
			int parameterIndex = 0;
			AssociatedTypes.Add (signature.GetAssociatedTypes ());

			if (hasInstance || isConstructor) {
				if (hasInstance)
					Parameters.Add (DBParameter.CreateInstanceParameter(parameterIndex));
				else
					Parameters.Add (DBParameter.CreateConstructorParameter (parameterIndex));
				return;
			}

			foreach (var parameter in signature.EachParameter) {
				var type = SwiftTypeToString.MapSwiftTypeToString (parameter);
				var publicName = "_";
				var privateName = "privateName";

				if (parameter.Name != null)
					publicName = privateName = parameter.Name.Name;

				Parameters.Add (new DBParameter (type, publicName, privateName, parameterIndex, parameter.IsVariadic ? ParameterOptions.IsVariadic : ParameterOptions.None));
				parameterIndex++;
			}
		}

		public DBParameterList (string propertyType, bool hasInstance, int index)
		{
			Index = index;

			if (hasInstance) {
				Parameters.Add (DBParameter.CreateInstanceParameter ());
				return;
			}

			if (propertyType == "Getter") {
				Parameters.Add (DBParameter.CreateGetterParameter ());
			} else {
				Parameters.Add (DBParameter.CreateSetterParameter ());
			}
		}

		public int Index { get; }
		public List<DBParameter> Parameters { get; } = new List<DBParameter> ();
		public DBAssociatedTypes AssociatedTypes { get; } = new DBAssociatedTypes ();


	}

	internal class DBParameterLists : IAssociatedTypes {
		public DBParameterLists (SwiftBaseFunctionType signature, bool hasInstance)
		{
			var parameterListIndex = 0;
			if (hasInstance) {
				ParameterLists.Add (new DBParameterList (signature, true, false, parameterListIndex));
				parameterListIndex++;
			} else if (signature.IsConstructor) {
				ParameterLists.Add (new DBParameterList (signature, false, true, parameterListIndex));
				parameterListIndex++;
			}

			ParameterLists.Add (new DBParameterList (signature, false, false, parameterListIndex));
			AssociatedTypes.Add (ParameterLists.GetChildrenAssociatedTypes ());
		}

		public DBParameterLists (bool hasInstance, string propertyType)
		{
			var parameterListIndex = 0;
			if (hasInstance) {
				ParameterLists.Add (new DBParameterList (propertyType, true, parameterListIndex));
				parameterListIndex++;
			}
			ParameterLists.Add (new DBParameterList (propertyType, false, parameterListIndex));
		}

		public List<DBParameterList> ParameterLists { get; } = new List<DBParameterList> ();

		public DBAssociatedTypes AssociatedTypes { get; } = new DBAssociatedTypes ();
	}
}
