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

void swiftAsmArg0 ()
{
#if __x86_64
	__asm("mov %rdi, %rax");
#elif __arm64
	__asm("mov x0, x0");
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

void swiftAsmArg1 ()
{
#if __x86_64
	__asm("mov %rsi, %rax");
#elif __arm64
	__asm("mov x0, x1");
#elif __arm
	__asm("mov r0, r1");
#elif i386
	asm {
		mov eax, dword ptr [ebp + 16]
	}
#else
#error("Unknown CPU type for register access");
#endif
}

void swiftAsmArg2 ()
{
#if __x86_64
	__asm("mov %rdx, %rax");
#elif __arm64
	__asm("mov x0, x2");
#elif __arm
	__asm("mov r0, r2");
#elif i386
	asm {
		mov eax, dword ptr [ebp + 20]
	}
#else
#error("Unknown CPU type for register access");
#endif
}

void swiftAsmArg3 ()
{
#if __x86_64
	__asm("mov %rcx, %rax");
#elif __arm64
	__asm("mov x0, x3");
#elif __arm
	__asm("mov r0, r3");
#elif i386
	asm {
		mov eax, dword ptr [ebp + 24]
	}
#else
#error("Unknown CPU type for register access");
#endif
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
