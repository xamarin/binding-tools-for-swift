using System;
using System.Runtime.InteropServices;
using SwiftRuntimeLibrary;

namespace SwiftRuntimeLibrary.SwiftMarshal {
	internal class BlindClosureMapper {
		[DllImport (SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.BlindClosureMapper_ActionMapA)]
		public static extern BlindSwiftClosureRepresentation ActionMap (BlindSwiftClosureRepresentation clos,
		                                                                SwiftMetatype mt1);

		[DllImport (SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.BlindClosureMapper_ActionMapAB)]
		public static extern BlindSwiftClosureRepresentation ActionMap (BlindSwiftClosureRepresentation clos,
		                                                                SwiftMetatype mt1, SwiftMetatype mt2);

		[DllImport (SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.BlindClosureMapper_ActionMapABC)]
		public static extern BlindSwiftClosureRepresentation ActionMap (BlindSwiftClosureRepresentation clos,
		                                                                SwiftMetatype mt1, SwiftMetatype mt2, SwiftMetatype mt3);

		[DllImport (SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.BlindClosureMapper_ActionMapABCD)]
		public static extern BlindSwiftClosureRepresentation ActionMap (BlindSwiftClosureRepresentation clos,
																		   SwiftMetatype mt1, SwiftMetatype mt2, SwiftMetatype mt3,
																		   SwiftMetatype mt4);

		[DllImport (SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.BlindClosureMapper_ActionMapABCDE)]
		public static extern BlindSwiftClosureRepresentation ActionMap (BlindSwiftClosureRepresentation clos,
		                                                                SwiftMetatype mt1, SwiftMetatype mt2, SwiftMetatype mt3,
		                                                                SwiftMetatype mt4, SwiftMetatype mt5);

		[DllImport (SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.BlindClosureMapper_ActionMapABCDEF)]
		public static extern BlindSwiftClosureRepresentation ActionMap (BlindSwiftClosureRepresentation clos,
		                                                                SwiftMetatype mt1, SwiftMetatype mt2, SwiftMetatype mt3,
		                                                                SwiftMetatype mt4, SwiftMetatype mt5, SwiftMetatype mt6);

		[DllImport (SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.BlindClosureMapper_ActionMapABCDEFG)]
		public static extern BlindSwiftClosureRepresentation ActionMap (BlindSwiftClosureRepresentation clos,
		                                                                SwiftMetatype mt1, SwiftMetatype mt2, SwiftMetatype mt3,
		                                                                SwiftMetatype mt4, SwiftMetatype mt5, SwiftMetatype mt6,
		                                                                SwiftMetatype mt7);

		[DllImport (SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.BlindClosureMapper_ActionMapABCDEFGH)]
		public static extern BlindSwiftClosureRepresentation ActionMap (BlindSwiftClosureRepresentation clos,
		                                                                SwiftMetatype mt1, SwiftMetatype mt2, SwiftMetatype mt3,
		                                                                SwiftMetatype mt4, SwiftMetatype mt5, SwiftMetatype mt6,
		                                                                SwiftMetatype mt7, SwiftMetatype mt8);

		[DllImport (SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.BlindClosureMapper_ActionMapABCDEFGHI)]
		public static extern BlindSwiftClosureRepresentation ActionMap (BlindSwiftClosureRepresentation clos,
		                                                                SwiftMetatype mt1, SwiftMetatype mt2, SwiftMetatype mt3,
		                                                                SwiftMetatype mt4, SwiftMetatype mt5, SwiftMetatype mt6,
		                                                                SwiftMetatype mt7, SwiftMetatype mt8, SwiftMetatype mt9);

		[DllImport (SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.BlindClosureMapper_ActionMapABCDEFGHIJ)]
		public static extern BlindSwiftClosureRepresentation ActionMap (BlindSwiftClosureRepresentation clos,
		                                                                SwiftMetatype mt1, SwiftMetatype mt2, SwiftMetatype mt3,
		                                                                SwiftMetatype mt4, SwiftMetatype mt5, SwiftMetatype mt6,
		                                                                SwiftMetatype mt7, SwiftMetatype mt8, SwiftMetatype mt9,
		                                                                SwiftMetatype mt10);

		[DllImport (SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.BlindClosureMapper_ActionMapABCDEFGHIJK)]
		public static extern BlindSwiftClosureRepresentation ActionMap (BlindSwiftClosureRepresentation clos,
		                                                                SwiftMetatype mt1, SwiftMetatype mt2, SwiftMetatype mt3,
		                                                                SwiftMetatype mt4, SwiftMetatype mt5, SwiftMetatype mt6,
		                                                                SwiftMetatype mt7, SwiftMetatype mt8, SwiftMetatype mt9,
		                                                                SwiftMetatype mt10, SwiftMetatype mt11);

