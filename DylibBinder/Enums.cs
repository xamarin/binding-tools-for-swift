using System;
namespace DylibBinder {
	internal enum TypeAccessibility {
		Public,
		Private,
		Internal,
		Open,
	}

	internal enum Storage {
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

	[Flags]
	internal enum ParameterOptions {
		None = 0,
		IsVariadic = 1 << 0,
		HasInstance = 1 << 1,
		IsConstructor = 1 << 2,
		IsEmptyParameter = 1 << 3,
	}
}
