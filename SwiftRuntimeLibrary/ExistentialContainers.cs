// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;
using SwiftRuntimeLibrary.SwiftMarshal;

namespace SwiftRuntimeLibrary {

	public interface ISwiftExistentialContainer {
		IntPtr Data0 { get; set; }
		IntPtr Data1 { get; set; }
		IntPtr Data2 { get; set; }
		SwiftMetatype ObjectMetadata { get; set; }
		IntPtr this [int index] { get; set; }
		int Count { get; }
		int SizeOf { get; }
		unsafe IntPtr CopyTo (IntPtr memory);
		void CopyTo <T>(ref T container) where T : ISwiftExistentialContainer;
	}

	public struct SwiftExistentialContainer0 : ISwiftExistentialContainer {
		IntPtr d0, d1, d2;
		SwiftMetatype md;
		public IntPtr Data0 { get { return d0; } set { d0 = value; } }
		public IntPtr Data1 { get { return d1; } set { d1 = value; } }
		public IntPtr Data2 { get { return d2; } set { d2 = value; } }
		public SwiftMetatype ObjectMetadata { get { return md; } set { md = value; } }
		public IntPtr this [int index] { get { throw new NotSupportedException ("This existential container has no witness table entries"); } set { throw new NotSupportedException ("SwiftAny has no witness table entries"); } }
		public int Count { get { return 0; } }
		public int SizeOf { get { return (Count + 3) * IntPtr.Size; } }

		// copy into memory
		public IntPtr CopyTo (IntPtr memory)
		{
			Marshal.WriteIntPtr (memory, Data0);
			memory += IntPtr.Size;
			Marshal.WriteIntPtr (memory, Data1);
			memory += IntPtr.Size;
			Marshal.WriteIntPtr (memory, Data2);
			memory += IntPtr.Size;
			Marshal.WriteIntPtr (memory, ObjectMetadata.Handle);
			memory += IntPtr.Size;
			return memory;
		}

		// copy to another container (of the same size, presumably
		public void CopyTo<T> (ref T to) where T : ISwiftExistentialContainer
		{
			Copy (this, ref to);
		}

		// copy from a container to memory
		internal static IntPtr CopyTo (ISwiftExistentialContainer from, IntPtr memory)
		{
			Marshal.WriteIntPtr (memory, from.Data0);
			memory += IntPtr.Size;
			Marshal.WriteIntPtr (memory, from.Data1);
			memory += IntPtr.Size;
			Marshal.WriteIntPtr (memory, from.Data2);
			memory += IntPtr.Size;
			Marshal.WriteIntPtr (memory, from.ObjectMetadata.Handle);
			memory += IntPtr.Size;

			for (int i=0; i< from.Count; i++) {
				Marshal.WriteIntPtr (memory, from [i]);
				memory += IntPtr.Size;
			}
			return memory;
		}

		// copy from one container to another implementation
		internal static void Copy<T> (ISwiftExistentialContainer from, ref T to) where T : ISwiftExistentialContainer
		{
			if (from.Count != to.Count)
				throw new ArgumentOutOfRangeException ($"{nameof (from)} and {nameof (to)} must have matching Count properties");
			to.Data0 = from.Data0;
			to.Data1 = from.Data1;
			to.Data2 = from.Data2;
			to.ObjectMetadata = from.ObjectMetadata;
			for (int i = 0; i < from.Count; i++) {
				to [i] = from [i];
			}
		}

		// copy from memory to a container0, used for boxing
		internal static void CopyTo (IntPtr memoryFrom, ref SwiftExistentialContainer0 to)
		{
			to.Data0 = Marshal.ReadIntPtr (memoryFrom);
			memoryFrom += IntPtr.Size;
			to.Data1 = Marshal.ReadIntPtr (memoryFrom);
			memoryFrom += IntPtr.Size;
			to.Data2 = Marshal.ReadIntPtr (memoryFrom);
			memoryFrom += IntPtr.Size;
			to.ObjectMetadata = new SwiftMetatype (Marshal.ReadIntPtr (memoryFrom));
			memoryFrom += IntPtr.Size;

			for (int i = 0; i < to.Count; i++) {
				to [i] = Marshal.ReadIntPtr (memoryFrom);
				memoryFrom += IntPtr.Size;
			}
		}

