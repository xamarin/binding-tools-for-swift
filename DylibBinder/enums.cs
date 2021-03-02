using System;
namespace DylibBinder {
	public class enums {
		// TODO Currently not using these (except Accessibility), but would
		// be good to use it when I write XML with XElements
		public enum Accessibility {
			Public,
			Private,
			Internal,
			Open,
		}

		public enum Storage {
			Addressed,
			AddressedWithObservers,
			AddressedWithTrivialAccessors,
			Computed,
			ComputedWithMutableAddress,
			Inherited,
			InheritedWithObservers,
			Stored, StoredWithObservers,
			StoredWithTrivialAccessors,
			Coroutine,
			MutableAddressor,
		}

		public enum OperatorKind {
			None,
			Prefix,
			Postfix,
			Infix,
		}
	}
}
