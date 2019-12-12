// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using SwiftRuntimeLibrary.SwiftMarshal;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Linq;

namespace SwiftRuntimeLibrary {
	public class SwiftCore {
#if __IOS__ || __MACOS__ || __WATCHOS__ || __TVOS__
		internal const string kXamGlue = "@rpath/XamGlue.framework/XamGlue";
#else
		internal const string kXamGlue = "XamGlue";
#endif

		[DllImport (SwiftCoreConstants.LibSwiftCore)]
		static extern void swift_retain (IntPtr p);

		public static IntPtr Retain (IntPtr p)
		{
			if (p == IntPtr.Zero)
				throw new ArgumentOutOfRangeException (nameof (p), "zero pointer passed to Retain.");
			swift_retain (p);
			return p;
		}

		[DllImport (SwiftCoreConstants.LibSwiftCore)]
		static extern bool swift_isDeallocating (IntPtr p);

		public static bool IsDeallocating (IntPtr p)
		{
			if (p == IntPtr.Zero)
				throw new ArgumentOutOfRangeException (nameof (p), "zero pointer passed to IsDeallocating.");
			return swift_isDeallocating (p);
		}

		[DllImport (SwiftCoreConstants.LibSwiftCore)]
		static extern void swift_release (IntPtr p);

		public static IntPtr Release (IntPtr p)
		{
			if (p == IntPtr.Zero)
				throw new ArgumentOutOfRangeException (nameof (p), "zero pointer passed to Release.");
			if (swift_isDeallocating (p)) {
				var format = $"X{IntPtr.Size * 2}";
				throw new SwiftRuntimeException ($"Attempt to release a swift object {p.ToString (format)} that has been deinitialized");
			}
			swift_release (p);
			return p;
		}

		[DllImport ("libobjc.A.dylib")]
		static extern void objc_retain (IntPtr p);

		public static IntPtr RetainObjC (IntPtr p)
		{
			if (p == IntPtr.Zero)
				throw new ArgumentOutOfRangeException (nameof (p), "zero pointer passed to RetainObjC.");
			objc_retain (p);
			return p;
		}

		[DllImport ("libobjc.A.dylib")]
		static extern void objc_release (IntPtr p);

		public static IntPtr ReleaseObjC (IntPtr p)
		{
			if (p == IntPtr.Zero)
				throw new ArgumentOutOfRangeException (nameof (p), "zero pointer passed to ReleaseObjC.");
			objc_release (p);
			return p;
		}

		[DllImport ("libobjc.A.dylib")]
		internal static extern IntPtr objc_lookUpClass (IntPtr utf8Name);

		[DllImport (SwiftCoreConstants.LibSwiftCore)]
		static extern void swift_unownedRetain (IntPtr p);

		public static void RetainWeak (IntPtr p)
		{
			if (p == IntPtr.Zero)
				throw new ArgumentOutOfRangeException (nameof (p), "zero pointer passed to RetainWeak.");
			swift_unownedRetain (p);
		}

		[DllImport (SwiftCoreConstants.LibSwiftCore)]
		static extern void swift_unownedRelease (IntPtr p);

		public static void ReleaseWeak (IntPtr p)
		{
			if (p == IntPtr.Zero)
				throw new ArgumentOutOfRangeException (nameof (p), "zero pointer passed to ReleaseWeak.");
			swift_unownedRelease (p);
		}

		[DllImport (SwiftCoreConstants.LibSwiftCore)]
		internal static unsafe extern void swift_beginAccess (IntPtr objPtr, byte* buffer, nuint flags, IntPtr pc);

		[DllImport (SwiftCoreConstants.LibSwiftCore)]
		internal static unsafe extern void swift_endAccess (byte* buffer);

		[DllImport (SwiftCoreConstants.LibSwiftCore)]
		static extern nint swift_retainCount (IntPtr p);

		public static nint RetainCount (IntPtr p)
		{
			if (p == IntPtr.Zero)
				throw new ArgumentOutOfRangeException (nameof (p), "zero pointer passed to RetainCount.");
			return swift_retainCount (p);
		}

		[DllImport (SwiftCoreConstants.LibSwiftCore)]
		static extern nint swift_unownedRetainCount (IntPtr p);

