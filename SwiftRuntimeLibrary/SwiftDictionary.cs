using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using SwiftRuntimeLibrary.SwiftMarshal;

namespace SwiftRuntimeLibrary {

	[SwiftStruct (SwiftCoreConstants.LibSwiftCore, SwiftCoreConstants.SwiftDictionary_NominalTypeDescriptor, "", "")]
	public class SwiftDictionary<T, U> : IDictionary<T, U>, ISwiftStruct {
		public byte [] SwiftData { get; set; }

		public SwiftDictionary () : this (0) { }

		public SwiftDictionary (int capacity)
		    : this (SwiftNominalCtorArgument.None)
		{
			unsafe {
				fixed (byte* retvalData = StructMarshal.Marshaler.PrepareNominal (this)) {
					DictPI.NewDict (new IntPtr(retvalData), capacity, StructMarshal.Marshaler.Metatypeof (typeof (T)),
					                StructMarshal.Marshaler.Metatypeof (typeof (U)),
					                StructMarshal.Marshaler.ProtocolWitnessof (typeof (ISwiftHashable), typeof (T)));
					StructMarshal.Marshaler.RetainNominalData (typeof (SwiftDictionary<T, U>), retvalData, SwiftData.Length);
				}
			}
		}

		public SwiftDictionary (IDictionary<T, U> elems)
		    : this (0)
		{
			if (elems == null)
				throw new ArgumentNullException (nameof (elems));
			foreach (KeyValuePair<T, U> pair in elems) {
				Add (pair);
			}
		}

		internal SwiftDictionary (SwiftNominalCtorArgument unused)
		{
			StructMarshal.Marshaler.PrepareNominal (this);
		}


		public static SwiftMetatype GetSwiftMetatype ()
		{
			return DictPI.PIMetadataAccessor_SwiftDictionary (SwiftMetadataRequest.Complete, StructMarshal.Marshaler.Metatypeof (typeof (T)),
			                                StructMarshal.Marshaler.Metatypeof (typeof (U)),
			                                StructMarshal.Marshaler.ProtocolWitnessof (typeof (ISwiftHashable), typeof (T)));
		}

		public unsafe int Count {
			get {
				fixed (byte* thisPtr = SwiftData) {
					var thisIntPtr = new IntPtr (thisPtr);
					return (int)DictPI.DictCount (thisIntPtr,
								      StructMarshal.Marshaler.Metatypeof (typeof (T)),
								      StructMarshal.Marshaler.Metatypeof (typeof (U)),
								      StructMarshal.Marshaler.ProtocolWitnessof (typeof (ISwiftHashable), typeof (T)));
				}
			}
		}

		public bool IsReadOnly {
			get {
				return false;
			}
		}

		public ICollection<T> Keys {
			get {
				unsafe {
					fixed (byte* thisPtr = SwiftData) {
						var keys = new SwiftArray<T> (SwiftNominalCtorArgument.None);
						fixed (byte *keyData = StructMarshal.Marshaler.PrepareNominal(keys)) {
							var thisIntPtr = new IntPtr (thisPtr);
							DictPI.DictKeys(new IntPtr(keyData), thisIntPtr,
							                StructMarshal.Marshaler.Metatypeof (typeof (T)),
							                StructMarshal.Marshaler.Metatypeof (typeof (U)),
							                StructMarshal.Marshaler.ProtocolWitnessof (typeof (ISwiftHashable), typeof (T)));
						}
						return keys;
					}
				}
			}
		}


		public ICollection<U> Values {
			get {
				unsafe {
					fixed (byte* thisPtr = SwiftData) {
						var values = new SwiftArray<U> (SwiftNominalCtorArgument.None);
						fixed (byte* valueData = StructMarshal.Marshaler.PrepareNominal (values)) {
							var thisIntPtr = new IntPtr (thisPtr);
							DictPI.DictKeys (new IntPtr (valueData), thisIntPtr,
									StructMarshal.Marshaler.Metatypeof (typeof (T)),
									StructMarshal.Marshaler.Metatypeof (typeof (U)),
									StructMarshal.Marshaler.ProtocolWitnessof (typeof (ISwiftHashable), typeof (T)));
						}
						return values;
					}
				}
			}
		}

		public void Add (KeyValuePair<T, U> item)
		{
			Add (item.Key, item.Value);
		}

		public void Add (T key, U value)
		{
			if (ContainsKey (key)) {
				throw new ArgumentException ($"key {key} already present", nameof (key));
			}
			this [key] = value;
		}

		public bool Contains (KeyValuePair<T, U> item)
		{
			U val = default (U);
			return TryGetValue (item.Key, out val) ? EqualityComparer<U>.Default.Equals (item.Value, val) : false;
		}