		[DllImport (SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.BlindClosureMapper_ActionMapABCDEFGHIJKL)]
		public static extern BlindSwiftClosureRepresentation ActionMap (BlindSwiftClosureRepresentation clos,
		                                                                SwiftMetatype mt1, SwiftMetatype mt2, SwiftMetatype mt3,
		                                                                SwiftMetatype mt4, SwiftMetatype mt5, SwiftMetatype mt6,
		                                                                SwiftMetatype mt7, SwiftMetatype mt8, SwiftMetatype mt9,
		                                                                SwiftMetatype mt10, SwiftMetatype mt11, SwiftMetatype mt12);

		[DllImport (SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.BlindClosureMapper_ActionMapABCDEFGHIJKLM)]
		public static extern BlindSwiftClosureRepresentation ActionMap (BlindSwiftClosureRepresentation clos,
		                                                                SwiftMetatype mt1, SwiftMetatype mt2, SwiftMetatype mt3,
		                                                                SwiftMetatype mt4, SwiftMetatype mt5, SwiftMetatype mt6,
		                                                                SwiftMetatype mt7, SwiftMetatype mt8, SwiftMetatype mt9,
		                                                                SwiftMetatype mt10, SwiftMetatype mt11, SwiftMetatype mt12,
		                                                                SwiftMetatype mt13);

		[DllImport (SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.BlindClosureMapper_ActionMapABCDEFGHIJKLMN)]
		public static extern BlindSwiftClosureRepresentation ActionMap (BlindSwiftClosureRepresentation clos,
		                                                                SwiftMetatype mt1, SwiftMetatype mt2, SwiftMetatype mt3,
		                                                                SwiftMetatype mt4, SwiftMetatype mt5, SwiftMetatype mt6,
		                                                                SwiftMetatype mt7, SwiftMetatype mt8, SwiftMetatype mt9,
		                                                                SwiftMetatype mt10, SwiftMetatype mt11, SwiftMetatype mt12,
		                                                                SwiftMetatype mt13, SwiftMetatype mt14);

		[DllImport (SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.BlindClosureMapper_ActionMapABCDEFGHIJKLMNO)]
		public static extern BlindSwiftClosureRepresentation ActionMap (BlindSwiftClosureRepresentation clos,
		                                                                SwiftMetatype mt1, SwiftMetatype mt2, SwiftMetatype mt3,
		                                                                SwiftMetatype mt4, SwiftMetatype mt5, SwiftMetatype mt6,
		                                                                SwiftMetatype mt7, SwiftMetatype mt8, SwiftMetatype mt9,
		                                                                SwiftMetatype mt10, SwiftMetatype mt11, SwiftMetatype mt12,
		                                                                SwiftMetatype mt13, SwiftMetatype mt14, SwiftMetatype mt15);

		[DllImport (SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.BlindClosureMapper_ActionMapABCDEFGHIJKLMNOP)]
		public static extern BlindSwiftClosureRepresentation ActionMap (BlindSwiftClosureRepresentation clos,
		                                                                SwiftMetatype mt1, SwiftMetatype mt2, SwiftMetatype mt3,
		                                                                SwiftMetatype mt4, SwiftMetatype mt5, SwiftMetatype mt6,
		                                                                SwiftMetatype mt7, SwiftMetatype mt8, SwiftMetatype mt9,
		                                                                SwiftMetatype mt10, SwiftMetatype mt11, SwiftMetatype mt12,
		                                                                SwiftMetatype mt13, SwiftMetatype mt14, SwiftMetatype mt15,
		                                                                SwiftMetatype mt16);

		[DllImport (SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.BlindClosureMapper_FuncMapA)]
		public static extern BlindSwiftClosureRepresentation FuncMap (BlindSwiftClosureRepresentation clos,
		                                                              SwiftMetatype mtr);

		[DllImport (SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.BlindClosureMapper_FuncMapAB)]
		public static extern BlindSwiftClosureRepresentation FuncMap (BlindSwiftClosureRepresentation clos,
		                                                              SwiftMetatype mt1,
		                                                              SwiftMetatype mtr);

		[DllImport (SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.BlindClosureMapper_FuncMapABC)]
		public static extern BlindSwiftClosureRepresentation FuncMap (BlindSwiftClosureRepresentation clos,
		                                                              SwiftMetatype mt1, SwiftMetatype mt2,
		                                                              SwiftMetatype mtr);