		public static nint UnownedRetainCount (IntPtr p)
		{
			if (p == IntPtr.Zero)
				throw new ArgumentOutOfRangeException (nameof (p), "zero pointer passed to UnownedRetainCount.");
			return swift_unownedRetainCount (p);
		}

		[DllImport (SwiftCoreConstants.LibSwiftCore)]
		static extern nint swift_weakRetainCount (IntPtr p);

		public static nint WeakRetainCount (IntPtr p)
		{
			if (p == IntPtr.Zero)
				throw new ArgumentOutOfRangeException (nameof (p), "zero pointer passed to WeakRetainCount.");
			return swift_weakRetainCount (p);
		}

		[DllImport (SwiftCoreConstants.LibSwiftCore)]
		static extern void swift_deallocObject (IntPtr p);
		internal static void Dealloc (IntPtr p)
		{
			if (p == IntPtr.Zero)
				throw new ArgumentOutOfRangeException (nameof (p), "zero pointer passed to retain no result.");
			swift_deallocObject (p);
		}

		[DllImport (kXamGlue, EntryPoint = XamGlueConstants.SwiftCore_swiftsizeof)]
		static extern nint swift_sizeof (IntPtr ignored, SwiftMetatype mt);

		internal static nint SizeOf (SwiftMetatype mt)
		{
			return swift_sizeof (IntPtr.Zero, mt);
		}

		[DllImport (kXamGlue, EntryPoint = XamGlueConstants.SwiftCore_swiftstrideof)]
		static extern nint swift_strideof (IntPtr ignored, SwiftMetatype mt);

		internal static nint StrideOf (SwiftMetatype mt)
		{
			return swift_strideof (IntPtr.Zero, mt);
		}

		[DllImport (kXamGlue, EntryPoint = XamGlueConstants.SwiftCore_swiftalignmentof)]
		static extern nint swift_alignmentof (IntPtr ignored, SwiftMetatype mt);

		internal static nint AlignmentOf (SwiftMetatype mt)
		{
			return swift_alignmentof (IntPtr.Zero, mt);
		}

		[DllImport (SwiftCoreConstants.LibSwiftCore)]
		static extern IntPtr swift_getExistentialTypeMetadata (nint classConstraint, IntPtr superClassConstraint,
			nint numProtocols, IntPtr protocolDescriptors);

		internal static unsafe SwiftMetatype ExistentialContainerMetadata (SwiftNominalTypeDescriptor [] descriptors)
		{
			var arr = stackalloc IntPtr [descriptors.Length];
			for (int i=0; i < descriptors.Length; i++) {
				arr [i] = descriptors [i].Handle;
			}
			var result = swift_getExistentialTypeMetadata (1, IntPtr.Zero, descriptors.Length, new IntPtr (arr));
			return new SwiftMetatype (result);
		}

		internal static unsafe SwiftMetatype ExistentialContainerMetadata (SwiftNominalTypeDescriptor descriptor)
		{
			var arr = stackalloc IntPtr [1];
			arr [0] = descriptor.Handle;
			var result = swift_getExistentialTypeMetadata (1, IntPtr.Zero, 1, new IntPtr (arr));
			return new SwiftMetatype (result);
		}

		[DllImport (SwiftCoreConstants.LibSwiftCore)]
		static extern IntPtr swift_getTupleTypeMetadata (nint request, nint flags, IntPtr elements,
			IntPtr labels, IntPtr proposedWitnesses);

		internal static unsafe SwiftMetatype TupleMetatype (SwiftMetatype [] tupleMetatypes)
		{
			var count = tupleMetatypes.Length;
			var flags = (nint)count; // for non-constant labels, or in 0x10000
			var metatypes = stackalloc IntPtr [tupleMetatypes.Length];
			for (int i = 0; i < count; i++) {
				metatypes [i] = tupleMetatypes [i].handle;
			}
			// the request parameter is a value to determine how swift will perform this task.
			// this is from MetadataValues.h
			// 0 - complete
			// 1 - non-transitive complete
			// 3f - layout complete
			// ff - abstract
			// from the comments, I don't see any reason to use anything but complete
			return new SwiftMetatype (swift_getTupleTypeMetadata (0, flags, new IntPtr (metatypes), IntPtr.Zero, IntPtr.Zero));
		}

