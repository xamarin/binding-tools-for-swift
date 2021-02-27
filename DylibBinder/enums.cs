using System;
namespace DylibBinder {
	public class enums {
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
