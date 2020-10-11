// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if !TOM_SWIFTY
using System;
using System.Runtime.InteropServices;
using Foundation;
using SwiftRuntimeLibrary.SwiftMarshal;

namespace SwiftRuntimeLibrary {
	[SwiftTypeName ("Foundation.Date")]
	[SwiftStruct (SwiftFoundationConstants.LibSwiftFoundation, SwiftFoundationConstants.SwiftDate_NominalTypeDescriptor, SwiftFoundationConstants.SwiftData_TypeMetadata, "")]
	public class SwiftDate : SwiftNativeValueType, ISwiftStruct {
		public static SwiftDate SwiftDate_TimeIntervalSinceReferenceDate (double timeIntervalSinceReferenceDate)
		{
			unsafe {

				SwiftDate this0 = StructMarshal.DefaultValueType<SwiftDate> ();
				fixed (byte* thisDataPtr = StructMarshal.Marshaler.PrepareValueType (this0)) {
					IntPtr thisPtr = new IntPtr (thisDataPtr);
					NativeMethodsForDDate.PI_Date_timeIntervalSinceReferenceDate (thisPtr, timeIntervalSinceReferenceDate);
					return this0;
				}
			}
		}

		public SwiftDate (double timeInterval, SwiftDate since)
		{
			unsafe {
				fixed (byte* thisDataPtr = StructMarshal.Marshaler.PrepareValueType (this)) {
					fixed (byte* sinceSwiftDataPtr = StructMarshal.Marshaler.PrepareValueType (since)) {
						IntPtr thisPtr = new IntPtr (thisDataPtr);
						NativeMethodsForDDate.PI_Date (thisPtr, timeInterval, (IntPtr)sinceSwiftDataPtr);
					}
				}
			}
		}

		public static SwiftDate SwiftDate_TimeIntervalSince1970 (double timeIntervalSince1970)
		{
			unsafe {

				SwiftDate this0 = StructMarshal.DefaultValueType<SwiftDate> ();
				fixed (byte* thisDataPtr = StructMarshal.Marshaler.PrepareValueType (this0)) {
					IntPtr thisPtr = new IntPtr (thisDataPtr);
					NativeMethodsForDDate.PI_Date_timeIntervalSince1970 (thisPtr, timeIntervalSince1970);
					return this0;
				}
			}
		}
		public SwiftDate ()
		{
			unsafe {
				fixed (byte* thisDataPtr = StructMarshal.Marshaler.PrepareValueType (this)) {
					IntPtr thisPtr = new IntPtr (thisDataPtr);
					NativeMethodsForDDate.PI_DateNew (thisPtr);
				}
			}
		}

		public SwiftDate (NSDate nSDate)
		{
			unsafe {
				fixed (byte* thisDataPtr = StructMarshal.Marshaler.PrepareValueType (this)) {
					IntPtr thisPtr = new IntPtr (thisDataPtr);
					NativeMethodsForDDate.PI_DateNewFromNSDate (thisPtr, nSDate.Handle);
				}
			}
		}

		public SwiftDate (double timeIntervalSinceNow)
		{
			unsafe {
				fixed (byte* thisDataPtr = StructMarshal.Marshaler.PrepareValueType (this)) {
					IntPtr thisPtr = new IntPtr (thisDataPtr);
					NativeMethodsForDDate.PI_DateNewFromInterval (thisPtr, timeIntervalSinceNow);
				}
			}
		}

		internal SwiftDate (SwiftValueTypeCtorArgument unused)
			: base ()
		{
		}

		public static SwiftMetatype GetSwiftMetatype ()
		{
			return NativeMethodsForDDate.PIMetadataAccessor_DDate (SwiftMetadataRequest.Complete);
		}

		~SwiftDate ()
		{
			Dispose (false);
		}

		public NSDate ToNSDate ()
		{
			unsafe {
				fixed (byte* thisSwiftDataPtr = StructMarshal.Marshaler.PrepareValueType (this)) {

					NSDate retval = null;

					IntPtr retvalIntPtr = IntPtr.Zero;
					retvalIntPtr = NativeMethodsForDDate.PImethod_DateXamarin_DateDtoNSDate ((IntPtr)thisSwiftDataPtr);
					retval = ObjCRuntime.Runtime.GetNSObject<NSDate> (retvalIntPtr);
					return retval;
				}
			}
		}

		public static explicit operator NSDate (SwiftDate date)
		{
			return date.ToNSDate ();
		}
	}

	internal class NativeMethodsForDDate {
		[DllImport (SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.DateNewIntervalSinceDate)]
		internal static extern void PI_Date_timeIntervalSinceReferenceDate (IntPtr retval, double timeIntervalSinceReferenceDate);

		[DllImport (SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.DateNewIntervalSince)]
		internal static extern void PI_Date (IntPtr retval, double timeInterval, IntPtr since);

		[DllImport (SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.DateNewSince1970)]
		internal static extern void PI_Date_timeIntervalSince1970 (IntPtr retval, double timeIntervalSince1970);

		[DllImport (SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.DateNew)]
		internal static extern void PI_DateNew (IntPtr retval);

		[DllImport (SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.DateNewFromNSDate)]
		internal static extern void PI_DateNewFromNSDate (IntPtr retval, IntPtr nsDate);

		[DllImport (SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.DateNewFromInterval)]
		internal static extern void PI_DateNewFromInterval (IntPtr retval, double interval);

		[DllImport (SwiftFoundationConstants.LibSwiftFoundation, EntryPoint = SwiftFoundationConstants.SwiftDate_MetadataAcessor)]
		internal static extern SwiftMetatype PIMetadataAccessor_DDate (SwiftMetadataRequest request);

		[DllImport (SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.DateToNSDate)]
		internal static extern IntPtr PImethod_DateXamarin_DateDtoNSDate (IntPtr this0);
	}
}

#endif