		[DllImport (SwiftCoreConstants.LibSwiftCore)]
		static unsafe extern SwiftMetatype swift_getFunctionTypeMetadata (nuint flags,
				     SwiftMetatype *parameters, int *parameterFlags, SwiftMetatype returnType);

		internal static unsafe SwiftMetatype GetFunctionTypeMetadata (SwiftMetatype [] parameterTypes, int [] inOutFlags, SwiftMetatype returnType, bool throws)
		{
			nuint funcFlags = (uint)parameterTypes.Length;
			// calling convention goes in the 3rd byte. It's one of:
			// 0x000000 - swift
			// 0x010000 - block
			// 0x020000 - thin
			// 0x030000 - c
			// if we wanted to support these, we'd or them in here.

			// if the flags of parameters is useful, set it.
			bool inOutFlagsUseful = false;
			foreach (var flag in inOutFlags) {
				if (flag != 0) {
					inOutFlagsUseful = true;
					break;
				}
			}
			if (inOutFlagsUseful)
				funcFlags |= 0x02000000;
			if (throws)
				funcFlags |= 0x01000000;
			// if it's escapting, or in 0x04000000

			// on no useful flags, pass in null
	    		fixed (int *flags = inOutFlagsUseful ? inOutFlags : null) {
				// on no parameters, pass in null
				fixed (SwiftMetatype *parameters = parameterTypes.Length > 0 ? parameterTypes : null) {
					return swift_getFunctionTypeMetadata (funcFlags, parameters, flags, returnType);
				}
			}
		}

		[DllImport (SwiftCoreConstants.LibSwiftCore)]
		static extern IntPtr object_getClass (IntPtr p);

		internal static IntPtr GetClassPtr (ISwiftObject obj)
		{
			if (obj == null)
				throw new ArgumentNullException (nameof (obj));
			if (obj.SwiftObject == IntPtr.Zero)
				return IntPtr.Zero;
			return object_getClass (obj.SwiftObject);
		}

		[DllImport (SwiftCoreConstants.LibSwiftCore)]
		static extern IntPtr swift_getObjectType (IntPtr p);

		internal static SwiftMetatype GetObjectType(ISwiftObject obj)
		{
			return new SwiftMetatype (swift_getObjectType (obj.SwiftObject));
		}

		// avoid calling this unless you really know what you're doing.
		[DllImport (SwiftCoreConstants.LibSwiftCore)]
		private extern static SwiftMetatype swift_getExistentialTypeMetadata (bool isValue, IntPtr superClassConstraint, nint numProtocols, IntPtr protocolRefPtr);

		internal static SwiftMetatype AnyObjectMetatype {
			get {
				return swift_getExistentialTypeMetadata (false, IntPtr.Zero, 0, IntPtr.Zero);
			}
		}

		internal static SwiftMetatype AnyMetatype {
			get {
				return swift_getExistentialTypeMetadata (true, IntPtr.Zero, 0, IntPtr.Zero);
			}
		}


		[DllImport (SwiftCoreConstants.LibSwiftCore)]
		static extern bool swift_dynamicCast (IntPtr dest, IntPtr src, SwiftMetatype srcType, SwiftMetatype targetType, nint flags);

		unsafe static bool DynamicCast (ref object dest, object src, Type srcType, Type destType, DynamicCastFlags flags)
		{
			var destPtr = IntPtr.Zero;
			var srcPtr = IntPtr.Zero;
			var srcMetaType = StructMarshal.Marshaler.Metatypeof (srcType);
			var destMetaType = StructMarshal.Marshaler.Metatypeof (destType);
			if (src is ISwiftObject) {
				srcPtr = ((ISwiftObject)src).SwiftObject;
			} else {
				var srcData = stackalloc byte [StructMarshal.Marshaler.Sizeof (srcType)];
				srcPtr = new IntPtr (srcData);
				StructMarshal.Marshaler.ToSwift (src, srcPtr);
			}
			int size = StructMarshal.Marshaler.Sizeof (destType);
			if (size > 0) {
				var destData = stackalloc byte [size];
				destPtr = new IntPtr (destData);
			}
			if (swift_dynamicCast (destPtr, srcPtr, srcMetaType, destMetaType, (nint)(int)flags)) {
				StructMarshal.Marshaler.ReleaseSwiftPointer (srcType, srcPtr);
				if (size > 0) {
					if (typeof (ISwiftObject).IsAssignableFrom (destType)) {
						var objptr = Marshal.ReadIntPtr (destPtr);
						dest = SwiftObjectRegistry.Registry.CSObjectForSwiftObject (objptr, destType);
					} else {
						dest = StructMarshal.Marshaler.ToNet (destPtr, destType);
					}
					return true;
				}
			}
			return false;
		}

