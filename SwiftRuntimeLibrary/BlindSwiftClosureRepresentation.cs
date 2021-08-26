// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;

namespace SwiftRuntimeLibrary {
	public struct BlindSwiftClosureRepresentation {
		public BlindSwiftClosureRepresentation (IntPtr function, IntPtr data)
		{
#if DEBUG
			//Console.WriteLine ("Constructed blind swift closure with data: " + data.ToString ("X8"));
#endif
			Function = function;
			Data = data;
		}
		public IntPtr Function;
		public IntPtr Data;

		public override bool Equals (object obj)
		{
			if (obj == null || !(obj is BlindSwiftClosureRepresentation))
				return false;
			var other = (BlindSwiftClosureRepresentation)obj;
			return other.Function == Function && other.Data == Data;
		}

		public override int GetHashCode ()
		{
			return (int)(Function.ToInt64 () + Data.ToInt64 ());
		}


		[DllImport (SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.BlindSwiftClosureRepresentation_InvokePlainAction)]
		internal static extern void InvokePlainAction (BlindSwiftClosureRepresentation clos);

		[DllImport (SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.BlindSwiftClosureRepresentation_InvokeAction1)]
		internal static extern void InvokeAction (BlindSwiftClosureRepresentation clos, IntPtr args, SwiftMetatype t1);

		[DllImport (SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.BlindSwiftClosureRepresentation_InvokeAction2)]
		internal static extern void InvokeAction (BlindSwiftClosureRepresentation clos, IntPtr args, SwiftMetatype t1, SwiftMetatype t2);

		[DllImport (SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.BlindSwiftClosureRepresentation_InvokeAction3)]
		internal static extern void InvokeAction (BlindSwiftClosureRepresentation clos, IntPtr args, SwiftMetatype t1, SwiftMetatype t2, SwiftMetatype t3);

		[DllImport (SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.BlindSwiftClosureRepresentation_InvokeAction4)]
		internal static extern void InvokeAction (BlindSwiftClosureRepresentation clos, IntPtr args, SwiftMetatype t1, SwiftMetatype t2, SwiftMetatype t3, SwiftMetatype t4);

		[DllImport (SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.BlindSwiftClosureRepresentation_InvokeAction5)]
		internal static extern void InvokeAction (BlindSwiftClosureRepresentation clos, IntPtr args, SwiftMetatype t1, SwiftMetatype t2, SwiftMetatype t3, SwiftMetatype t4,
							  SwiftMetatype t5);

		[DllImport (SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.BlindSwiftClosureRepresentation_InvokeAction6)]
		internal static extern void InvokeAction (BlindSwiftClosureRepresentation clos, IntPtr args, SwiftMetatype t1, SwiftMetatype t2, SwiftMetatype t3, SwiftMetatype t4,
							  SwiftMetatype t5, SwiftMetatype t6);

		[DllImport (SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.BlindSwiftClosureRepresentation_InvokeAction7)]
		internal static extern void InvokeAction (BlindSwiftClosureRepresentation clos, IntPtr args, SwiftMetatype t1, SwiftMetatype t2, SwiftMetatype t3, SwiftMetatype t4,
							  SwiftMetatype t5, SwiftMetatype t6, SwiftMetatype t7);

		[DllImport (SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.BlindSwiftClosureRepresentation_InvokeAction8)]
		internal static extern void InvokeAction (BlindSwiftClosureRepresentation clos, IntPtr args, SwiftMetatype t1, SwiftMetatype t2, SwiftMetatype t3, SwiftMetatype t4,
							  SwiftMetatype t5, SwiftMetatype t6, SwiftMetatype t7, SwiftMetatype t8);

		[DllImport (SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.BlindSwiftClosureRepresentation_InvokeAction9)]
		internal static extern void InvokeAction (BlindSwiftClosureRepresentation clos, IntPtr args, SwiftMetatype t1, SwiftMetatype t2, SwiftMetatype t3, SwiftMetatype t4,
							  SwiftMetatype t5, SwiftMetatype t6, SwiftMetatype t7, SwiftMetatype t8,
							  SwiftMetatype t9);

