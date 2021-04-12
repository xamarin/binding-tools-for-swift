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
	}


	internal class DBParameterList {
		public DBParameterList (SwiftBaseFunctionType signature, bool hasInstance, bool isConstructor, int index)
		{
			Index = index;
			int parameterIndex = 0;

			if (hasInstance) {
				Parameters.Add (new DBParameter ("", "", "self", parameterIndex, ParameterOptions.HasInstance));
				return;
			} else if (isConstructor) {
				Parameters.Add (new DBParameter (".Type", "", "self", parameterIndex, ParameterOptions.IsConstructor));
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

		public DBParameterList (string type, string propertyType, bool hasInstance, int index)
		{
			Index = index;

			if (hasInstance) {
				Parameters.Add (new DBParameter ("", "", "self", 0, ParameterOptions.HasInstance));
				return;
			}

			if (propertyType == "Getter") {
				Parameters.Add (new DBParameter (null, null, null, 0, ParameterOptions.IsEmptyParameter));
			} else {
				Parameters.Add (new DBParameter (type, "_", "privateName", 0, ParameterOptions.None));
			}
		}

		public int Index { get; }
		public List<DBParameter> Parameters { get; } = new List<DBParameter> ();
	}

	internal class DBParameterLists {
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
		}

		public DBParameterLists (string type, bool hasInstance, string propertyType)
		{
			var parameterListIndex = 0;
			if (hasInstance) {
				ParameterLists.Add (new DBParameterList (type, propertyType, true, parameterListIndex));
				parameterListIndex++;
			}
			ParameterLists.Add (new DBParameterList (type, propertyType, false, parameterListIndex));
		}

		public List<DBParameterList> ParameterLists { get; } = new List<DBParameterList> ();
	}
}