		public const int MaximumContainerSize = 8;

		public unsafe static object Unbox(ISwiftExistentialContainer container)
		{
			Type targetType;
			if (!SwiftTypeRegistry.Registry.TryGetValue (container.ObjectMetadata, out targetType))
				throw new SwiftRuntimeException ($"Unable to unbox swift type {container.ObjectMetadata.TypeName}.");

			byte* anyPtr = stackalloc byte [container.SizeOf];
			var anyIntPtr = new IntPtr (anyPtr);
			CopyTo (container, anyIntPtr);

			byte* resultPtr = stackalloc byte [(int)SwiftCore.StrideOf (container.ObjectMetadata)];
			var resultIntPtr = new IntPtr (resultPtr);
			AnyPinvokes.FromAny (resultIntPtr, anyIntPtr, container.ObjectMetadata);
			return StructMarshal.Marshaler.ToNet (resultIntPtr, targetType);
		}

		public static T Unbox<T> (ISwiftExistentialContainer container)
		{
			var targetType = typeof (T);
			var obj = Unbox (container);
			// this shouldn't ever happen, but...
			if (obj == null)
				throw new SwiftRuntimeException ($"Unexpected null from unboxing a container of type {container.ObjectMetadata.TypeName}");
			if (!targetType.IsAssignableFrom (obj.GetType ()))
				throw new SwiftRuntimeException ($"C# type {targetType.Name} can't be set from actual type {obj.GetType ()}");
			return (T)obj;
		}

		public static unsafe SwiftExistentialContainer0 Box (object o)
		{
			if (o == null)
				throw new ArgumentNullException (nameof (o));
			var mt = StructMarshal.Marshaler.Metatypeof (o.GetType ());
			SwiftExistentialContainer0 result = new SwiftExistentialContainer0 ();
			byte* anyPtr = stackalloc byte [result.SizeOf];
			var anyIntPtr = new IntPtr (anyPtr);

			byte* argPtr = stackalloc byte [StructMarshal.Marshaler.Strideof (o.GetType ())];
			var argIntPtr = new IntPtr (argPtr);

			StructMarshal.Marshaler.ToSwift (o, argIntPtr);

			AnyPinvokes.ToAny (anyIntPtr, argIntPtr, mt);
			CopyTo (anyIntPtr, ref result);

			return result;
		}

	}

	public struct SwiftExistentialContainer1 : ISwiftExistentialContainer {
		public SwiftExistentialContainer1 (EveryProtocol everyProtocol, IntPtr protocolWitnessTable)
		{
			d0 = everyProtocol.SwiftObject;
			d1 = IntPtr.Zero;
			d2 = IntPtr.Zero;
			md = EveryProtocol.GetSwiftMetatype ();
			wt0 = protocolWitnessTable;
		}

		public SwiftExistentialContainer1 (Type interfaceType, EveryProtocol everyProtocol)
			: this (everyProtocol, SwiftMarshal.StructMarshal.Marshaler.ProtocolWitnessof (interfaceType))
		{
		}

		public SwiftExistentialContainer1 (IntPtr memory)
		{
			d0 = Marshal.ReadIntPtr (memory);
			memory += IntPtr.Size;
			d1 = Marshal.ReadIntPtr (memory);
			memory += IntPtr.Size;
			d2 = Marshal.ReadIntPtr (memory);
			memory += IntPtr.Size;
			md = new SwiftMetatype (Marshal.ReadIntPtr (memory));
			memory += IntPtr.Size;
			wt0 = Marshal.ReadIntPtr (memory);
		}

		public SwiftExistentialContainer1 (ISwiftExistentialContainer container)
		{
			if (container.Count != 1)
				throw new ArgumentException ($"Existential container has {container.Count} elements instead of 1.");
			d0 = container.Data0;
			d1 = container.Data1;
			d2 = container.Data2;
			md = container.ObjectMetadata;
			wt0 = container [0];
		}