		[DllImport (SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.BlindSwiftClosureRepresentation_InvokeAction10)]
		internal static extern void InvokeAction (BlindSwiftClosureRepresentation clos, IntPtr args, SwiftMetatype t1, SwiftMetatype t2, SwiftMetatype t3, SwiftMetatype t4,
							  SwiftMetatype t5, SwiftMetatype t6, SwiftMetatype t7, SwiftMetatype t8,
							  SwiftMetatype t9, SwiftMetatype t10);

		[DllImport (SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.BlindSwiftClosureRepresentation_InvokeAction11)]
		internal static extern void InvokeAction (BlindSwiftClosureRepresentation clos, IntPtr args, SwiftMetatype t1, SwiftMetatype t2, SwiftMetatype t3, SwiftMetatype t4,
							  SwiftMetatype t5, SwiftMetatype t6, SwiftMetatype t7, SwiftMetatype t8,
							  SwiftMetatype t9, SwiftMetatype t10, SwiftMetatype t11);

		[DllImport (SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.BlindSwiftClosureRepresentation_InvokeAction12)]
		internal static extern void InvokeAction (BlindSwiftClosureRepresentation clos, IntPtr args, SwiftMetatype t1, SwiftMetatype t2, SwiftMetatype t3, SwiftMetatype t4,
							  SwiftMetatype t5, SwiftMetatype t6, SwiftMetatype t7, SwiftMetatype t8,
							  SwiftMetatype t9, SwiftMetatype t10, SwiftMetatype t11, SwiftMetatype t12);

		[DllImport (SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.BlindSwiftClosureRepresentation_InvokeAction13)]
		internal static extern void InvokeAction (BlindSwiftClosureRepresentation clos, IntPtr args, SwiftMetatype t1, SwiftMetatype t2, SwiftMetatype t3, SwiftMetatype t4,
							  SwiftMetatype t5, SwiftMetatype t6, SwiftMetatype t7, SwiftMetatype t8,
							  SwiftMetatype t9, SwiftMetatype t10, SwiftMetatype t11, SwiftMetatype t12,
							  SwiftMetatype t13);

		[DllImport (SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.BlindSwiftClosureRepresentation_InvokeAction14)]
		internal static extern void InvokeAction (BlindSwiftClosureRepresentation clos, IntPtr args, SwiftMetatype t1, SwiftMetatype t2, SwiftMetatype t3, SwiftMetatype t4,
							  SwiftMetatype t5, SwiftMetatype t6, SwiftMetatype t7, SwiftMetatype t8,
							  SwiftMetatype t9, SwiftMetatype t10, SwiftMetatype t11, SwiftMetatype t12,
							  SwiftMetatype t13, SwiftMetatype t14);

		[DllImport (SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.BlindSwiftClosureRepresentation_InvokeAction15)]
		internal static extern void InvokeAction (BlindSwiftClosureRepresentation clos, IntPtr args, SwiftMetatype t1, SwiftMetatype t2, SwiftMetatype t3, SwiftMetatype t4,
							  SwiftMetatype t5, SwiftMetatype t6, SwiftMetatype t7, SwiftMetatype t8,
							  SwiftMetatype t9, SwiftMetatype t10, SwiftMetatype t11, SwiftMetatype t12,
							  SwiftMetatype t13, SwiftMetatype t14, SwiftMetatype t15);

		[DllImport (SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.BlindSwiftClosureRepresentation_InvokeAction16)]
		internal static extern void InvokeAction (BlindSwiftClosureRepresentation clos, IntPtr args, SwiftMetatype t1, SwiftMetatype t2, SwiftMetatype t3, SwiftMetatype t4,
							  SwiftMetatype t5, SwiftMetatype t6, SwiftMetatype t7, SwiftMetatype t8,
							  SwiftMetatype t9, SwiftMetatype t10, SwiftMetatype t11, SwiftMetatype t12,
							  SwiftMetatype t13, SwiftMetatype t14, SwiftMetatype t15, SwiftMetatype t16);