		[DllImport (SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.BlindClosureMapper_FuncMapABCD)]
		public static extern BlindSwiftClosureRepresentation FuncMap (BlindSwiftClosureRepresentation clos,
		                                                              SwiftMetatype mt1, SwiftMetatype mt2, SwiftMetatype mt3,
		                                                              SwiftMetatype mtr);

		[DllImport (SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.BlindClosureMapper_FuncMapABCDE)]
		public static extern BlindSwiftClosureRepresentation FuncMap (BlindSwiftClosureRepresentation clos,
		                                                              SwiftMetatype mt1, SwiftMetatype mt2, SwiftMetatype mt3,
		                                                              SwiftMetatype mt4,
		                                                              SwiftMetatype mtr);

		[DllImport (SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.BlindClosureMapper_FuncMapABCDEF)]
		public static extern BlindSwiftClosureRepresentation FuncMap (BlindSwiftClosureRepresentation clos,
		                                                              SwiftMetatype mt1, SwiftMetatype mt2, SwiftMetatype mt3,
		                                                              SwiftMetatype mt4, SwiftMetatype mt5,
		                                                              SwiftMetatype mtr);

		[DllImport (SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.BlindClosureMapper_FuncMapABCDEFG)]
		public static extern BlindSwiftClosureRepresentation FuncMap (BlindSwiftClosureRepresentation clos,
		                                                              SwiftMetatype mt1, SwiftMetatype mt2, SwiftMetatype mt3,
		                                                              SwiftMetatype mt4, SwiftMetatype mt5, SwiftMetatype mt6,
		                                                              SwiftMetatype mtr);

		[DllImport (SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.BlindClosureMapper_FuncMapABCDEFGH)]
		public static extern BlindSwiftClosureRepresentation FuncMap (BlindSwiftClosureRepresentation clos,
		                                                              SwiftMetatype mt1, SwiftMetatype mt2, SwiftMetatype mt3,
		                                                              SwiftMetatype mt4, SwiftMetatype mt5, SwiftMetatype mt6,
		                                                              SwiftMetatype mt7,
		                                                              SwiftMetatype mtr);

		[DllImport (SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.BlindClosureMapper_FuncMapABCDEFGHI)]
		public static extern BlindSwiftClosureRepresentation FuncMap (BlindSwiftClosureRepresentation clos,
		                                                              SwiftMetatype mt1, SwiftMetatype mt2, SwiftMetatype mt3,
		                                                              SwiftMetatype mt4, SwiftMetatype mt5, SwiftMetatype mt6,
		                                                              SwiftMetatype mt7, SwiftMetatype mt8,
		                                                              SwiftMetatype mtr);

		[DllImport (SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.BlindClosureMapper_FuncMapABCDEFGHIJ)]
		public static extern BlindSwiftClosureRepresentation FuncMap (BlindSwiftClosureRepresentation clos,
		                                                              SwiftMetatype mt1, SwiftMetatype mt2, SwiftMetatype mt3,
		                                                              SwiftMetatype mt4, SwiftMetatype mt5, SwiftMetatype mt6,
		                                                              SwiftMetatype mt7, SwiftMetatype mt8, SwiftMetatype mt9,
		                                                              SwiftMetatype mtr);

		[DllImport (SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.BlindClosureMapper_FuncMapABCDEFGHIJK)]
		public static extern BlindSwiftClosureRepresentation FuncMap (BlindSwiftClosureRepresentation clos,
		                                                              SwiftMetatype mt1, SwiftMetatype mt2, SwiftMetatype mt3,
		                                                              SwiftMetatype mt4, SwiftMetatype mt5, SwiftMetatype mt6,
		                                                              SwiftMetatype mt7, SwiftMetatype mt8, SwiftMetatype mt9,
		                                                              SwiftMetatype mt10,
		                                                              SwiftMetatype mtr);

		[DllImport (SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.BlindClosureMapper_FuncMapABCDEFGHIJKL)]
		public static extern BlindSwiftClosureRepresentation FuncMap (BlindSwiftClosureRepresentation clos,
		                                                              SwiftMetatype mt1, SwiftMetatype mt2, SwiftMetatype mt3,
		                                                              SwiftMetatype mt4, SwiftMetatype mt5, SwiftMetatype mt6,
		                                                              SwiftMetatype mt7, SwiftMetatype mt8, SwiftMetatype mt9,
		                                                              SwiftMetatype mt10, SwiftMetatype mt11,
		                                                              SwiftMetatype mtr);