		internal static int IndexRangeCheck (ISwiftExistentialContainer impl, int index)
		{
			if (index < 0 || index >= impl.Count)
				throw new ArgumentOutOfRangeException (nameof (index));
			return index;
		}
		IntPtr d0, d1, d2;
		SwiftMetatype md;
		IntPtr wt0;
		public IntPtr Data0 { get { return d0; } set { d0 = value; } }
		public IntPtr Data1 { get { return d1; } set { d1 = value; } }
		public IntPtr Data2 { get { return d2; } set { d2 = value; } }
		public SwiftMetatype ObjectMetadata { get { return md; } set { md = value; } }
		public IntPtr this [int index] { get { IndexRangeCheck (this, index); return wt0; } set { IndexRangeCheck (this, index); wt0 = value; } }
		public int Count { get { return 1; } }
		public int SizeOf { get { return (Count + 3) * IntPtr.Size; } }

		public IntPtr CopyTo (IntPtr memory)
		{
			return SwiftExistentialContainer0.CopyTo (this, memory);
		}

		public void CopyTo<T> (ref T to) where T : ISwiftExistentialContainer
		{
			SwiftExistentialContainer0.Copy (this, ref to);
		}
	}

	public struct SwiftExistentialContainer2 : ISwiftExistentialContainer {
		IntPtr d0, d1, d2;
		SwiftMetatype md;
		IntPtr wt0, wt1;
		public IntPtr Data0 { get { return d0; } set { d0 = value; } }
		public IntPtr Data1 { get { return d1; } set { d1 = value; } }
		public IntPtr Data2 { get { return d2; } set { d2 = value; } }
		public SwiftMetatype ObjectMetadata { get { return md; } set { md = value; } }
		public IntPtr this [int index] {
			get {
				switch (SwiftExistentialContainer1.IndexRangeCheck (this, index)) {
				case 0: return wt0;
				case 1: return wt1;
				default: return IntPtr.Zero;
				}
			}
			set {
				switch (SwiftExistentialContainer1.IndexRangeCheck (this, index)) {
				case 0: wt0 = value; break;
				case 1: wt1 = value; break;
				default: break;
				}
			}
		}
		public int Count { get { return 2; } }
		public int SizeOf { get { return (Count + 3) * IntPtr.Size; } }

		public IntPtr CopyTo (IntPtr memory)
		{
			return SwiftExistentialContainer0.CopyTo (this, memory);
		}

		public void CopyTo<T> (ref T to) where T : ISwiftExistentialContainer
		{
			SwiftExistentialContainer0.Copy (this, ref to);
		}
	}

	public struct SwiftExistentialContainer3 : ISwiftExistentialContainer {
		IntPtr d0, d1, d2;
		SwiftMetatype md;
		IntPtr wt0, wt1, wt2;
		public IntPtr Data0 { get { return d0; } set { d0 = value; } }
		public IntPtr Data1 { get { return d1; } set { d1 = value; } }
		public IntPtr Data2 { get { return d2; } set { d2 = value; } }
		public SwiftMetatype ObjectMetadata { get { return md; } set { md = value; } }
		public IntPtr this [int index] {
			get {
				switch (SwiftExistentialContainer1.IndexRangeCheck (this, index)) {
				case 0: return wt0;
				case 1: return wt1;
				case 2: return wt2;
				default: return IntPtr.Zero;
				}
			}
			set {
				switch (SwiftExistentialContainer1.IndexRangeCheck (this, index)) {
				case 0: wt0 = value; break;
				case 1: wt1 = value; break;
				case 2: wt2 = value; break;
				default: break;
				}
			}
		}
		public int Count { get { return 3; } }
		public int SizeOf { get { return (Count + 3) * IntPtr.Size; } }

		public IntPtr CopyTo (IntPtr memory)
		{
			return SwiftExistentialContainer0.CopyTo (this, memory);
		}

		public void CopyTo<T> (ref T to) where T : ISwiftExistentialContainer
		{
			SwiftExistentialContainer0.Copy (this, ref to);
		}
	}

