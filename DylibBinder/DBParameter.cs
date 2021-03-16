using System;
using System.Collections.Generic;
using SwiftReflector;

namespace DylibBinder {
	public class DBParameter {
		public DBParameter (string type, string publicName, string privateName, bool isVariadic, bool hasInstance, bool isConstructor, int index, bool isEmptyParameter)
		{
			Type = type;
			PublicName = publicName;
			PrivateName = privateName;
			IsVariadic = isVariadic;
			Index = index;
			HasInstance = hasInstance;
			IsConstructor = isConstructor;
			IsEmptyParameter = isEmptyParameter;
		}

		public string Type { get; internal set; }
		public string PublicName { get; }
		public string PrivateName { get; }
		public bool IsVariadic { get; }
		public int Index { get; }

		public bool HasInstance { get; }
		public bool IsConstructor { get; }
		public bool IsEmptyParameter { get; }
	}


	public class DBParameterList {
		public DBParameterList (SwiftBaseFunctionType signature, bool hasInstance, bool isConstructor, int index)
		{
			Index = index;
			int parameterIndex = 0;

			if (hasInstance) {
				Parameters.Add (new DBParameter ("", "", "self", false, true, false, parameterIndex, false));
				return;
			} else if (isConstructor) {
				Parameters.Add (new DBParameter (".Type", "", "self", false, false, true, parameterIndex, false));
				return;
			}

			foreach (var parameter in signature.EachParameter) {
				var type = SwiftTypeToString.MapSwiftTypeToString (parameter);
				var publicName = "_";
				var privateName = $"privateName{PrivateNameCounter}";
				PrivateNameCounter++;

				if (parameter.Name != null) {
					publicName = privateName = parameter.Name.Name;
					PrivateNameCounter--;
				}

				Parameters.Add (new DBParameter (type, publicName, privateName, parameter.IsVariadic, false, false, parameterIndex, false));
				parameterIndex++;
			}
		}

		public DBParameterList (string type, string propertyType, bool hasInstance, int index)
		{
			Index = index;

			if (hasInstance) {
				Parameters.Add (new DBParameter ("", "", "self", false, true, false, 0, false));
				return;
			}

			if (propertyType == "Getter") {
				Parameters.Add (new DBParameter (null, null, null, false, false, false, 0, true));
			} else {
				Parameters.Add (new DBParameter (type, "_", $"privateName{PrivateNameCounter}", false, false, false, 0, false));
				PrivateNameCounter++;
			}
		}

		public int Index { get; }
		public List<DBParameter> Parameters { get; } = new List<DBParameter> ();
		static int PrivateNameCounter { get; set; }
	}


	public class DBParameterLists {
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
