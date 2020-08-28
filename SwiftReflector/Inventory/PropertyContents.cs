// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using SwiftReflector.ExceptionTools;
using SwiftReflector.Demangling;
using ObjCRuntime;

namespace SwiftReflector.Inventory {
	public class PropertyContents {
		int sizeofMachinePointer;
		public PropertyContents (SwiftClassType ofClass, SwiftName propName, int sizeofMachinePointer)
		{
			Class = Ex.ThrowOnNull (ofClass, nameof(ofClass));
			Name = Ex.ThrowOnNull (propName, nameof(propName));
			this.sizeofMachinePointer = sizeofMachinePointer;
		}

		public SwiftClassType Class { get; private set; }

		public SwiftName Name { get; private set; }

		public void Add (TLFunction tlf, SwiftPropertyType prop)
		{
			var method = tlf as TLMethodDescriptor;
			if (method != null) {
				switch (prop.PropertyType) {
				case PropertyType.Getter:
					TLFGetterDescriptor = method;
					break;
				case PropertyType.Setter:
					TLFSetterDescriptor = method;
					break;
				case PropertyType.Materializer:
					TLFMaterializerDescriptor = method;
					break;
				case PropertyType.DidSet:
					TLFDidSetDescriptor = method;
					break;
				case PropertyType.WillSet:
					TLFWillSetDescriptor = method;
					break;
				case PropertyType.ModifyAccessor:
					TLFModifyDescriptor = method;
					break;
				default:
					throw ErrorHelper.CreateError (ReflectorError.kCantHappenBase + 68, $"Unexpected property descriptor {prop.PropertyType.ToString ()}");
				}
			} else {
				switch (prop.PropertyType) {
				case PropertyType.Getter:
					Getter = prop;
					TLFGetter = tlf;
					break;
				case PropertyType.Setter:
					Setter = prop;
					TLFSetter = tlf;
					break;
				case PropertyType.Materializer:
					Materializer = prop;
					TLFMaterializer = tlf;
					break;
				case PropertyType.DidSet:
					DidSet = prop;
					TLFDidSet = tlf;
					break;
				case PropertyType.WillSet:
					WillSet = prop;
					TLFWillSet = tlf;
					break;
				case PropertyType.ModifyAccessor:
					ModifyAccessor = prop;
					TLFModifyAccessor = tlf;
					break;
				default:
					throw ErrorHelper.CreateError (ReflectorError.kCantHappenBase + 2, $"Unexpected property element {prop.PropertyType.ToString ()}");
				}
			}
		}

		SwiftPropertyType getter;
		public SwiftPropertyType Getter {
			get {
				return getter;
			}
			set {
				ThrowOnExistingProperty (getter, "getter");
				getter = value;
			}
		}
		public TLFunction TLFGetter { get; set; }
		public TLMethodDescriptor TLFGetterDescriptor { get; set; }

		SwiftPropertyType setter;
		public SwiftPropertyType Setter {
			get {
				return setter;
			}
			set {
				ThrowOnExistingProperty (setter, "setter");
				setter = value;
			}
		}
		public TLFunction TLFSetter { get; set; }
		public TLMethodDescriptor TLFSetterDescriptor { get; set; }

		SwiftPropertyType materializer;
		public SwiftPropertyType Materializer {
			get {
				return materializer;
			}
			set {
				ThrowOnExistingProperty (materializer, "materializer");
				materializer = value;
			}
		}
		public TLFunction TLFMaterializer { get; set; }
		public TLMethodDescriptor TLFMaterializerDescriptor { get; set; }

		SwiftPropertyType modifyAccessor;
		public SwiftPropertyType ModifyAccessor {
			get { return modifyAccessor; }
			set {
				ThrowOnExistingProperty (modifyAccessor, "modifyAccessor");
				modifyAccessor = value;
			}
		}
		public TLFunction TLFModifyAccessor { get; set; }
		public TLMethodDescriptor TLFModifyDescriptor { get; set; }

		SwiftPropertyType didSet;
		public SwiftPropertyType DidSet {
			get {
				return didSet;
			}
			set {
				ThrowOnExistingProperty (didSet, "did set");
				didSet = value;
			}
		}
		public TLFunction TLFDidSet { get; set; }
		public TLMethodDescriptor TLFDidSetDescriptor { get; set; }

		SwiftPropertyType willSet;
		public SwiftPropertyType WillSet {
			get {
				return willSet;
			}
			set {
				ThrowOnExistingProperty (willSet, "will set");
				willSet = value;
			}
		}
		public TLFunction TLFWillSet { get; set; }
		public TLMethodDescriptor TLFWillSetDescriptor { get; set; }

		void ThrowOnExistingProperty (SwiftPropertyType prop, string propType)
		{
			if (prop != null)
				throw ErrorHelper.CreateError (ReflectorError.kInventoryBase + 6, $"Already have a {propType} entry for property {Name} in {Class.ClassName.ToFullyQualifiedName ()}");
		}

		public int SizeofMachinePointer { get { return sizeofMachinePointer; } }
	}

}

