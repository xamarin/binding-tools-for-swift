//#define CRASH_ON_EXCEPTION

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using ObjCRuntime;

namespace SwiftReflector {
	public class ErrorHandling {
		List<ReflectorError> messages;

		public ErrorHandling ()
		{
			messages = new List<ReflectorError> ();
			SkippedTypes = new List<string> ();
			SkippedFunctions = new List<string> ();
		}

		public IEnumerable<ReflectorError> Messages {
			get { return messages; }
		}

		public IEnumerable<ReflectorError> Errors {
			get { return messages.Where ((v) => !v.IsWarning); }
		}

		public IEnumerable<ReflectorError> Warnings {
			get { return messages.Where ((v) => v.IsWarning); }
		}

		public List<string> SkippedTypes { get; private set; }
		public List<string> SkippedFunctions { get; private set; }

		public void Add (ErrorHandling eh)
		{
			messages.AddRange (eh.messages);
		}

		public void Add (params ReflectorError [] errors)
		{
			messages.AddRange (errors);
		}

		public void Add (Exception exception)
		{
#if CRASH_ON_EXCEPTION
			ExceptionDispatchInfo.Capture (exception).Throw ();
#else
			messages.Add (new ReflectorError (exception));
#endif
		}

		public bool AnyMessages {
			get { return messages.Count > 0; }
		}

		public bool AnyErrors {
			get { return messages.Any ((v) => !v.IsWarning);  }
		}

		public int WarningCount {
			get { return messages.Count ((v) => v.IsWarning); }
		}

		public int ErrorCount {
			get { return messages.Count ((v) => !v.IsWarning); }
		}

		public int Show (int verbosity)
		{
			ErrorHelper.Verbosity = verbosity;
			return ErrorHelper.Show (messages.Select ((v) => v.Exception));
		}
	}
}