		public static bool DynamicCast<T> (ref T dst, object src, Type srcType, DynamicCastFlags flags = DynamicCastFlags.None)
		{
			if (src == null)
				return false;
			if (srcType == null)
				throw new ArgumentNullException (nameof (srcType));
			object o = null;
			if (DynamicCast (ref o, src, srcType, typeof (T), flags)) {
				dst = (T)o;
				return true;
			}
			return false;
		}

		[DllImport (kXamGlue, EntryPoint = XamGlueConstants.SwiftCore_SwiftErrorMetatype)]
		static extern IntPtr _SwiftErrorMetatype ();

		internal static SwiftMetatype SwiftErrorMetatype ()
		{
			return new SwiftMetatype (_SwiftErrorMetatype ());
		}

		[DllImport (SwiftCoreConstants.LibSwiftCore)]
		static extern IntPtr swift_getWitnessTable (IntPtr swiftConformanceDescriptor, SwiftMetatype metadata, IntPtr extraData);

		public static IntPtr ProtocolWitnessTableFromFile (string dylibFile, string conformanceIdentifier, SwiftMetatype metadata)
		{
			using (var dylib = new DynamicLib (dylibFile, DLOpenMode.Now)) {
				var descriptor = dylib.FindSymbolAddress (conformanceIdentifier);
				if (descriptor == IntPtr.Zero)
					throw new SwiftRuntimeException ($"Unable to find swift protocol conformance descriptor {conformanceIdentifier} in file {dylib}");
				return swift_getWitnessTable (descriptor, metadata, IntPtr.Zero);
			}
		}

		[DllImport (SwiftCoreConstants.LibSwiftCore)]
		static extern IntPtr swift_conformsToProtocol (SwiftMetatype metadata, SwiftNominalTypeDescriptor protocolDescriptor);

		public static SwiftProtocolWitnessTable ConformsToSwiftProtocol (SwiftMetatype metadata, SwiftNominalTypeDescriptor protocolDescriptor)
		{
			return new SwiftProtocolWitnessTable (swift_conformsToProtocol (metadata, protocolDescriptor));
		}

		struct MetadataResponse {
			public SwiftMetatype Metadata;
			public nint ResponseState;
		}

		[DllImport (SwiftCoreConstants.LibSwiftCore)]
		static extern MetadataResponse swift_getAssociatedTypeWitness (SwiftMetadataRequest request, SwiftProtocolWitnessTable witness,
			SwiftMetatype conformingType, IntPtr conformanceBaseDescriptor, IntPtr conformanceRequest);

		internal static SwiftMetatype AssociatedTypeMetadataRequest (SwiftMetatype conformingType, SwiftProtocolWitnessTable witness,
			IntPtr protocolRequirementsBaseDescriptor, SwiftAssociatedTypeDescriptor assocDesc)
		{
			var response = swift_getAssociatedTypeWitness (SwiftMetadataRequest.Complete, witness, conformingType,
				protocolRequirementsBaseDescriptor, assocDesc.Handle);
			if (response.ResponseState > 1) // 0 and 1 are ok for us
				throw new SwiftRuntimeException ($"Error retrieving associated type from protocol - returned {response.ResponseState}");
			return response.Metadata;
		}

		#region ClosureAdapters

		[DllImport (kXamGlue, EntryPoint = XamGlueConstants.SwiftCore_ActionToSwiftClosure1)]
		internal static extern BlindSwiftClosureRepresentation ActionToSwiftClosure (SwiftClosureRepresentation clos,
											  SwiftMetatype t1
											 );

		[DllImport (kXamGlue, EntryPoint = XamGlueConstants.SwiftCore_ActionToSwiftClosure2)]
		internal static extern BlindSwiftClosureRepresentation ActionToSwiftClosure (SwiftClosureRepresentation clos,
											  SwiftMetatype t1, SwiftMetatype t2
											 );