	public struct SwiftExistentialContainer4 : ISwiftExistentialContainer {
		IntPtr d0, d1, d2;
		SwiftMetatype md;
		IntPtr wt0, wt1, wt2, wt3;
		public IntPtr Data0 { get { return d0; } set { d0 = value; } }
		public IntPtr Data1 { get { return d1; } set { d1 = value; } }
		public IntPtr Data2 { get { return d2; } set { d2 = value; } }
		public SwiftMetatype ObjectMetadata { get { return md; } set { md = value; } }
		public IntPtr this [int index] {
			get {
				switch (SwiftExistentialContainer1.IndexRangeCheck (this, index)) {
				case 0: return wt0;
				case 1: return wt1;
				case 2: return wt2;
				case 3: return wt3;
				default: return IntPtr.Zero;
				}
			}
			set {
				switch (SwiftExistentialContainer1.IndexRangeCheck (this, index)) {
				case 0: wt0 = value; break;
				case 1: wt1 = value; break;
				case 2: wt2 = value; break;
				case 3: wt3 = value; break;
				default: break;
				}
			}
		}
		public int Count { get { return 4; } }
		public int SizeOf { get { return (Count + 3) * IntPtr.Size; } }

		public IntPtr CopyTo (IntPtr memory)
		{
			return SwiftExistentialContainer0.CopyTo (this, memory);
		}

		public void CopyTo<T> (ref T to) where T : ISwiftExistentialContainer
		{
			SwiftExistentialContainer0.Copy (this, ref to);
		}
	}

	public struct SwiftExistentialContainer5 : ISwiftExistentialContainer {
		IntPtr d0, d1, d2;
		SwiftMetatype md;
		IntPtr wt0, wt1, wt2, wt3, wt4;
		public IntPtr Data0 { get { return d0; } set { d0 = value; } }
		public IntPtr Data1 { get { return d1; } set { d1 = value; } }
		public IntPtr Data2 { get { return d2; } set { d2 = value; } }
		public SwiftMetatype ObjectMetadata { get { return md; } set { md = value; } }
		public IntPtr this [int index] {
			get {
				switch (SwiftExistentialContainer1.IndexRangeCheck (this, index)) {
				case 0: return wt0;
				case 1: return wt1;
				case 2: return wt2;
				case 3: return wt3;
				case 4: return wt4;
				default: return IntPtr.Zero;
				}
			}
			set {
				switch (SwiftExistentialContainer1.IndexRangeCheck (this, index)) {
				case 0: wt0 = value; break;
				case 1: wt1 = value; break;
				case 2: wt2 = value; break;
				case 3: wt3 = value; break;
				case 4: wt4 = value; break;
				default: break;
				}
			}
		}
		public int Count { get { return 5; } }
		public int SizeOf { get { return (Count + 3) * IntPtr.Size; } }

		public IntPtr CopyTo (IntPtr memory)
		{
			return SwiftExistentialContainer0.CopyTo (this, memory);
		}

		public void CopyTo<T> (ref T to) where T : ISwiftExistentialContainer
		{
			SwiftExistentialContainer0.Copy (this, ref to);
		}
	}

	public struct SwiftExistentialContainer6 : ISwiftExistentialContainer {
		IntPtr d0, d1, d2;
		SwiftMetatype md;
		IntPtr wt0, wt1, wt2, wt3, wt4, wt5;
		public IntPtr Data0 { get { return d0; } set { d0 = value; } }
		public IntPtr Data1 { get { return d1; } set { d1 = value; } }
		public IntPtr Data2 { get { return d2; } set { d2 = value; } }
		public SwiftMetatype ObjectMetadata { get { return md; } set { md = value; } }
		public IntPtr this [int index] {
			get {
				switch (SwiftExistentialContainer1.IndexRangeCheck (this, index)) {
				case 0: return wt0;
				case 1: return wt1;
				case 2: return wt2;
				case 3: return wt3;
				case 4: return wt4;
				case 5: return wt5;
				default: return IntPtr.Zero;
				}
			}
			set {
				switch (SwiftExistentialContainer1.IndexRangeCheck (this, index)) {
				case 0: wt0 = value; break;
				case 1: wt1 = value; break;
				case 2: wt2 = value; break;
				case 3: wt3 = value; break;
				case 4: wt4 = value; break;
				case 5: wt5 = value; break;
				default: break;
				}
			}
		}
		public int Count { get { return 6; } }
		public int SizeOf { get { return (Count + 3) * IntPtr.Size; } }

