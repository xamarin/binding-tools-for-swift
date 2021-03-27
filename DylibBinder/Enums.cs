using System;
namespace DylibBinder {
	public enum TypeAccessibility {
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
		Stored,
		StoredWithObservers,
		StoredWithTrivialAccessors,
		Coroutine,
		MutableAddressor,
	}
}