		[DllImport (kXamGlue, EntryPoint = XamGlueConstants.SwiftCore_ActionToSwiftClosure3)]
		internal static extern BlindSwiftClosureRepresentation ActionToSwiftClosure (SwiftClosureRepresentation clos,
											  SwiftMetatype t1, SwiftMetatype t2,
											  SwiftMetatype t3
											 );

		[DllImport (kXamGlue, EntryPoint = XamGlueConstants.SwiftCore_ActionToSwiftClosure4)]
		internal static extern BlindSwiftClosureRepresentation ActionToSwiftClosure (SwiftClosureRepresentation clos,
											  SwiftMetatype t1, SwiftMetatype t2,
											  SwiftMetatype t3, SwiftMetatype t4
											 );

		[DllImport (kXamGlue, EntryPoint = XamGlueConstants.SwiftCore_ActionToSwiftClosure5)]
		internal static extern BlindSwiftClosureRepresentation ActionToSwiftClosure (SwiftClosureRepresentation clos,
											  SwiftMetatype t1, SwiftMetatype t2,
											  SwiftMetatype t3, SwiftMetatype t4,
											  SwiftMetatype t5
											 );

		[DllImport (kXamGlue, EntryPoint = XamGlueConstants.SwiftCore_ActionToSwiftClosure6)]
		internal static extern BlindSwiftClosureRepresentation ActionToSwiftClosure (SwiftClosureRepresentation clos,
											  SwiftMetatype t1, SwiftMetatype t2,
											  SwiftMetatype t3, SwiftMetatype t4,
											  SwiftMetatype t5, SwiftMetatype t6
											 );

		[DllImport (kXamGlue, EntryPoint = XamGlueConstants.SwiftCore_ActionToSwiftClosure7)]
		internal static extern BlindSwiftClosureRepresentation ActionToSwiftClosure (SwiftClosureRepresentation clos,
											  SwiftMetatype t1, SwiftMetatype t2,
											  SwiftMetatype t3, SwiftMetatype t4,
											  SwiftMetatype t5, SwiftMetatype t6,
											  SwiftMetatype t7
												 );

		[DllImport (kXamGlue, EntryPoint = XamGlueConstants.SwiftCore_ActionToSwiftClosure8)]
		internal static extern BlindSwiftClosureRepresentation ActionToSwiftClosure (SwiftClosureRepresentation clos,
											  SwiftMetatype t1, SwiftMetatype t2,
											  SwiftMetatype t3, SwiftMetatype t4,
											  SwiftMetatype t5, SwiftMetatype t6,
											  SwiftMetatype t7, SwiftMetatype t8
											 );


		[DllImport (kXamGlue, EntryPoint = XamGlueConstants.SwiftCore_ActionToSwiftClosure9)]
		internal static extern BlindSwiftClosureRepresentation ActionToSwiftClosure (SwiftClosureRepresentation clos,
											  SwiftMetatype t1, SwiftMetatype t2,
											  SwiftMetatype t3, SwiftMetatype t4,
											  SwiftMetatype t5, SwiftMetatype t6,
											  SwiftMetatype t7, SwiftMetatype t8,
											  SwiftMetatype t9
											 );


		[DllImport (kXamGlue, EntryPoint = XamGlueConstants.SwiftCore_ActionToSwiftClosure10)]
		internal static extern BlindSwiftClosureRepresentation ActionToSwiftClosure (SwiftClosureRepresentation clos,
											  SwiftMetatype t1, SwiftMetatype t2,
											  SwiftMetatype t3, SwiftMetatype t4,
											  SwiftMetatype t5, SwiftMetatype t6,
											  SwiftMetatype t7, SwiftMetatype t8,
											  SwiftMetatype t9, SwiftMetatype t10
											 );

		[DllImport (kXamGlue, EntryPoint = XamGlueConstants.SwiftCore_ActionToSwiftClosure11)]
		internal static extern BlindSwiftClosureRepresentation ActionToSwiftClosure (SwiftClosureRepresentation clos,
											  SwiftMetatype t1, SwiftMetatype t2,
											  SwiftMetatype t3, SwiftMetatype t4,
											  SwiftMetatype t5, SwiftMetatype t6,
											  SwiftMetatype t7, SwiftMetatype t8,
											  SwiftMetatype t9, SwiftMetatype t10,
											  SwiftMetatype t11
											 );