		public bool ContainsKey (T key)
		{
			unsafe {
				fixed (byte* thisPtr = SwiftData) {
					var thisIntPtr = new IntPtr (thisPtr);
					StructMarshal.Marshaler.RetainNominalData (typeof (SwiftDictionary<T, U>), thisIntPtr, SwiftData.Length);
					byte* keyBuffer = stackalloc byte [StructMarshal.Marshaler.Sizeof (typeof (T))];
					var keyBufferPtr = new IntPtr (keyBuffer);
					StructMarshal.Marshaler.ToSwift (typeof (T), key, keyBufferPtr);
					var result = DictPI.DictContainsKey (thisIntPtr, keyBufferPtr,
								       StructMarshal.Marshaler.Metatypeof (typeof (T)),
								       StructMarshal.Marshaler.Metatypeof (typeof (U)),
								       StructMarshal.Marshaler.ProtocolWitnessof (typeof (ISwiftHashable), typeof (T)));
					StructMarshal.Marshaler.ReleaseSwiftPointer (typeof (SwiftDictionary<T, U>), thisIntPtr);
					StructMarshal.Marshaler.ReleaseSwiftPointer (typeof (T), keyBufferPtr);
					return result;
				}
			}
		}


		public void CopyTo (KeyValuePair<T, U> [] array, int arrayIndex)
		{
			foreach (KeyValuePair<T, U> kvp in this) {
				array [arrayIndex++] = kvp;
			}
		}

		public IEnumerator<KeyValuePair<T, U>> GetEnumerator ()
		{
			foreach (T key in Keys) {
				yield return new KeyValuePair<T, U> (key, this [key]);
			}
		}

		public void Clear ()
		{
			unsafe {
				fixed (byte* thisPtr = SwiftData) {
					IntPtr thisIntPtr = new IntPtr (thisPtr);
					StructMarshal.Marshaler.RetainNominalData (typeof (SwiftDictionary<T, U>), thisIntPtr, SwiftData.Length);
					DictPI.DictClear (thisIntPtr, StructMarshal.Marshaler.Metatypeof (typeof (T)),
							 StructMarshal.Marshaler.Metatypeof (typeof (U)),
							 StructMarshal.Marshaler.ProtocolWitnessof (typeof (ISwiftHashable), typeof (T)));
					StructMarshal.Marshaler.ReleaseSwiftPointer (typeof (SwiftDictionary<T, U>), thisIntPtr);
				}
			}
		}



		public bool Remove (KeyValuePair<T, U> item)
		{
			return Contains (item) ? Remove (item.Key) : false;
		}

		public bool Remove (T key)
		{
			unsafe {
				fixed (byte* thisPtr = SwiftData) {
					var thisIntPtr = new IntPtr (thisPtr);
					StructMarshal.Marshaler.RetainNominalData (typeof (SwiftDictionary<T, U>), thisIntPtr, SwiftData.Length);
					byte* keyBuffer = stackalloc byte [StructMarshal.Marshaler.Sizeof (typeof (T))];
					var keyBufferPtr = new IntPtr (keyBuffer);
					StructMarshal.Marshaler.ToSwift (typeof (T), key, keyBufferPtr);
					bool retval = DictPI.DictRemove (thisIntPtr, keyBufferPtr,
								 StructMarshal.Marshaler.Metatypeof (typeof (T)),
								 StructMarshal.Marshaler.Metatypeof (typeof (U)),
								 StructMarshal.Marshaler.ProtocolWitnessof (typeof (ISwiftHashable), typeof (T)));
					StructMarshal.Marshaler.ReleaseSwiftPointer (typeof (SwiftDictionary<T, U>), thisIntPtr);
					StructMarshal.Marshaler.ReleaseSwiftPointer (typeof (T), keyBufferPtr);

					return retval;
				}
			}
		}

		public bool TryGetValue (T key, out U value)
		{
			if (ContainsKey (key)) {
				value = this [key];
				return true;
			} else {
				value = default (U);
				return false;
			}
		}

		IEnumerator IEnumerable.GetEnumerator ()
		{
			return GetEnumerator ();
		}


		bool disposed = false;
		public void Dispose ()
		{
			if (!disposed) {
				disposed = true;
				Dispose (true);
				GC.SuppressFinalize (this);
			}
		}

		void Dispose (bool disposing)
		{
			StructMarshal.Marshaler.ReleaseNominalData (this);
		}