		public IntPtr CopyTo (IntPtr memory)
		{
			return SwiftExistentialContainer0.CopyTo (this, memory);
		}

		public void CopyTo<T> (ref T to) where T : ISwiftExistentialContainer
		{
			SwiftExistentialContainer0.Copy (this, ref to);
		}
	}

	public struct SwiftExistentialContainer7 : ISwiftExistentialContainer {
		IntPtr d0, d1, d2;
		SwiftMetatype md;
		IntPtr wt0, wt1, wt2, wt3, wt4, wt5, wt6;
		public IntPtr Data0 { get { return d0; } set { d0 = value; } }
		public IntPtr Data1 { get { return d1; } set { d1 = value; } }
		public IntPtr Data2 { get { return d2; } set { d2 = value; } }
		public SwiftMetatype ObjectMetadata { get { return md; } set { md = value; } }
		public IntPtr this [int index] {
			get {
				switch (SwiftExistentialContainer1.IndexRangeCheck (this, index)) {
				case 0: return wt0;
				case 1: return wt1;
				case 2: return wt2;
				case 3: return wt3;
				case 4: return wt4;
				case 5: return wt5;
				case 6: return wt6;
				default: return IntPtr.Zero;
				}
			}
			set {
				switch (SwiftExistentialContainer1.IndexRangeCheck (this, index)) {
				case 0: wt0 = value; break;
				case 1: wt1 = value; break;
				case 2: wt2 = value; break;
				case 3: wt3 = value; break;
				case 4: wt4 = value; break;
				case 5: wt5 = value; break;
				case 6: wt6 = value; break;
				default: break;
				}
			}
		}
		public int Count { get { return 7; } }
		public int SizeOf { get { return (Count + 3) * IntPtr.Size; } }

		public IntPtr CopyTo (IntPtr memory)
		{
			return SwiftExistentialContainer0.CopyTo (this, memory);
		}

		public void CopyTo<T> (ref T to) where T : ISwiftExistentialContainer
		{
			SwiftExistentialContainer0.Copy (this, ref to);
		}
	}

	public struct SwiftExistentialContainer8 : ISwiftExistentialContainer {
		IntPtr d0, d1, d2;
		SwiftMetatype md;
		IntPtr wt0, wt1, wt2, wt3, wt4, wt5, wt6, wt7;
		public IntPtr Data0 { get { return d0; } set { d0 = value; } }
		public IntPtr Data1 { get { return d1; } set { d1 = value; } }
		public IntPtr Data2 { get { return d2; } set { d2 = value; } }
		public SwiftMetatype ObjectMetadata { get { return md; } set { md = value; } }
		public IntPtr this [int index] {
			get {
				switch (SwiftExistentialContainer1.IndexRangeCheck (this, index)) {
				case 0: return wt0;
				case 1: return wt1;
				case 2: return wt2;
				case 3: return wt3;
				case 4: return wt4;
				case 5: return wt5;
				case 6: return wt6;
				case 7: return wt7;
				default: return IntPtr.Zero;
				}
			}
			set {
				switch (SwiftExistentialContainer1.IndexRangeCheck (this, index)) {
				case 0: wt0 = value; break;
				case 1: wt1 = value; break;
				case 2: wt2 = value; break;
				case 3: wt3 = value; break;
				case 4: wt4 = value; break;
				case 5: wt5 = value; break;
				case 6: wt6 = value; break;
				case 7: wt7 = value; break;
				default: break;
				}
			}
		}
		public int Count { get { return 8; } }
		public int SizeOf { get { return (Count + 3) * IntPtr.Size; } }

		public IntPtr CopyTo (IntPtr memory)
		{
			return SwiftExistentialContainer0.CopyTo (this, memory);
		}

		public void CopyTo<T> (ref T to) where T : ISwiftExistentialContainer
		{
			SwiftExistentialContainer0.Copy (this, ref to);
		}
	}

	internal static class AnyPinvokes {
		[DllImport (SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.ToAny)]
		public static extern void ToAny (IntPtr anyPtr, IntPtr argPtr, SwiftMetatype argPtrType);
		[DllImport (SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.FromAny)]
		public static extern void FromAny (IntPtr resultPtr, IntPtr anyPtr, SwiftMetatype resultType);
	}
}