		[DllImport (SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.BlindSwiftClosureRepresentation_InvokeFunction1)]
		internal static extern void InvokeFunction (BlindSwiftClosureRepresentation clos, IntPtr retval, SwiftMetatype tr);

		[DllImport (SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.BlindSwiftClosureRepresentation_InvokeFunction2)]
		internal static extern void InvokeFunction (BlindSwiftClosureRepresentation clos, IntPtr retval, IntPtr args, SwiftMetatype t1, SwiftMetatype tr);

		[DllImport (SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.BlindSwiftClosureRepresentation_InvokeFunction3)]
		internal static extern void InvokeFunction (BlindSwiftClosureRepresentation clos, IntPtr retval, IntPtr args, SwiftMetatype t1, SwiftMetatype t2, SwiftMetatype tr);

		[DllImport (SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.BlindSwiftClosureRepresentation_InvokeFunction4)]
		internal static extern void InvokeFunction (BlindSwiftClosureRepresentation clos, IntPtr retval, IntPtr args, SwiftMetatype t1, SwiftMetatype t2, SwiftMetatype t3, SwiftMetatype tr);

		[DllImport (SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.BlindSwiftClosureRepresentation_InvokeFunction5)]
		internal static extern void InvokeFunction (BlindSwiftClosureRepresentation clos, IntPtr retval, IntPtr args, SwiftMetatype t1, SwiftMetatype t2, SwiftMetatype t3, SwiftMetatype t4, SwiftMetatype tr);

		[DllImport (SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.BlindSwiftClosureRepresentation_InvokeFunction6)]
		internal static extern void InvokeFunction (BlindSwiftClosureRepresentation clos, IntPtr retval, IntPtr args, SwiftMetatype t1, SwiftMetatype t2, SwiftMetatype t3, SwiftMetatype t4,
							    SwiftMetatype t5, SwiftMetatype tr);

		[DllImport (SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.BlindSwiftClosureRepresentation_InvokeFunction7)]
		internal static extern void InvokeFunction (BlindSwiftClosureRepresentation clos, IntPtr retval, IntPtr args, SwiftMetatype t1, SwiftMetatype t2, SwiftMetatype t3, SwiftMetatype t4,
							    SwiftMetatype t5, SwiftMetatype t6, SwiftMetatype tr);

		[DllImport (SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.BlindSwiftClosureRepresentation_InvokeFunction8)]
		internal static extern void InvokeFunction (BlindSwiftClosureRepresentation clos, IntPtr retval, IntPtr args, SwiftMetatype t1, SwiftMetatype t2, SwiftMetatype t3, SwiftMetatype t4,
							    SwiftMetatype t5, SwiftMetatype t6, SwiftMetatype t7, SwiftMetatype tr);

		[DllImport (SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.BlindSwiftClosureRepresentation_InvokeFunction9)]
		internal static extern void InvokeFunction (BlindSwiftClosureRepresentation clos, IntPtr retval, IntPtr args, SwiftMetatype t1, SwiftMetatype t2, SwiftMetatype t3, SwiftMetatype t4,
							    SwiftMetatype t5, SwiftMetatype t6, SwiftMetatype t7, SwiftMetatype t8, SwiftMetatype tr);

		[DllImport (SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.BlindSwiftClosureRepresentation_InvokeFunction10)]
		internal static extern void InvokeFunction (BlindSwiftClosureRepresentation clos, IntPtr retval, IntPtr args, SwiftMetatype t1, SwiftMetatype t2, SwiftMetatype t3, SwiftMetatype t4,
		                                            SwiftMetatype t5, SwiftMetatype t6, SwiftMetatype t7, SwiftMetatype t8,
							    SwiftMetatype t9, SwiftMetatype tr);

		[DllImport (SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.BlindSwiftClosureRepresentation_InvokeFunction11)]
		internal static extern void InvokeFunction (BlindSwiftClosureRepresentation clos, IntPtr retval, IntPtr args, SwiftMetatype t1, SwiftMetatype t2, SwiftMetatype t3, SwiftMetatype t4,
		                                            SwiftMetatype t5, SwiftMetatype t6, SwiftMetatype t7, SwiftMetatype t8,
							    SwiftMetatype t9, SwiftMetatype t10, SwiftMetatype tr);

