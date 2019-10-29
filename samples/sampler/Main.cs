// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using SwiftRuntimeLibrary;
using Sampler;
using System.Reflection;

namespace Sampler {
	public class MainClass {
		public static void Main(string[] args) {
			Number n = Number.NewInteger(5);
			Console.WriteLine("{0}", n.ValueInteger);
			try {
				Console.WriteLine("Integer enum payload {0}", n.ValueReal);
			}
			catch (ArgumentOutOfRangeException e) {
				Console.WriteLine("Intentionally incorrectly accessed float payload.");
				Console.WriteLine("Intentionally caught exception message ValueReal: {0}", e.Message);
			}
			double d = AFinalClass.AStaticMethod();
			Console.WriteLine("Static double is {0}", d);
			bool b = AFinalClass.AStaticProp;
			AFinalClass.AStaticProp = !b;
			Console.WriteLine("Static prop read {0}, set {1}",
				b, AFinalClass.AStaticProp);
			using(AFinalClass cl = new AFinalClass(7.28f)) {
				Console.WriteLine("Instance prop {0}", cl.XGetOnly);
				Console.WriteLine("Indexer[1] {0}", cl[1]);
				AFinalClass.AStruct st = new AFinalClass.AStruct(cl);
				AFinalClass stcl = st.GetClass();
				Console.WriteLine("cl {0} st.GetClass() {1} cl == st.GetClass() {2}",
cl.SwiftObject.ToString("X"), stcl.SwiftObject.ToString("X"), cl == stcl);
			}
		}
	}
}
