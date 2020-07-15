// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

// These are accessors for each supported CPU to get the register arguments.
// These are used because swift puts implicit arguments in registers including
// type metadata and protocol witness tables.

// supported CPUs:
// __x86_64
// __arm64
// __arm
// i386
// we could probably further distinguish armv7, armv7k, and armv7s, but
// I don't imagine that we'll need the extensions since all we're doing is
// register juggling

// Yes, the signature here doesn't match the declaration in registeraccess.h
// This is a feature, not a bug.
void *swiftAsmArg0 (void *arg0)
{
	return arg0;
}

void *swiftAsmArg1 (void *arg0, void *arg1)
{
	return arg1;
}

void *swiftAsmArg2 (void *arg0, void *arg1, void *arg2)
{
	return arg2;
}

void *swiftAsmArg3 (void *arg0, void *arg1, void *arg2, void *arg3)
{
	return arg3;
}

void swiftSelfArg ()
{
#if __x86_64
	__asm("mov %r13, %rax");
#elif __arm64
	__asm("mov x0, x20");
#elif __arm
	__asm("mov r0, r0");
#elif i386
	asm {
		mov eax, dword ptr [ebp + 12]
	}
#else
#error("Unknown CPU type for register access");
#endif
}
