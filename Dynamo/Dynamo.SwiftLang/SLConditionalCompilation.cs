using System;

namespace Dynamo.SwiftLang {
	public class SLConditionalCompilation : LineCodeElementCollection<ICodeElement> {
		SLConditionalCompilation (SLIdentifier tag, SLIdentifier condition)
			: base (true, false, false)
		{
			Add (tag);
			if (condition != null) {
				Add (SimpleElememt.Spacer);
				Add (condition);
			}
		}

		public SLConditionalCompilation AttachBefore (ICodeElement thing)
		{
			Exceptions.ThrowOnNull (thing, nameof (thing));
			thing.Begin += (s, eventArgs) => {
				this.WriteAll (eventArgs.Writer);
			};
			return this;
		}

		public SLConditionalCompilation AttachAfter (ICodeElement thing)
		{
			Exceptions.ThrowOnNull (thing, nameof (thing));
			thing.End += (s, eventArgs) => {
				this.WriteAll (eventArgs.Writer);
			};
			return this;
		}



		static SLConditionalCompilation _else = new SLConditionalCompilation (new SLIdentifier ("#else"), null);
		public static SLConditionalCompilation Else { get { return _else; } }
		static SLConditionalCompilation _endif = new SLConditionalCompilation (new SLIdentifier ("#endif"), null);
		public static SLConditionalCompilation Endif {  get { return _endif; } }

		public static SLConditionalCompilation If (SLIdentifier condition)
		{
			return new SLConditionalCompilation (new SLIdentifier ("#if"), Exceptions.ThrowOnNull (condition, nameof (condition)));
		}
	}
}
