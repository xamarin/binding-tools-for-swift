// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;
using SwiftRuntimeLibrary.SwiftMarshal;
using System.Collections.Generic;
#if __IOS__ || __MACOS__ || __TVOS__ || __WATCHOS__
using ObjCRuntime;
#endif
using System.Reflection;

namespace SwiftRuntimeLibrary {
        public class SwiftIteratorProtocolProtocol<ATElement> :
            BaseAssociatedTypeProxy, ISwiftIteratorProtocol<ATElement> {

                static SwiftIteratorProtocol_xam_vtable xamVtableISwiftIteratorProtocol;
                static IntPtr protocolWitnessTable;
                ISwiftIteratorProtocol<ATElement> xamarinImpl;

                static SwiftIteratorProtocolProtocol ()
                {
                        XamSetVTable ();
                }

                static IntPtr _XamSwiftIteratorProtocolProtocolCtorImpl ()
                {
                        IntPtr retvalIntPtr = IntPtr.Zero;
                        retvalIntPtr = NativeMethodsForSwiftIteratorProtocolProtocol.PI_SwiftIteratorProtocolProtocol (StructMarshal.Marshaler.Metatypeof (typeof (ATElement)));
                        return retvalIntPtr;
                }

                public SwiftIteratorProtocolProtocol () : base (SwiftIteratorProtocolProtocol<ATElement>._XamSwiftIteratorProtocolProtocolCtorImpl (),
                        GetSwiftMetatype (), SwiftObjectRegistry.Registry)
                {
                }

                protected SwiftIteratorProtocolProtocol (IntPtr handle,
                        SwiftMetatype mt, SwiftObjectRegistry registry) : base (handle, mt, registry)
                {
                }

                public static object XamarinFactory (IntPtr p, Type [] genericTypes)
                {
                        Type t = typeof (SwiftIteratorProtocolProtocol<>).MakeGenericType (genericTypes);

                        ConstructorInfo ci = t.GetConstructor (BindingFlags.Instance | BindingFlags.NonPublic, null,
                                new Type [] { typeof(IntPtr), typeof(SwiftObjectRegistry)}, null);
                        return ci.Invoke (new object [] { p, SwiftObjectRegistry.Registry });
                }

                public static SwiftMetatype GetSwiftMetatype ()
                {
                        return NativeMethodsForSwiftIteratorProtocolProtocol.PIMetadataAccessor_SwiftIteratorProtocolProtocol (SwiftMetadataRequest.Complete, StructMarshal.Marshaler.Metatypeof (typeof (ATElement)));
                }

                public SwiftIteratorProtocolProtocol (ISwiftIteratorProtocol<ATElement> actualImplementation) : this ()
                {
                        xamarinImpl = actualImplementation;
                }

                public SwiftOptional<ATElement> Next ()
                {
                        if (xamarinImpl != null) {
                                return xamarinImpl.Next ();
                        } else {
                                unsafe {
                                        SwiftOptional<ATElement> retval = new SwiftOptional<ATElement> ();
                                        fixed (byte* retvalSwiftDataPtr = StructMarshal.Marshaler.PrepareValueType (retval)) {
                                                IntPtr thisIntPtr = StructMarshal.RetainSwiftObject (this);
                                                NativeMethodsForSwiftIteratorProtocolProtocol.PImethod_SwiftIteratorProtocolProtocolXamarin_SwiftIteratorProtocolProtocolDnext00000001 ((IntPtr)retvalSwiftDataPtr, thisIntPtr);
                                                SwiftCore.Release (thisIntPtr);
                                                return retval;
                                        }
                                }
                        }
                }

#if __IOS__ || __MACOS__ || __TVOS__ || __WATCHOS__
                [MonoPInvokeCallback(typeof(SwiftIteratorProtocol_xam_vtable.Delfunc0))]
#endif
                static void xamVtable_recv_Next_SwiftOptionalT0 (IntPtr xam_retval, IntPtr self)
                {
                        SwiftOptional<ATElement> retval = SwiftObjectRegistry.Registry.CSObjectForSwiftObject<SwiftIteratorProtocolProtocol<ATElement>> (self).Next ();
                        if (typeof (ISwiftObject).IsAssignableFrom (typeof (SwiftOptional<ATElement>))) {
                                Marshal.WriteIntPtr (xam_retval, ((ISwiftObject)retval).SwiftObject);
                        } else {
                                StructMarshal.Marshaler.ToSwift (typeof (SwiftOptional<ATElement>), retval, xam_retval);
                        }
                }

                static void XamSetVTable ()
                {
                        xamVtableISwiftIteratorProtocol.func0 = xamVtable_recv_Next_SwiftOptionalT0;
                        unsafe {
                                byte* vtData = stackalloc byte [Marshal.SizeOf (xamVtableISwiftIteratorProtocol)];
                                IntPtr vtPtr = new IntPtr (vtData);
                                Marshal.WriteIntPtr (vtPtr, Marshal.GetFunctionPointerForDelegate (xamVtableISwiftIteratorProtocol.func0));
                                NativeMethodsForSwiftIteratorProtocolProtocol.SwiftXamSetVtable (vtPtr,
                                        StructMarshal.Marshaler.Metatypeof (typeof (ATElement)));
                        }
                }

                public static IntPtr ProtocolWitnessTable {
                        get {
                                if (protocolWitnessTable == IntPtr.Zero) {
                                        protocolWitnessTable = SwiftCore.ProtocolWitnessTableFromFile (SwiftCore.kXamGlue,
                                                "$s7XamGlue021SwiftIteratorProtocolE0CyxGStAAMc", GetSwiftMetatype ());
                                }
                                return protocolWitnessTable;
                        }
                }
        }