		[DllImport (SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.BlindSwiftClosureRepresentation_InvokeFunction12)]
		internal static extern void InvokeFunction (BlindSwiftClosureRepresentation clos, IntPtr retval, IntPtr args, SwiftMetatype t1, SwiftMetatype t2, SwiftMetatype t3, SwiftMetatype t4,
		                                            SwiftMetatype t5, SwiftMetatype t6, SwiftMetatype t7, SwiftMetatype t8,
							    SwiftMetatype t9, SwiftMetatype t10, SwiftMetatype t11, SwiftMetatype tr);

		[DllImport (SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.BlindSwiftClosureRepresentation_InvokeFunction13)]
		internal static extern void InvokeFunction (BlindSwiftClosureRepresentation clos, IntPtr retval, IntPtr args, SwiftMetatype t1, SwiftMetatype t2, SwiftMetatype t3, SwiftMetatype t4,
		                                            SwiftMetatype t5, SwiftMetatype t6, SwiftMetatype t7, SwiftMetatype t8,
							    SwiftMetatype t9, SwiftMetatype t10, SwiftMetatype t11, SwiftMetatype t12, SwiftMetatype tr);

		[DllImport (SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.BlindSwiftClosureRepresentation_InvokeFunction14)]
		internal static extern void InvokeFunction (BlindSwiftClosureRepresentation clos, IntPtr retval, IntPtr args, SwiftMetatype t1, SwiftMetatype t2, SwiftMetatype t3, SwiftMetatype t4,
		                                            SwiftMetatype t5, SwiftMetatype t6, SwiftMetatype t7, SwiftMetatype t8,
		                                            SwiftMetatype t9, SwiftMetatype t10, SwiftMetatype t11, SwiftMetatype t12,
							    SwiftMetatype t13, SwiftMetatype tr);

		[DllImport (SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.BlindSwiftClosureRepresentation_InvokeFunction15)]
		internal static extern void InvokeFunction (BlindSwiftClosureRepresentation clos, IntPtr retval, IntPtr args, SwiftMetatype t1, SwiftMetatype t2, SwiftMetatype t3, SwiftMetatype t4,
		                                            SwiftMetatype t5, SwiftMetatype t6, SwiftMetatype t7, SwiftMetatype t8,
		                                            SwiftMetatype t9, SwiftMetatype t10, SwiftMetatype t11, SwiftMetatype t12,
							    SwiftMetatype t13, SwiftMetatype t14, SwiftMetatype tr);

		[DllImport (SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.BlindSwiftClosureRepresentation_InvokeFunction16)]
		internal static extern void InvokeFunction (BlindSwiftClosureRepresentation clos, IntPtr retval, IntPtr args, SwiftMetatype t1, SwiftMetatype t2, SwiftMetatype t3, SwiftMetatype t4,
		                                            SwiftMetatype t5, SwiftMetatype t6, SwiftMetatype t7, SwiftMetatype t8,
		                                            SwiftMetatype t9, SwiftMetatype t10, SwiftMetatype t11, SwiftMetatype t12,
							    SwiftMetatype t13, SwiftMetatype t14, SwiftMetatype t15, SwiftMetatype tr);

		[DllImport (SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.BlindSwiftClosureRepresentation_InvokeFunction17)]
		internal static extern void InvokeFunction (BlindSwiftClosureRepresentation clos, IntPtr retval, IntPtr args, SwiftMetatype t1, SwiftMetatype t2, SwiftMetatype t3, SwiftMetatype t4,
		                                            SwiftMetatype t5, SwiftMetatype t6, SwiftMetatype t7, SwiftMetatype t8,
		                                            SwiftMetatype t9, SwiftMetatype t10, SwiftMetatype t11, SwiftMetatype t12,
							    SwiftMetatype t13, SwiftMetatype t14, SwiftMetatype t15, SwiftMetatype t16, SwiftMetatype tr);

		[DllImport (SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.BlindSwiftClosureRepresentation_InvokeFunctionThrows1)]
		internal static extern void InvokeFunctionThrows (BlindSwiftClosureRepresentation clos, IntPtr retval, SwiftMetatype tr);