		[DllImport (SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.BlindClosureMapper_FuncMapABCDEFGHIJKLM)]
		public static extern BlindSwiftClosureRepresentation FuncMap (BlindSwiftClosureRepresentation clos,
		                                                              SwiftMetatype mt1, SwiftMetatype mt2, SwiftMetatype mt3,
		                                                              SwiftMetatype mt4, SwiftMetatype mt5, SwiftMetatype mt6,
		                                                              SwiftMetatype mt7, SwiftMetatype mt8, SwiftMetatype mt9,
		                                                              SwiftMetatype mt10, SwiftMetatype mt11, SwiftMetatype mt12,
		                                                              SwiftMetatype mtr);

		[DllImport (SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.BlindClosureMapper_FuncMapABCDEFGHIJKLMN)]
		public static extern BlindSwiftClosureRepresentation FuncMap (BlindSwiftClosureRepresentation clos,
		                                                              SwiftMetatype mt1, SwiftMetatype mt2, SwiftMetatype mt3,
		                                                              SwiftMetatype mt4, SwiftMetatype mt5, SwiftMetatype mt6,
		                                                              SwiftMetatype mt7, SwiftMetatype mt8, SwiftMetatype mt9,
		                                                              SwiftMetatype mt10, SwiftMetatype mt11, SwiftMetatype mt12,
		                                                              SwiftMetatype mt13,
		                                                              SwiftMetatype mtr);

		[DllImport (SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.BlindClosureMapper_FuncMapABCDEFGHIJKLMNO)]
		public static extern BlindSwiftClosureRepresentation FuncMap (BlindSwiftClosureRepresentation clos,
		                                                              SwiftMetatype mt1, SwiftMetatype mt2, SwiftMetatype mt3,
		                                                              SwiftMetatype mt4, SwiftMetatype mt5, SwiftMetatype mt6,
		                                                              SwiftMetatype mt7, SwiftMetatype mt8, SwiftMetatype mt9,
		                                                              SwiftMetatype mt10, SwiftMetatype mt11, SwiftMetatype mt12,
		                                                              SwiftMetatype mt13, SwiftMetatype mt14,
		                                                              SwiftMetatype mtr);

		[DllImport (SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.BlindClosureMapper_FuncMapABCDEFGHIJKLMNOP)]
		public static extern BlindSwiftClosureRepresentation FuncMap (BlindSwiftClosureRepresentation clos,
		                                                              SwiftMetatype mt1, SwiftMetatype mt2, SwiftMetatype mt3,
		                                                              SwiftMetatype mt4, SwiftMetatype mt5, SwiftMetatype mt6,
		                                                              SwiftMetatype mt7, SwiftMetatype mt8, SwiftMetatype mt9,
		                                                              SwiftMetatype mt10, SwiftMetatype mt11, SwiftMetatype mt12,
		                                                              SwiftMetatype mt13, SwiftMetatype mt14, SwiftMetatype mt15,
		                                                              SwiftMetatype mtr);

		[DllImport (SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.BlindClosureMapper_FuncMapABCDEFGHIJKLMNOPQ)]
		public static extern BlindSwiftClosureRepresentation FuncMap (BlindSwiftClosureRepresentation clos,
		                                                              SwiftMetatype mt1, SwiftMetatype mt2, SwiftMetatype mt3,
		                                                              SwiftMetatype mt4, SwiftMetatype mt5, SwiftMetatype mt6,
		                                                              SwiftMetatype mt7, SwiftMetatype mt8, SwiftMetatype mt9,
		                                                              SwiftMetatype mt10, SwiftMetatype mt11, SwiftMetatype mt12,
		                                                              SwiftMetatype mt13, SwiftMetatype mt14, SwiftMetatype mt15,
		                                                              SwiftMetatype mt16,
		                                                              SwiftMetatype mtr);


#pragma warning disable 0649 // Field 'X' is never assigned to, and will always have its default value
		struct ActionVoidRep {
			[MarshalAs (UnmanagedType.FunctionPtr)]
			public Action<IntPtr> Func;
			public IntPtr Data;
		}

		struct ActionRep {
			[MarshalAs (UnmanagedType.FunctionPtr)]
			public Action<IntPtr, IntPtr> Func;
			public IntPtr Data;
		}

		public struct FuncVoidRep {
			[MarshalAs (UnmanagedType.FunctionPtr)]
			public Action<IntPtr, IntPtr> Func;
			public IntPtr Data;
		}

		public struct FuncRep {
			[MarshalAs (UnmanagedType.FunctionPtr)]
			public Action<IntPtr, IntPtr, IntPtr> Func;
			public IntPtr Data;
		}
	}
#pragma warning restore 0649
}