		public U this [T key] {
			get {
				unsafe {
					Type retType = typeof (Tuple<U, bool>);

					fixed (byte* thisPtr = SwiftData) {
						var thisIntPtr = new IntPtr (thisPtr);

						byte* retBuff = stackalloc byte [StructMarshal.Marshaler.Sizeof (retType)];
						var retPtr = new IntPtr (retBuff);

						byte* keyBuffer = stackalloc byte [StructMarshal.Marshaler.Sizeof (typeof (T))];
						var keyBufferPtr = new IntPtr (keyBuffer);

						StructMarshal.Marshaler.ToSwift (typeof (T), key, keyBufferPtr);

						DictPI.DictGet (retPtr, thisIntPtr, keyBufferPtr,
							       StructMarshal.Marshaler.Metatypeof (typeof (T)),
							       StructMarshal.Marshaler.Metatypeof (typeof (U)),
							       StructMarshal.Marshaler.ProtocolWitnessof (typeof (ISwiftHashable), typeof (T)));
						var retTuple = StructMarshal.Marshaler.ToNet<Tuple<U, bool>> (retPtr);
						StructMarshal.Marshaler.ReleaseSwiftPointer (typeof (T), keyBufferPtr);
						if (retTuple.Item2)
							return retTuple.Item1;
						throw new KeyNotFoundException ($"key {key} not found");
					}
				}

			}

			set {
				unsafe {

					fixed (byte* thisPtr = SwiftData) {
						var thisIntPtr = new IntPtr (thisPtr);
						byte* keyBuffer = stackalloc byte [StructMarshal.Marshaler.Sizeof (typeof (T))];
						var keyBufferPtr = new IntPtr (keyBuffer);
						StructMarshal.Marshaler.ToSwift (typeof (T), key, keyBufferPtr);
						byte* valBuffer = stackalloc byte [StructMarshal.Marshaler.Sizeof (typeof (U))];
						var valBufferPtr = new IntPtr (valBuffer);
						StructMarshal.Marshaler.ToSwift (typeof (U), value, valBufferPtr);
						DictPI.DictSet (thisIntPtr, keyBufferPtr, valBufferPtr,
									 StructMarshal.Marshaler.Metatypeof (typeof (T)),
									 StructMarshal.Marshaler.Metatypeof (typeof (U)),
									 StructMarshal.Marshaler.ProtocolWitnessof (typeof (ISwiftHashable), typeof (T)));
						StructMarshal.Marshaler.ReleaseSwiftPointer (typeof (T), keyBufferPtr);
						StructMarshal.Marshaler.ReleaseSwiftPointer (typeof (T), valBufferPtr);
					}
				}
			}
		}

		static void PrintPtr (string tag, IntPtr p)
		{
			Console.WriteLine ($"{tag} {p.ToString ("X8")}");
		}

		static void PrintBufferPtr (string tag, IntPtr p)
		{
			var target = (p == IntPtr.Zero) ? IntPtr.Zero : Marshal.ReadIntPtr (p);
			Console.WriteLine ($"{tag} {p.ToString ("X8")}: {target.ToString ("X8")}");
		}
	}


	internal static class DictPI {

		[DllImport (SwiftCoreConstants.LibSwiftCore, EntryPoint = SwiftCoreConstants.SwiftDictionary_PIMetadataAccessor)]
		public static extern SwiftMetatype PIMetadataAccessor_SwiftDictionary (SwiftMetadataRequest request, SwiftMetatype t0, SwiftMetatype t1, IntPtr protocolWitness);

		[DllImport (SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.SwiftDictionary_NewDict)]
		public static extern void NewDict (IntPtr retval, nint capacity, SwiftMetatype keyType, SwiftMetatype valType,
							    IntPtr protoWitness);

		[DllImport (SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.SwiftDictionary_DictCount)]
		public static extern nint DictCount (IntPtr dict, SwiftMetatype keyType, SwiftMetatype valType,
							    IntPtr protoWitness);

		[DllImport (SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.SwiftDictionary_DictKeys)]
		public static extern void DictKeys (IntPtr retVal, IntPtr dict, SwiftMetatype keyType, SwiftMetatype valType,
					     IntPtr protoWitness);

		[DllImport (SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.SwiftDictionary_DictValues)]
		public static extern IntPtr DictValues (IntPtr retVal, IntPtr dict, SwiftMetatype keyType, SwiftMetatype valType,
														   IntPtr protoWitness);

		[DllImport (SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.SwiftDictionary_DictContainsKey)]
		public static extern bool DictContainsKey (IntPtr dict, IntPtr keyPtr, SwiftMetatype keyType, SwiftMetatype valType,
													  IntPtr protoWitness);

		[DllImport (SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.SwiftDictionary_DictClear)]
		public static extern void DictClear (IntPtr dictPtr, SwiftMetatype keyType, SwiftMetatype valType,
					    IntPtr protoWitness);

		[DllImport (SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.SwiftDictionary_DictRemove)]
		public static extern bool DictRemove (IntPtr dictPtr, IntPtr keyPtr, SwiftMetatype keyType, SwiftMetatype valType,
							     IntPtr protoWitness);

		[DllImport (SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.SwiftDictionary_DictSet)]
		public static extern void DictSet (IntPtr dictPtr, IntPtr keyPtr, IntPtr valPtr, SwiftMetatype keyType, SwiftMetatype valType,
						  IntPtr protoWitness);

		[DllImport (SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.SwiftDictionary_DictGet)]
		public static extern void DictGet (IntPtr retval, IntPtr dict, IntPtr keyPtr, SwiftMetatype keyType, SwiftMetatype valType,
						  IntPtr protoWitness);
	}
}
