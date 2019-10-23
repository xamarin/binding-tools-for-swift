// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using SwiftReflector.Exceptions;
using System.IO;
using SwiftReflector.Demangling;
using ObjCRuntime;

namespace SwiftReflector.Inventory {
	public class OverloadInventory : Inventory<List<TLFunction>> {
		int sizeofMachinePointer;
		public OverloadInventory (SwiftName name, int sizeofMachinePointer)
		{
			Name = Ex.ThrowOnNull (name, nameof(name));
			Functions = new List<TLFunction> ();
			this.sizeofMachinePointer = sizeofMachinePointer;
		}

		public override void Add (TLDefinition tld, Stream srcStm)
		{
			TLFunction tlf = tld as TLFunction;
			if (tlf == null)
				throw ErrorHelper.CreateError (ReflectorError.kInventoryBase + 11, $"expected a top-level function but got a {tld.GetType ().Name}");
			if (Functions.Exists (f => tlf.MangledName == f.MangledName)) {
				throw ErrorHelper.CreateError (ReflectorError.kInventoryBase + 12, $"duplicate function found for {tlf.MangledName}");
			} else {
				Functions.Add (tlf);
			}
		}

		public List<TLFunction> Functions { get; private set; }

		public SwiftName Name { get; private set; }
		public int SizeofMachinePointer { get { return sizeofMachinePointer; } }
	}

}

