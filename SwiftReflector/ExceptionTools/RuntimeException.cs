// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

// Copyright 2011-2012, Xamarin Inc. All rights reserved,

using System;
using System.Collections.Generic;

#if XAMCORE_2_0
namespace ObjCRuntime {
#endif
#if BINDING_TOOLS_FOR_SWIFT
namespace SwiftReflector.ExceptionTools {
#endif

	public class RuntimeException : Exception {
		// Store the stack trace when this exception was created, and return
		// it if the base class doesn't have a stack trace (which would happen
		// if this exception was never thrown).
		string stack_trace;

		public RuntimeException (string message, params object [] args)
			: base (string.Format (message, args))
		{
			stack_trace = new System.Diagnostics.StackTrace (true).ToString ();
		}

		public RuntimeException (int code, string message, params object [] args) :
			this (code, false, message, args)
		{
		}

		public RuntimeException (int code, bool error, string message, params object [] args) :
			this (code, error, null, message, args)
		{
		}

		public RuntimeException (int code, bool error, Exception innerException, string message, params object [] args) :
			base (String.Format (message, args), innerException)
		{
			stack_trace = new System.Diagnostics.StackTrace (true).ToString ();
			Code = code;
			Error = error;
		}

		public int Code { get; private set; }

		public bool Error { get; private set; }

		// http://blogs.msdn.com/b/msbuild/archive/2006/11/03/msbuild-visual-studio-aware-error-messages-and-message-formats.aspx
		public override string ToString ()
		{
			return String.Format ("{0} {3}{1:0000}: {2}",
			                      Error ? "error" : "warning", Code, Message, ObjCRuntime.ErrorHelper.Prefix);
		}

		public override string StackTrace {
			get {
				var thrownTrace = base.StackTrace;
				if (string.IsNullOrEmpty (thrownTrace))
					return stack_trace;
				return thrownTrace;
			}
		}
	}
#if XAMCORE_2_0
}
#endif
#if BINDING_TOOLS_FOR_SWIFT
}
#endif
