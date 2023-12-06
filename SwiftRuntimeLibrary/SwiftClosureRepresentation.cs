// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;
using SwiftRuntimeLibrary.SwiftMarshal;

namespace SwiftRuntimeLibrary {
	public struct SwiftClosureRepresentation {
		public unsafe SwiftClosureRepresentation (void *function, IntPtr data)
		{
			Function = function;
			Data = data;
#if DEBUG
			//Console.WriteLine ($"Constructed SwiftClosureRepresentation with data {data.ToString ("X8")}");
#endif
		}
		[MarshalAs (UnmanagedType.FunctionPtr)]
		public unsafe void *Function;
		public IntPtr Data;


		static IntPtr LocateRefPtrFromPartialApplicationForwarder(IntPtr p)
		{
			p = Marshal.ReadIntPtr (p + IntPtr.Size);
			return Marshal.ReadIntPtr (p + 3 * IntPtr.Size);
		}

		[UnmanagedCallersOnly]
		public static void FuncCallbackVoid (IntPtr retValPtr, IntPtr refPtr)
		{
			if (refPtr == IntPtr.Zero)
				throw new ArgumentNullException (nameof (refPtr), "Inside a closure callback, the closure data pointer was null.");
			refPtr = LocateRefPtrFromPartialApplicationForwarder(refPtr);

			var capsule = SwiftObjectRegistry.Registry.ExistingCSObjectForSwiftObject<SwiftDotNetCapsule> (refPtr);
			var delInfo = SwiftObjectRegistry.Registry.ClosureForCapsule (capsule);
			var retval = delInfo.Item1.DynamicInvoke (null);
			StructMarshal.Marshaler.ToSwift (delInfo.Item3, retval, retValPtr);
			StructMarshal.ReleaseSwiftObject (capsule);
		}

		[UnmanagedCallersOnly]
		public static void FuncCallback (IntPtr retValPtr, IntPtr args, IntPtr refPtr)
		{
			if (refPtr == IntPtr.Zero)
				throw new ArgumentNullException(nameof(refPtr), "Inside a closure callback, the closure data pointer was null.");
#if DEBUG
			//Console.WriteLine ($"FuncCallback: refPtr initially {refPtr.ToString ("X8")}");
			//Console.WriteLine ("dereferencing refPtr ");
#endif
			refPtr = LocateRefPtrFromPartialApplicationForwarder (refPtr);
#if DEBUG
			//Console.WriteLine($"FuncCallback: retValPtr {retValPtr.ToString("X8")} args {args.ToString("X8")} refPtr {refPtr.ToString("X8")}");
			//Console.WriteLine ("retValPtr: ");
			//Memory.Dump (retValPtr, 128);
			//Console.WriteLine ("args: ");
			//Memory.Dump (args, 128);
			//Console.WriteLine ("refPtr: ");
			//Memory.DumpPtrs (refPtr, 8);
#endif
			var capsule = SwiftObjectRegistry.Registry.ExistingCSObjectForSwiftObject<SwiftDotNetCapsule> (refPtr);
			var delInfo = SwiftObjectRegistry.Registry.ClosureForCapsule (capsule);
#if DEBUG
			//if (delInfo == null) {
			//	Console.WriteLine ("delInfo is null.");
			//}
#endif
			var argumentValues = StructMarshal.Marshaler.MarshalSwiftTupleMemoryToNet (args, delInfo.Item2);
			var retval = delInfo.Item1.DynamicInvoke (argumentValues);
			StructMarshal.Marshaler.ToSwift (delInfo.Item3, retval, retValPtr);
			StructMarshal.ReleaseSwiftObject (capsule);
		}

		[UnmanagedCallersOnly]
		public static void FuncCallbackVoidMaybeThrows (IntPtr retValPtr, IntPtr refPtr)
		{
			FuncCallbackMaybeThrowsImpl (retValPtr, IntPtr.Zero, refPtr);
		}

		[UnmanagedCallersOnly]
		public static void FuncCallbackMaybeThrows (IntPtr retValPtr, IntPtr args, IntPtr refPtr)
		{
			FuncCallbackMaybeThrowsImpl (retValPtr, IntPtr.Zero, refPtr);
		}

