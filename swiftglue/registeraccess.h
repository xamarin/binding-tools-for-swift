// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

//
//  registeraccess.h
//

#ifndef CGLUE_EXPORT
#if defined(__cplusplus)
#define CGLUE_EXPORT extern "C"
#else
#define CGLUE_EXPORT extern
#endif
#endif

CGLUE_EXPORT const void * swiftAsmArg0 ();
CGLUE_EXPORT const void * swiftAsmArg1 ();
CGLUE_EXPORT const void * swiftAsmArg2 ();
CGLUE_EXPORT const void * swiftAsmArg3 ();
CGLUE_EXPORT const void * swiftAsmArg4 ();
CGLUE_EXPORT const void * swiftAsmArg5 ();
CGLUE_EXPORT const void * swiftSelfArg ();
//#import <registeraccess.h>