		[DllImport (SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.BlindSwiftClosureRepresentation_InvokeFunctionThrows2)]
		internal static extern void InvokeFunctionThrows (BlindSwiftClosureRepresentation clos, IntPtr retval, IntPtr args, SwiftMetatype t1, SwiftMetatype tr);

		[DllImport (SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.BlindSwiftClosureRepresentation_InvokeFunctionThrows2)]
		internal static extern void InvokeFunctionThrows (BlindSwiftClosureRepresentation clos, IntPtr retval, IntPtr args, SwiftMetatype t1, SwiftMetatype t2, SwiftMetatype tr);

		[DllImport (SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.BlindSwiftClosureRepresentation_InvokeFunctionThrows4)]
		internal static extern void InvokeFunctionThrows (BlindSwiftClosureRepresentation clos, IntPtr retval, IntPtr args, SwiftMetatype t1, SwiftMetatype t2, SwiftMetatype t3, SwiftMetatype tr);

		[DllImport (SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.BlindSwiftClosureRepresentation_InvokeFunctionThrows5)]
		internal static extern void InvokeFunctionThrows (BlindSwiftClosureRepresentation clos, IntPtr retval, IntPtr args, SwiftMetatype t1, SwiftMetatype t2, SwiftMetatype t3, SwiftMetatype t4,
			SwiftMetatype tr);

		[DllImport (SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.BlindSwiftClosureRepresentation_InvokeFunctionThrows6)]
		internal static extern void InvokeFunctionThrows (BlindSwiftClosureRepresentation clos, IntPtr retval, IntPtr args, SwiftMetatype t1, SwiftMetatype t2, SwiftMetatype t3, SwiftMetatype t4,
			SwiftMetatype t5, SwiftMetatype tr);

		[DllImport (SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.BlindSwiftClosureRepresentation_InvokeFunctionThrows7)]
		internal static extern void InvokeFunctionThrows (BlindSwiftClosureRepresentation clos, IntPtr retval, IntPtr args, SwiftMetatype t1, SwiftMetatype t2, SwiftMetatype t3, SwiftMetatype t4,
			SwiftMetatype t5, SwiftMetatype t6, SwiftMetatype tr);

		[DllImport (SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.BlindSwiftClosureRepresentation_InvokeFunctionThrows8)]
		internal static extern void InvokeFunctionThrows (BlindSwiftClosureRepresentation clos, IntPtr retval, IntPtr args, SwiftMetatype t1, SwiftMetatype t2, SwiftMetatype t3, SwiftMetatype t4,
			SwiftMetatype t5, SwiftMetatype t6, SwiftMetatype t7, SwiftMetatype tr);

		[DllImport (SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.BlindSwiftClosureRepresentation_InvokeFunctionThrows9)]
		internal static extern void InvokeFunctionThrows (BlindSwiftClosureRepresentation clos, IntPtr retval, IntPtr args, SwiftMetatype t1, SwiftMetatype t2, SwiftMetatype t3, SwiftMetatype t4,
			SwiftMetatype t5, SwiftMetatype t6, SwiftMetatype t7, SwiftMetatype t8, SwiftMetatype tr);

		[DllImport (SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.BlindSwiftClosureRepresentation_InvokeFunctionThrows10)]
		internal static extern void InvokeFunctionThrows (BlindSwiftClosureRepresentation clos, IntPtr retval, IntPtr args, SwiftMetatype t1, SwiftMetatype t2, SwiftMetatype t3, SwiftMetatype t4,
			SwiftMetatype t5, SwiftMetatype t6, SwiftMetatype t7, SwiftMetatype t8, SwiftMetatype t9, SwiftMetatype tr);

		[DllImport (SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.BlindSwiftClosureRepresentation_InvokeFunctionThrows11)]
		internal static extern void InvokeFunctionThrows (BlindSwiftClosureRepresentation clos, IntPtr retval, IntPtr args, SwiftMetatype t1, SwiftMetatype t2, SwiftMetatype t3, SwiftMetatype t4,
			SwiftMetatype t5, SwiftMetatype t6, SwiftMetatype t7, SwiftMetatype t8, SwiftMetatype t9, SwiftMetatype t10, SwiftMetatype tr);

