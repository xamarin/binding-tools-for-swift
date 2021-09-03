// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Dynamo;
using SwiftReflector.SwiftXmlReflection;

namespace SwiftReflector {
	public class GenericReferenceRenamer {
		BaseDeclaration context;
		Func<int, int, string> genericNamer;
		public GenericReferenceRenamer (BaseDeclaration context, Func<int, int, string> genericNamer)
		{
			this.context = Exceptions.ThrowOnNull (context, nameof (context));
			this.genericNamer = Exceptions.ThrowOnNull (genericNamer, nameof (genericNamer));
		}

		public TypeSpec Rename (TypeSpec type)
		{
			bool changed;
			var newType = Rename (type, out changed);
			return changed ? newType : type;
		}

		TypeSpec Rename (TypeSpec type, out bool changed)
		{
			if (TypeSpec.IsNullOrEmptyTuple (type)) {
				changed = false;
				return type;
			}
			switch (type.Kind) {
			case TypeSpecKind.Closure:
				return Rename (type as ClosureTypeSpec, out changed);
			case TypeSpecKind.Named:
				return Rename (type as NamedTypeSpec, out changed);
			case TypeSpecKind.Tuple:
				return Rename (type as TupleTypeSpec, out changed);
			case TypeSpecKind.ProtocolList:
				return Rename (type as ProtocolListTypeSpec, out changed);
			default:
				throw new NotImplementedException ($"Unknown type spec kind {type.Kind.ToString ()}");
			}
		}

		TypeSpec Rename (ClosureTypeSpec type, out bool changed)
		{
			bool argsChanged, resultChanged;
			var args = Rename (type.Arguments, out argsChanged);
			var result = Rename (type.ReturnType, out resultChanged);
			changed = argsChanged || resultChanged;
			if (changed) {
				return new ClosureTypeSpec (args, result);
			}
			return type;
		}

		TypeSpec Rename (NamedTypeSpec type, out bool changed)
		{
			bool nameChanged = false, genArgsChanged = false;
			string newName = null;
			if (context.IsTypeSpecGenericReference (new NamedTypeSpec (type.Name))) {
				var depthIndex = context.GetGenericDepthAndIndex (type.Name);
				if (depthIndex.Item1 < 0 || depthIndex.Item2 < 0)
					throw new ArgumentOutOfRangeException (type.Name, $"not getting a useful depth or index even though marked as a generic reference");
				nameChanged = true;
				newName = genericNamer (depthIndex.Item1, depthIndex.Item2);
			}
			TypeSpec [] newGenParms = type.GenericParameters.ToArray ();
			if (type.GenericParameters != null) {
				newGenParms = new TypeSpec [type.GenericParameters.Count];
				for (int i = 0; i < newGenParms.Length; i++) {
					bool genChanged;
					newGenParms [i] = (Rename (type.GenericParameters [i], out genChanged));
					genArgsChanged = genArgsChanged || genChanged;
				}
			}
			changed = nameChanged || genArgsChanged;
			if (changed) {
				return new NamedTypeSpec (newName ?? type.Name, newGenParms);
			}
			return type;
		}

		TypeSpec Rename (TupleTypeSpec type, out bool changed)
		{
			changed = false;
			var newTupleElems = new TypeSpec [type.Elements.Count];
			for (int i = 0; i < type.Elements.Count; i++) {
				bool elemChanged;
				newTupleElems [i] = Rename (type.Elements [i], out elemChanged);
				changed = changed || elemChanged;
			}

			if (changed) {
				return new TupleTypeSpec (newTupleElems);
			}
			return type;
		}
	}
}