		[DllImport (kXamGlue, EntryPoint = XamGlueConstants.SwiftCore_ActionToSwiftClosure12)]
		internal static extern BlindSwiftClosureRepresentation ActionToSwiftClosure (SwiftClosureRepresentation clos,
											  SwiftMetatype t1, SwiftMetatype t2,
											  SwiftMetatype t3, SwiftMetatype t4,
											  SwiftMetatype t5, SwiftMetatype t6,
											  SwiftMetatype t7, SwiftMetatype t8,
											  SwiftMetatype t9, SwiftMetatype t10,
											  SwiftMetatype t11, SwiftMetatype t12
											 );

		[DllImport (kXamGlue, EntryPoint = XamGlueConstants.SwiftCore_ActionToSwiftClosure13)]
		internal static extern BlindSwiftClosureRepresentation ActionToSwiftClosure (SwiftClosureRepresentation clos,
											  SwiftMetatype t1, SwiftMetatype t2,
											  SwiftMetatype t3, SwiftMetatype t4,
											  SwiftMetatype t5, SwiftMetatype t6,
											  SwiftMetatype t7, SwiftMetatype t8,
											  SwiftMetatype t9, SwiftMetatype t10,
											  SwiftMetatype t11, SwiftMetatype t12,
											  SwiftMetatype t13
											 );

		[DllImport (kXamGlue, EntryPoint = XamGlueConstants.SwiftCore_ActionToSwiftClosure14)]
		internal static extern BlindSwiftClosureRepresentation ActionToSwiftClosure (SwiftClosureRepresentation clos,
											  SwiftMetatype t1, SwiftMetatype t2,
											  SwiftMetatype t3, SwiftMetatype t4,
											  SwiftMetatype t5, SwiftMetatype t6,
											  SwiftMetatype t7, SwiftMetatype t8,
											  SwiftMetatype t9, SwiftMetatype t10,
											  SwiftMetatype t11, SwiftMetatype t12,
											  SwiftMetatype t13, SwiftMetatype t14
											 );

		[DllImport (kXamGlue, EntryPoint = XamGlueConstants.SwiftCore_ActionToSwiftClosure15)]
		internal static extern BlindSwiftClosureRepresentation ActionToSwiftClosure (SwiftClosureRepresentation clos,
											  SwiftMetatype t1, SwiftMetatype t2,
											  SwiftMetatype t3, SwiftMetatype t4,
											  SwiftMetatype t5, SwiftMetatype t6,
											  SwiftMetatype t7, SwiftMetatype t8,
											  SwiftMetatype t9, SwiftMetatype t10,
											  SwiftMetatype t11, SwiftMetatype t12,
											  SwiftMetatype t13, SwiftMetatype t14,
											  SwiftMetatype t15
											 );


		[DllImport (kXamGlue, EntryPoint = XamGlueConstants.SwiftCore_ActionToSwiftClosure16)]
		internal static extern BlindSwiftClosureRepresentation ActionToSwiftClosure (SwiftClosureRepresentation clos,
											  SwiftMetatype t1, SwiftMetatype t2,
											  SwiftMetatype t3, SwiftMetatype t4,
											  SwiftMetatype t5, SwiftMetatype t6,
											  SwiftMetatype t7, SwiftMetatype t8,
											  SwiftMetatype t9, SwiftMetatype t10,
											  SwiftMetatype t11, SwiftMetatype t12,
											  SwiftMetatype t13, SwiftMetatype t14,
											  SwiftMetatype t15, SwiftMetatype t16
											 );


		[DllImport (kXamGlue, EntryPoint = XamGlueConstants.SwiftCore_FuncToSwiftClosure1)]
		internal static extern BlindSwiftClosureRepresentation FuncToSwiftClosure (SwiftClosureRepresentation clos,

										SwiftMetatype tResult);


		[DllImport (kXamGlue, EntryPoint = XamGlueConstants.SwiftCore_FuncToSwiftClosure2)]
		internal static extern BlindSwiftClosureRepresentation FuncToSwiftClosure (SwiftClosureRepresentation clos,
											SwiftMetatype t1,

											SwiftMetatype tResult);