		[DllImport (SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.BlindSwiftClosureRepresentation_InvokeFunctionThrows12)]
		internal static extern void InvokeFunctionThrows (BlindSwiftClosureRepresentation clos, IntPtr retval, IntPtr args, SwiftMetatype t1, SwiftMetatype t2, SwiftMetatype t3, SwiftMetatype t4,
			SwiftMetatype t5, SwiftMetatype t6, SwiftMetatype t7, SwiftMetatype t8, SwiftMetatype t9, SwiftMetatype t10, SwiftMetatype t11, SwiftMetatype tr);

		[DllImport (SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.BlindSwiftClosureRepresentation_InvokeFunctionThrows13)]
		internal static extern void InvokeFunctionThrows (BlindSwiftClosureRepresentation clos, IntPtr retval, IntPtr args, SwiftMetatype t1, SwiftMetatype t2, SwiftMetatype t3, SwiftMetatype t4,
			SwiftMetatype t5, SwiftMetatype t6, SwiftMetatype t7, SwiftMetatype t8, SwiftMetatype t9, SwiftMetatype t10, SwiftMetatype t11,
			SwiftMetatype t12, SwiftMetatype tr);

		[DllImport (SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.BlindSwiftClosureRepresentation_InvokeFunctionThrows14)]
		internal static extern void InvokeFunctionThrows (BlindSwiftClosureRepresentation clos, IntPtr retval, IntPtr args, SwiftMetatype t1, SwiftMetatype t2, SwiftMetatype t3, SwiftMetatype t4,
			SwiftMetatype t5, SwiftMetatype t6, SwiftMetatype t7, SwiftMetatype t8, SwiftMetatype t9, SwiftMetatype t10, SwiftMetatype t11,
			SwiftMetatype t12, SwiftMetatype t13, SwiftMetatype tr);

		[DllImport (SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.BlindSwiftClosureRepresentation_InvokeFunctionThrows15)]
		internal static extern void InvokeFunctionThrows (BlindSwiftClosureRepresentation clos, IntPtr retval, IntPtr args, SwiftMetatype t1, SwiftMetatype t2, SwiftMetatype t3, SwiftMetatype t4,
			SwiftMetatype t5, SwiftMetatype t6, SwiftMetatype t7, SwiftMetatype t8, SwiftMetatype t9, SwiftMetatype t10, SwiftMetatype t11,
			SwiftMetatype t12, SwiftMetatype t13, SwiftMetatype t14, SwiftMetatype tr);

		[DllImport (SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.BlindSwiftClosureRepresentation_InvokeFunctionThrows16)]
		internal static extern void InvokeFunctionThrows (BlindSwiftClosureRepresentation clos, IntPtr retval, IntPtr args, SwiftMetatype t1, SwiftMetatype t2, SwiftMetatype t3, SwiftMetatype t4,
			SwiftMetatype t5, SwiftMetatype t6, SwiftMetatype t7, SwiftMetatype t8, SwiftMetatype t9, SwiftMetatype t10, SwiftMetatype t11,
			SwiftMetatype t12, SwiftMetatype t13, SwiftMetatype t14, SwiftMetatype t15, SwiftMetatype tr);

		[DllImport (SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.BlindSwiftClosureRepresentation_InvokeFunctionThrows17)]
		internal static extern void InvokeFunctionThrows (BlindSwiftClosureRepresentation clos, IntPtr retval, IntPtr args, SwiftMetatype t1, SwiftMetatype t2, SwiftMetatype t3, SwiftMetatype t4,
			SwiftMetatype t5, SwiftMetatype t6, SwiftMetatype t7, SwiftMetatype t8, SwiftMetatype t9, SwiftMetatype t10, SwiftMetatype t11,
			SwiftMetatype t12, SwiftMetatype t13, SwiftMetatype t14, SwiftMetatype t15, SwiftMetatype t16, SwiftMetatype tr);
	}
}
