using System;
using SwiftReflector.Exceptions;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using SwiftReflector.Demangling;
using ObjCRuntime;

namespace SwiftReflector.Inventory {
	public class FunctionInventory : Inventory<OverloadInventory> {
		int sizeofMachinePointer;
		public FunctionInventory (int sizeofMachinePointer)
		{
			this.sizeofMachinePointer = sizeofMachinePointer;
		}
		public override void Add (TLDefinition tld, Stream srcStm)
		{
			TLFunction tlf = tld as TLFunction;
			if (tlf == null)
				throw ErrorHelper.CreateError (ReflectorError.kInventoryBase + 10, $"expected a top-level function but got a {tld.GetType ().Name}");

			OverloadInventory overloads = null;
			if (!values.TryGetValue (tlf.Name, out overloads)) {
				overloads = new OverloadInventory (tlf.Name, sizeofMachinePointer);
				values.Add (tlf.Name, overloads);
			}
			overloads.Add (tlf, srcStm);
		}

		public IEnumerable<Tuple<SwiftName, TLFunction>> AllMethodsNoCDTor ()
		{
			foreach (SwiftName key in values.Keys) {
				OverloadInventory oi = values [key];
				foreach (TLFunction tlf in oi.Functions)
					yield return new Tuple<SwiftName, TLFunction> (key, tlf);
			}
		}

		public List<TLFunction> MethodsWithName (SwiftName name)
		{
			var result = new List<TLFunction> ();
			foreach (var oi in Values) {
				if (oi.Name.Name == name.Name)
					result.AddRange (oi.Functions);
			}
			return result;
		}

		public List<TLFunction> MethodsWithName (string name)
		{
			SwiftName sn = new SwiftName (name, false);
			return MethodsWithName (sn);
		}

		public List<TLFunction> AllocatingConstructors ()
		{
			return MethodsWithName (Decomposer.kSwiftAllocatingConstructorName);
		}

		public List<TLFunction> DeallocatingDestructors ()
		{
			return MethodsWithName (Decomposer.kSwiftDeallocatingDestructorName);
		}
	}

}