		[DllImport (kXamGlue, EntryPoint = XamGlueConstants.SwiftCore_FuncToSwiftClosure3)]
		internal static extern BlindSwiftClosureRepresentation FuncToSwiftClosure (SwiftClosureRepresentation clos,
											SwiftMetatype t1, SwiftMetatype t2,

											SwiftMetatype tResult);

		[DllImport (kXamGlue, EntryPoint = XamGlueConstants.SwiftCore_FuncToSwiftClosure4)]
		internal static extern BlindSwiftClosureRepresentation FuncToSwiftClosure (SwiftClosureRepresentation clos,
											SwiftMetatype t1, SwiftMetatype t2,
											SwiftMetatype t3,

											SwiftMetatype tResult);

		[DllImport (kXamGlue, EntryPoint = XamGlueConstants.SwiftCore_FuncToSwiftClosure5)]
		internal static extern BlindSwiftClosureRepresentation FuncToSwiftClosure (SwiftClosureRepresentation clos,
											SwiftMetatype t1, SwiftMetatype t2,
											SwiftMetatype t3, SwiftMetatype t4,

											SwiftMetatype tResult);

		[DllImport (kXamGlue, EntryPoint = XamGlueConstants.SwiftCore_FuncToSwiftClosure6)]
		internal static extern BlindSwiftClosureRepresentation FuncToSwiftClosure (SwiftClosureRepresentation clos,
											SwiftMetatype t1, SwiftMetatype t2,
											SwiftMetatype t3, SwiftMetatype t4,
											SwiftMetatype t5,

											SwiftMetatype tResult);

		[DllImport (kXamGlue, EntryPoint = XamGlueConstants.SwiftCore_FuncToSwiftClosure7)]
		internal static extern BlindSwiftClosureRepresentation FuncToSwiftClosure (SwiftClosureRepresentation clos,
											SwiftMetatype t1, SwiftMetatype t2,
											SwiftMetatype t3, SwiftMetatype t4,
											SwiftMetatype t5, SwiftMetatype t6,

											SwiftMetatype tResult);

		[DllImport (kXamGlue, EntryPoint = XamGlueConstants.SwiftCore_FuncToSwiftClosure8)]
		internal static extern BlindSwiftClosureRepresentation FuncToSwiftClosure (SwiftClosureRepresentation clos,
											SwiftMetatype t1, SwiftMetatype t2,
											SwiftMetatype t3, SwiftMetatype t4,
											SwiftMetatype t5, SwiftMetatype t6,
											SwiftMetatype t7,

											SwiftMetatype tResult);


		[DllImport (kXamGlue, EntryPoint = XamGlueConstants.SwiftCore_FuncToSwiftClosure9)]
		internal static extern BlindSwiftClosureRepresentation FuncToSwiftClosure (SwiftClosureRepresentation clos,
											SwiftMetatype t1, SwiftMetatype t2,
											SwiftMetatype t3, SwiftMetatype t4,
											SwiftMetatype t5, SwiftMetatype t6,
											SwiftMetatype t7, SwiftMetatype t8,

											SwiftMetatype tResult);


		[DllImport (kXamGlue, EntryPoint = XamGlueConstants.SwiftCore_FuncToSwiftClosure10)]
		internal static extern BlindSwiftClosureRepresentation FuncToSwiftClosure (SwiftClosureRepresentation clos,
											SwiftMetatype t1, SwiftMetatype t2,
											SwiftMetatype t3, SwiftMetatype t4,
											SwiftMetatype t5, SwiftMetatype t6,
											SwiftMetatype t7, SwiftMetatype t8,
											SwiftMetatype t9,

											SwiftMetatype tResult);


		[DllImport (kXamGlue, EntryPoint = XamGlueConstants.SwiftCore_FuncToSwiftClosure11)]
		internal static extern BlindSwiftClosureRepresentation FuncToSwiftClosure (SwiftClosureRepresentation clos,
											SwiftMetatype t1, SwiftMetatype t2,
											SwiftMetatype t3, SwiftMetatype t4,
											SwiftMetatype t5, SwiftMetatype t6,
											SwiftMetatype t7, SwiftMetatype t8,
											SwiftMetatype t9, SwiftMetatype t10,

											SwiftMetatype tResult);


