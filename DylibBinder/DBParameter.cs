using System;
using System.Collections.Generic;
using SwiftReflector;

namespace DylibBinder {
	public class DBParameter {
		public DBParameter (string type, string publicName, string privateName, string isVariadic, bool hasInstance, bool isConstructor, string index, bool isEmptyParameter)
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
		public string IsVariadic { get; }
		public string Index { get; }

		public bool HasInstance { get; }
		public bool IsConstructor { get; }
		public bool IsEmptyParameter { get; }
	}


	public class DBParameterList {
		public DBParameterList (SwiftBaseFunctionType signature, bool hasInstance, bool isConstructor, string index)
		{
			Index = index;
			int counter = 0;

			if (hasInstance) {
				Parameters.Add (new DBParameter ("", "", "self", "false", true, false, counter.ToString (), false));
				return;
			} else if (isConstructor) {
				Parameters.Add (new DBParameter (".Type", "", "self", "false", false, true, counter.ToString (), false));
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
				var isVaradic = parameter.IsVariadic.ToString ();

				Parameters.Add (new DBParameter (type, publicName, privateName, isVaradic, false, false, counter.ToString (), false));
				counter++;
			}
		}

		public DBParameterList (string type, string propertyType, bool hasInstance, string index)
		{
			Index = index;

			if (hasInstance) {
				Parameters.Add (new DBParameter ("", "", "self", "false", true, false, "0", false));
				return;
			}

			if (propertyType == "Getter") {
				Parameters.Add (new DBParameter (null, null, null, null, false, false, null, true));
			} else {
				Parameters.Add (new DBParameter (type, "_", $"privateName{PrivateNameCounter}", "False", false, false, "0", false));
				PrivateNameCounter++;
			}
		}

		public string Index { get; }
		public List<DBParameter> Parameters { get; } = new List<DBParameter> ();
		static int PrivateNameCounter { get; set; }
	}


	public class DBParameterLists {
		public DBParameterLists (SwiftBaseFunctionType signature, string hasInstance)
		{
			var counter = 0;
			if (hasInstance == "True") {
				ParameterLists.Add (new DBParameterList (signature, true, false, counter.ToString ()));
				counter++;
			} else if (signature.IsConstructor) {
				ParameterLists.Add (new DBParameterList (signature, false, true, counter.ToString ()));
				counter++;
			}

			ParameterLists.Add (new DBParameterList (signature, false, false, counter.ToString ()));
		}

		public DBParameterLists (string type, string hasInstance, string propertyType)
		{
			var counter = 0;
			if (hasInstance == "True") {
				ParameterLists.Add (new DBParameterList (type, propertyType, true, counter.ToString ()));
				counter++;
			}
			ParameterLists.Add (new DBParameterList (type, propertyType, false, counter.ToString ()));
		}

		public List<DBParameterList> ParameterLists { get; } = new List<DBParameterList> ();
	}
}
