# 'all' is the default target, and must always come first
all:

MANUAL_BINDER_FINDER_LIBRARIES= \
	libswiftCore \
	libswiftCoreFoundation \
	libswiftCoreGraphics \
	libswiftDarwin \
	libswift_Differentiation \
	libswiftDifferentiationUnittest \
	libswiftFoundation \
	libswiftObjectiveC \
	libswiftOSLogTestHelper \
	libswiftRemoteMirror \
	libswiftRuntimeUnittest \
	libswiftStdlibUnittest \
	libswiftStdlibUnittestFoundationExtras \
	libswiftSwiftOnoneSupport \
	libswiftSwiftPrivate \
	libswiftSwiftPrivateLibcExtras \
	libswiftSwiftPrivateThreadExtras \
	libswiftSwiftReflectionTest \
	libswiftXCTest \

# There is an error with libswiftDispatch:
# System.NotImplementedException: At least one needs to be a thunk - should never happen
