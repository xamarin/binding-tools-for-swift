using System;
using Props;
using SwiftRuntimeLibrary;

namespace Tester {
	public class MainClass {
		public static void Main(string[] args)
		{
			PropertyBag<bool> props = new PropertyBag<bool>();
			props.Add(SwiftString.FromString("healthy"), true);
			props.Add(SwiftString.FromString("wealthy"), true);
			props.Add(SwiftString.FromString("wise"), false);
			foreach (var pair in props.Contents()) {
				Console.WriteLine($"{pair.Item1} : {pair.Item2}");
			}
		}
	}
}