		[DllImport (kXamGlue, EntryPoint = XamGlueConstants.SwiftCore_FuncToSwiftClosure12)]
		internal static extern BlindSwiftClosureRepresentation FuncToSwiftClosure (SwiftClosureRepresentation clos,
											SwiftMetatype t1, SwiftMetatype t2,
											SwiftMetatype t3, SwiftMetatype t4,
											SwiftMetatype t5, SwiftMetatype t6,
											SwiftMetatype t7, SwiftMetatype t8,
											SwiftMetatype t9, SwiftMetatype t10,
											SwiftMetatype t11,

											SwiftMetatype tResult);


		[DllImport (kXamGlue, EntryPoint = XamGlueConstants.SwiftCore_FuncToSwiftClosure13)]
		internal static extern BlindSwiftClosureRepresentation FuncToSwiftClosure (SwiftClosureRepresentation clos,
											SwiftMetatype t1, SwiftMetatype t2,
											SwiftMetatype t3, SwiftMetatype t4,
											SwiftMetatype t5, SwiftMetatype t6,
											SwiftMetatype t7, SwiftMetatype t8,
											SwiftMetatype t9, SwiftMetatype t10,
											SwiftMetatype t11, SwiftMetatype t12,

											SwiftMetatype tResult);


		[DllImport (kXamGlue, EntryPoint = XamGlueConstants.SwiftCore_FuncToSwiftClosure14)]
		internal static extern BlindSwiftClosureRepresentation FuncToSwiftClosure (SwiftClosureRepresentation clos,
											SwiftMetatype t1, SwiftMetatype t2,
											SwiftMetatype t3, SwiftMetatype t4,
											SwiftMetatype t5, SwiftMetatype t6,
											SwiftMetatype t7, SwiftMetatype t8,
											SwiftMetatype t9, SwiftMetatype t10,
											SwiftMetatype t11, SwiftMetatype t12,
											SwiftMetatype t13,

											SwiftMetatype tResult);


		[DllImport (kXamGlue, EntryPoint = XamGlueConstants.SwiftCore_FuncToSwiftClosure15)]
		internal static extern BlindSwiftClosureRepresentation FuncToSwiftClosure (SwiftClosureRepresentation clos,
											SwiftMetatype t1, SwiftMetatype t2,
											SwiftMetatype t3, SwiftMetatype t4,
											SwiftMetatype t5, SwiftMetatype t6,
											SwiftMetatype t7, SwiftMetatype t8,
											SwiftMetatype t9, SwiftMetatype t10,
											SwiftMetatype t11, SwiftMetatype t12,
											SwiftMetatype t13, SwiftMetatype t14,

											SwiftMetatype tResult);



		[DllImport (kXamGlue, EntryPoint = XamGlueConstants.SwiftCore_FuncToSwiftClosure16)]
		internal static extern BlindSwiftClosureRepresentation FuncToSwiftClosure (SwiftClosureRepresentation clos,
											SwiftMetatype t1, SwiftMetatype t2,
											SwiftMetatype t3, SwiftMetatype t4,
											SwiftMetatype t5, SwiftMetatype t6,
											SwiftMetatype t7, SwiftMetatype t8,
											SwiftMetatype t9, SwiftMetatype t10,
											SwiftMetatype t11, SwiftMetatype t12,
											SwiftMetatype t13, SwiftMetatype t14,
											SwiftMetatype t15,

											SwiftMetatype tResult);


		[DllImport (kXamGlue, EntryPoint = XamGlueConstants.SwiftCore_FuncToSwiftClosure17)]
		internal static extern BlindSwiftClosureRepresentation FuncToSwiftClosure (SwiftClosureRepresentation clos,
		SwiftMetatype t1, SwiftMetatype t2,
											SwiftMetatype t3, SwiftMetatype t4,
											SwiftMetatype t5, SwiftMetatype t6,
											SwiftMetatype t7, SwiftMetatype t8,
											SwiftMetatype t9, SwiftMetatype t10,
											SwiftMetatype t11, SwiftMetatype t12,
											SwiftMetatype t13, SwiftMetatype t14,
											SwiftMetatype t15, SwiftMetatype t16,

											SwiftMetatype tResult);



		#endregion

	}
}