		static void FuncCallbackMaybeThrowsImpl (IntPtr retValPtr, IntPtr args, IntPtr refPtr)
		{
			// instead of a pointer to a return value, this is a pointer to a Medusa tuple of the form:
			// (T, SwiftError, bool)
			// T is the return value if and only if the bool is false and in that case
			// SwiftError will be invalid.
			// SwiftError is the exception thrown if and only if the bool is true and in that case
			// T will be invalid.
#if DEBUG
			//Console.WriteLine ($"FuncCallback: refPtr initially {refPtr.ToString ("X8")}");
			//Console.WriteLine ("dereferencing refPtr ");
#endif
			refPtr = LocateRefPtrFromPartialApplicationForwarder (refPtr);
#if DEBUG
			//Console.WriteLine($"Before call FuncCallbackMaybeThrows: retValPtr {retValPtr.ToString("X8")} args {args.ToString("X8")} refPtr {refPtr.ToString("X8")}");
			//Console.WriteLine ("retValPtr: ");
			//Memory.Dump (retValPtr, 128);
			//Console.WriteLine ("args: ");
			//Memory.Dump (args, 128);
			//Console.WriteLine ("refPtr: ");
			//Memory.DumpPtrs (refPtr, 8);
#endif
			var capsule = SwiftObjectRegistry.Registry.ExistingCSObjectForSwiftObject<SwiftDotNetCapsule> (refPtr);
			var delInfo = SwiftObjectRegistry.Registry.ClosureForCapsule (capsule);
#if DEBUG
			//if (delInfo == null) {
			//	Console.WriteLine ("delInfo is null.");
			//}
#endif
			var argumentValues = args != IntPtr.Zero ? StructMarshal.Marshaler.MarshalSwiftTupleMemoryToNet (args, delInfo.Item2) : null;
#if DEBUG
			//foreach (var arg in argumentValues) {
			//	Console.WriteLine ($"arg: {arg} type: {arg.GetType ().Name}");
			//}
#endif

			object retval = null;
			Exception thrownException = null;

			try {
				retval = delInfo.Item1.DynamicInvoke (argumentValues);
			} catch (Exception e) {
				thrownException = e;
			}

			if (thrownException != null) {
#if DEBUG
				//Console.WriteLine ($"FuncCallbackMaybeThrows: retValPtr {retValPtr.ToString ("X8")} args {args.ToString ("X8")} refPtr {refPtr.ToString ("X8")}");
				//Console.WriteLine ("before set error throws retValPtr: ");
				//Memory.Dump (retValPtr, 128);
#endif
		                StructMarshal.Marshaler.SetErrorThrown (retValPtr, SwiftError.FromException (thrownException), delInfo.Item3);
#if DEBUG
				//Console.WriteLine ($"FuncCallbackMaybeThrows: retValPtr {retValPtr.ToString ("X8")} args {args.ToString ("X8")} refPtr {refPtr.ToString ("X8")}");
				//Console.WriteLine ("after set error throws retValPtr: ");
				//Memory.Dump (retValPtr, 128);
#endif
			} else {
				StructMarshal.Marshaler.SetErrorNotThrownWithValue (retValPtr, delInfo.Item3, retval);
#if DEBUG
				//Console.WriteLine ($"After call FuncCallbackMaybeThrows: retValPtr {retValPtr.ToString ("X8")} args {args.ToString ("X8")} refPtr {refPtr.ToString ("X8")}");
				//Console.WriteLine ("retValPtr: ");
				//Memory.Dump (retValPtr, 128);
				//Console.WriteLine ("args: ");
				//Memory.Dump (args, 128);
				//Console.WriteLine ("refPtr: ");
				//Memory.DumpPtrs (refPtr, 8);
#endif
			}
			StructMarshal.ReleaseSwiftObject (capsule);
		}

		[UnmanagedCallersOnly]
		public static void ActionCallbackVoidVoid (IntPtr refPtr)
		{
			if (refPtr == IntPtr.Zero)
				throw new ArgumentNullException (nameof (refPtr), "Inside a closure callback, the closure data pointer was null.");
			refPtr = LocateRefPtrFromPartialApplicationForwarder (refPtr);
			#if DEBUG
						//Console.WriteLine($"refPtr {refPtr.ToString("X8")}");
			#endif
			var capsule = SwiftObjectRegistry.Registry.ExistingCSObjectForSwiftObject<SwiftDotNetCapsule> (refPtr);
			var delInfo = SwiftObjectRegistry.Registry.ClosureForCapsule (capsule);
			#if DEBUG
						//if (delInfo == null)
						//{
						//	Console.WriteLine("delInfo is null.");
						//}
			#endif
			delInfo.Item1.DynamicInvoke (null);
			StructMarshal.ReleaseSwiftObject (capsule);
		}


		[UnmanagedCallersOnly]
		public static void ActionCallback (IntPtr args, IntPtr refPtr)
		{
			if (refPtr == IntPtr.Zero)
				throw new ArgumentNullException (nameof (refPtr), "Inside a closure callback, the closure data pointer was null.");
			refPtr = LocateRefPtrFromPartialApplicationForwarder (refPtr);

			#if DEBUG
						//Console.WriteLine($"args {args.ToString("X8")} refPtr {refPtr.ToString("X8")}");
			#endif
			var capsule = SwiftObjectRegistry.Registry.ExistingCSObjectForSwiftObject<SwiftDotNetCapsule> (refPtr);
			var delInfo = SwiftObjectRegistry.Registry.ClosureForCapsule (capsule);
			var argumentValues = StructMarshal.Marshaler.MarshalSwiftTupleMemoryToNet (args, delInfo.Item2);
			delInfo.Item1.DynamicInvoke (argumentValues);
			StructMarshal.ReleaseSwiftObject (capsule);
		}
	}

}