        internal class NativeMethodsForSwiftIteratorProtocolProtocol {
                [DllImport (SwiftCore.kXamGlue, EntryPoint = "$s7XamGlue029xamarin_SwiftIteratorProtocolF13Dnext000000016retval4thisySpyxSgG_AA0defF0CyxGtlF")]
                internal static extern void PImethod_SwiftIteratorProtocolProtocolXamarin_SwiftIteratorProtocolProtocolDnext00000001 (IntPtr retval, IntPtr this0);
                [DllImport (SwiftCore.kXamGlue, EntryPoint = "$s7XamGlue35setSwiftIteratorProtocol_xam_vtableyySV_ypXptF")]
                internal static extern void SwiftXamSetVtable (IntPtr
                        vt, SwiftMetatype t0);
                [DllImport (SwiftCore.kXamGlue, EntryPoint = "$s7XamGlue029xamarin_SwiftIteratorProtocolf6DSwifteF16Protocol00000002AA0defF0CyxGylF")]
                internal static extern IntPtr PI_SwiftIteratorProtocolProtocol (SwiftMetatype mt);
                [DllImport (SwiftCore.kXamGlue, EntryPoint = "$s7XamGlue021SwiftIteratorProtocolE0CMa")]
                internal static extern SwiftMetatype PIMetadataAccessor_SwiftIteratorProtocolProtocol (SwiftMetadataRequest
                        request, SwiftMetatype mt0);
        }

        internal struct SwiftIteratorProtocol_xam_vtable {
                public delegate void Delfunc0 (IntPtr xam_retval, IntPtr self);
                [MarshalAs (UnmanagedType.FunctionPtr)]
                public Delfunc0 func0;
        }

        public static class SwiftIteratorProtocolExtensions {
                public static IEnumerable<T> AsIEnumerable<T> (this ISwiftIteratorProtocol<T> iterator)
                {
                        while (true) {
                                var element = iterator.Next ();
                                if (element.HasValue)
                                        yield return element.Value;
                                else
                                        break;
                        }
                        yield break;
                }

                public static ISwiftIteratorProtocol<T> AsISwiftIteratorProtocol<T> (this IEnumerable<T> iterator)
                {
                        return new EnumerableAdapter<T> (iterator);
                }
        }

        public sealed class EnumerableAdapter<T> : ISwiftIteratorProtocol<T> {
                IEnumerator<T> enumerator;
                public EnumerableAdapter (IEnumerable<T> enumerable)
                {
                        enumerator = Exceptions.ThrowOnNull (enumerable, nameof (enumerable)).GetEnumerator ();
                }

                public SwiftOptional<T> Next ()
                {
                        if (enumerator.MoveNext ())
                                return SwiftOptional<T>.Some (enumerator.Current);
                        else
                                return SwiftOptional<T>.None ();
                }
        }
}
