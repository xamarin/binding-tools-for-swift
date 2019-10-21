using System;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace System {
	[Serializable]
	public struct nint : IFormattable, IConvertible, IComparable, IComparable<nint>, IEquatable<nint> {
		public nint (nint v) { this.v = v.v; }
		public nint (Int32 v) { this.v = v; }

#if ARCH_32
		public static readonly nint MaxValue = Int32.MaxValue;
		public static readonly nint MinValue = Int32.MinValue;

		public Int32 v;

		public nint (Int64 v) { this.v = (Int32)v; }
#else
		public static readonly nint MaxValue = Int32.MaxValue;
		public static readonly nint MinValue = Int32.MinValue;

		Int64 v;

		public nint (Int64 v) { this.v = v; }
#endif

#if ARCH_32
#if NINT_JIT_OPTIMIZED
		public static implicit operator Int32 (nint v) { throw new NotImplementedException (); }
		public static implicit operator nint (Int32 v) { throw new NotImplementedException (); }
		public static implicit operator Int64 (nint v) { throw new NotImplementedException (); }
		public static explicit operator nint (Int64 v) { throw new NotImplementedException (); }
#else
		public static implicit operator Int32 (nint v) { return v.v; }
		public static implicit operator nint (Int32 v) { return new nint (v); }
		public static implicit operator Int64 (nint v) { return (Int64)v.v; }
		public static explicit operator nint (Int64 v) { return new nint (v); }
#endif
#else
#if NINT_JIT_OPTIMIZED
		public static explicit operator Int32 (nint v) { throw new NotImplementedException (); }
		public static implicit operator nint (Int32 v) { throw new NotImplementedException (); }
		public static implicit operator Int64 (nint v) { throw new NotImplementedException (); }
		public static implicit operator nint (Int64 v) { throw new NotImplementedException (); }
#else
		public static explicit operator Int32 (nint v) { return (Int32)v.v; }
		public static implicit operator nint (Int32 v) { return new nint (v); }
		public static implicit operator Int64 (nint v) { return v.v; }
		public static implicit operator nint (Int64 v) { return new nint (v); }
#endif
#endif

#if NINT_JIT_OPTIMIZED
		public static nint operator + (nint v) { throw new NotImplementedException (); }
		public static nint operator - (nint v) { throw new NotImplementedException (); }
		public static nint operator ~ (nint v) { throw new NotImplementedException (); }
#else
		public static nint operator + (nint v) { return new nint (+v.v); }
		public static nint operator - (nint v) { return new nint (-v.v); }
		public static nint operator ~ (nint v) { return new nint (~v.v); }
#endif

#if NINT_JIT_OPTIMIZED
		public static nint operator ++ (nint v) { throw new NotImplementedException (); }
		public static nint operator -- (nint v) { throw new NotImplementedException (); }
#else
		[MethodImpl (MethodImplOptions.AggressiveInlining)]
		public static nint operator ++ (nint v) { return new nint (v.v + 1); }
		public static nint operator -- (nint v) { return new nint (v.v - 1); }
#endif

#if NINT_JIT_OPTIMIZED
		public static nint operator + (nint l, nint r) { throw new NotImplementedException (); }
		public static nint operator - (nint l, nint r) { throw new NotImplementedException (); }
		public static nint operator * (nint l, nint r) { throw new NotImplementedException (); }
		public static nint operator / (nint l, nint r) { throw new NotImplementedException (); }
		public static nint operator % (nint l, nint r) { throw new NotImplementedException (); }
		public static nint operator & (nint l, nint r) { throw new NotImplementedException (); }
		public static nint operator | (nint l, nint r) { throw new NotImplementedException (); }
		public static nint operator ^ (nint l, nint r) { throw new NotImplementedException (); }

		public static nint operator << (nint l, int r) { throw new NotImplementedException (); }
		public static nint operator >> (nint l, int r) { throw new NotImplementedException (); }
#else
		public static nint operator + (nint l, nint r) { return new nint (l.v + r.v); }
		public static nint operator - (nint l, nint r) { return new nint (l.v - r.v); }
		public static nint operator * (nint l, nint r) { return new nint (l.v * r.v); }
		public static nint operator / (nint l, nint r) { return new nint (l.v / r.v); }
		public static nint operator % (nint l, nint r) { return new nint (l.v % r.v); }
		public static nint operator & (nint l, nint r) { return new nint (l.v & r.v); }
		public static nint operator | (nint l, nint r) { return new nint (l.v | r.v); }
		public static nint operator ^ (nint l, nint r) { return new nint (l.v ^ r.v); }

		public static nint operator << (nint l, int r) { return new nint (l.v << r); }
		public static nint operator >> (nint l, int r) { return new nint (l.v >> r); }
#endif

#if NINT_JIT_OPTIMIZED
		public static bool operator == (nint l, nint r) { throw new NotImplementedException (); }
		public static bool operator != (nint l, nint r) { throw new NotImplementedException (); }
		public static bool operator <  (nint l, nint r) { throw new NotImplementedException (); }
		public static bool operator >  (nint l, nint r) { throw new NotImplementedException (); }
		public static bool operator <= (nint l, nint r) { throw new NotImplementedException (); }
		public static bool operator >= (nint l, nint r) { throw new NotImplementedException (); }
#else
		public static bool operator == (nint l, nint r) { return l.v == r.v; }
		public static bool operator != (nint l, nint r) { return l.v != r.v; }
		public static bool operator < (nint l, nint r) { return l.v < r.v; }
		public static bool operator > (nint l, nint r) { return l.v > r.v; }
		public static bool operator <= (nint l, nint r) { return l.v <= r.v; }
		public static bool operator >= (nint l, nint r) { return l.v >= r.v; }
#endif

		public int CompareTo (nint value) { return v.CompareTo (value.v); }
		public int CompareTo (object value) { return v.CompareTo (value); }
		public bool Equals (nint obj) { return v.Equals (obj.v); }
		public override bool Equals (object obj) { return v.Equals (obj); }
		public override int GetHashCode () { return v.GetHashCode (); }

#if ARCH_32
		public static nint Parse (string s, IFormatProvider provider) { return Int32.Parse (s, provider); }
		public static nint Parse (string s, NumberStyles style) { return Int32.Parse (s, style); }
		public static nint Parse (string s) { return Int32.Parse (s); }
		public static nint Parse (string s, NumberStyles style, IFormatProvider provider) {
		return Int32.Parse (s, style, provider);
		}

		public static bool TryParse (string s, out nint result)
		{
		Int32 v;
		var r = Int32.TryParse (s, out v);
		result = v;
		return r;
		}

		public static bool TryParse (string s, NumberStyles style, IFormatProvider provider, out nint result)
		{
		Int32 v;
		var r = Int32.TryParse (s, style, provider, out v);
		result = v;
		return r;
		}
#else
		public static nint Parse (string s, IFormatProvider provider) { return Int64.Parse (s, provider); }
		public static nint Parse (string s, NumberStyles style) { return Int64.Parse (s, style); }
		public static nint Parse (string s) { return Int64.Parse (s); }
		public static nint Parse (string s, NumberStyles style, IFormatProvider provider)
		{
			return Int64.Parse (s, style, provider);
		}

		public static bool TryParse (string s, out nint result)
		{
			Int64 v;
			var r = Int64.TryParse (s, out v);
			result = v;
			return r;
		}

		public static bool TryParse (string s, NumberStyles style, IFormatProvider provider, out nint result)
		{
			Int64 v;
			var r = Int64.TryParse (s, style, provider, out v);
			result = v;
			return r;
		}
#endif

		public override string ToString () { return v.ToString (); }
		public string ToString (IFormatProvider provider) { return v.ToString (provider); }
		public string ToString (string format) { return v.ToString (format); }
		public string ToString (string format, IFormatProvider provider) { return v.ToString (format, provider); }

		public TypeCode GetTypeCode () { return v.GetTypeCode (); }

		bool IConvertible.ToBoolean (IFormatProvider provider) { return ((IConvertible)v).ToBoolean (provider); }
		byte IConvertible.ToByte (IFormatProvider provider) { return ((IConvertible)v).ToByte (provider); }
		char IConvertible.ToChar (IFormatProvider provider) { return ((IConvertible)v).ToChar (provider); }
		DateTime IConvertible.ToDateTime (IFormatProvider provider) { return ((IConvertible)v).ToDateTime (provider); }
		decimal IConvertible.ToDecimal (IFormatProvider provider) { return ((IConvertible)v).ToDecimal (provider); }
		double IConvertible.ToDouble (IFormatProvider provider) { return ((IConvertible)v).ToDouble (provider); }
		short IConvertible.ToInt16 (IFormatProvider provider) { return ((IConvertible)v).ToInt16 (provider); }
		int IConvertible.ToInt32 (IFormatProvider provider) { return ((IConvertible)v).ToInt32 (provider); }
		long IConvertible.ToInt64 (IFormatProvider provider) { return ((IConvertible)v).ToInt64 (provider); }
		sbyte IConvertible.ToSByte (IFormatProvider provider) { return ((IConvertible)v).ToSByte (provider); }
		float IConvertible.ToSingle (IFormatProvider provider) { return ((IConvertible)v).ToSingle (provider); }
		ushort IConvertible.ToUInt16 (IFormatProvider provider) { return ((IConvertible)v).ToUInt16 (provider); }
		uint IConvertible.ToUInt32 (IFormatProvider provider) { return ((IConvertible)v).ToUInt32 (provider); }
		ulong IConvertible.ToUInt64 (IFormatProvider provider) { return ((IConvertible)v).ToUInt64 (provider); }

		object IConvertible.ToType (Type targetType, IFormatProvider provider)
		{
			return ((IConvertible)v).ToType (targetType, provider);
		}
	}

	[Serializable]
	public struct nuint : IFormattable, IConvertible, IComparable, IComparable<nuint>, IEquatable<nuint> {
		public nuint (nuint v) { this.v = v.v; }
		public nuint (UInt32 v) { this.v = v; }

#if ARCH_32
		public static readonly nuint MaxValue = UInt32.MaxValue;
		public static readonly nuint MinValue = UInt32.MinValue;

		UInt32 v;

		public nuint (UInt64 v) { this.v = (UInt32)v; }
#else
		public static readonly nuint MaxValue = UInt32.MaxValue;
		public static readonly nuint MinValue = UInt32.MinValue;

		UInt64 v;

		public nuint (UInt64 v) { this.v = v; }
#endif

#if ARCH_32
#if NINT_JIT_OPTIMIZED
		public static implicit operator UInt32 (nuint v) { throw new NotImplementedException (); }
		public static implicit operator nuint (UInt32 v) { throw new NotImplementedException (); }
		public static implicit operator UInt64 (nuint v) { throw new NotImplementedException (); }
		public static explicit operator nuint (UInt64 v) { throw new NotImplementedException (); }
#else
		public static implicit operator UInt32 (nuint v) { return v.v; }
		public static implicit operator nuint (UInt32 v) { return new nuint (v); }
		public static implicit operator UInt64 (nuint v) { return (UInt64)v.v; }
		public static explicit operator nuint (UInt64 v) { return new nuint (v); }
#endif
#else
#if NINT_JIT_OPTIMIZED
		public static explicit operator UInt32 (nuint v) { throw new NotImplementedException (); }
		public static implicit operator nuint (UInt32 v) { throw new NotImplementedException (); }
		public static implicit operator UInt64 (nuint v) { throw new NotImplementedException (); }
		public static implicit operator nuint (UInt64 v) { throw new NotImplementedException (); }
#else
		public static explicit operator UInt32 (nuint v) { return (UInt32)v.v; }
		public static implicit operator nuint (UInt32 v) { return new nuint (v); }
		public static implicit operator UInt64 (nuint v) { return v.v; }
		public static implicit operator nuint (UInt64 v) { return new nuint (v); }
#endif
#endif

#if NINT_JIT_OPTIMIZED
		public static nuint operator + (nuint v) { throw new NotImplementedException (); }
		public static nuint operator ~ (nuint v) { throw new NotImplementedException (); }
#else
		public static nuint operator + (nuint v) { return new nuint (+v.v); }
		public static nuint operator ~ (nuint v) { return new nuint (~v.v); }
#endif

#if NINT_JIT_OPTIMIZED
		public static nuint operator ++ (nuint v) { throw new NotImplementedException (); }
		public static nuint operator -- (nuint v) { throw new NotImplementedException (); }
#else
		[MethodImpl (MethodImplOptions.AggressiveInlining)]
		public static nuint operator ++ (nuint v) { return new nuint (v.v + 1); }
		public static nuint operator -- (nuint v) { return new nuint (v.v - 1); }
#endif

#if NINT_JIT_OPTIMIZED
		public static nuint operator + (nuint l, nuint r) { throw new NotImplementedException (); }
		public static nuint operator - (nuint l, nuint r) { throw new NotImplementedException (); }
		public static nuint operator * (nuint l, nuint r) { throw new NotImplementedException (); }
		public static nuint operator / (nuint l, nuint r) { throw new NotImplementedException (); }
		public static nuint operator % (nuint l, nuint r) { throw new NotImplementedException (); }
		public static nuint operator & (nuint l, nuint r) { throw new NotImplementedException (); }
		public static nuint operator | (nuint l, nuint r) { throw new NotImplementedException (); }
		public static nuint operator ^ (nuint l, nuint r) { throw new NotImplementedException (); }

		public static nuint operator << (nuint l, int r) { throw new NotImplementedException (); }
		public static nuint operator >> (nuint l, int r) { throw new NotImplementedException (); }
#else
		public static nuint operator + (nuint l, nuint r) { return new nuint (l.v + r.v); }
		public static nuint operator - (nuint l, nuint r) { return new nuint (l.v - r.v); }
		public static nuint operator * (nuint l, nuint r) { return new nuint (l.v * r.v); }
		public static nuint operator / (nuint l, nuint r) { return new nuint (l.v / r.v); }
		public static nuint operator % (nuint l, nuint r) { return new nuint (l.v % r.v); }
		public static nuint operator & (nuint l, nuint r) { return new nuint (l.v & r.v); }
		public static nuint operator | (nuint l, nuint r) { return new nuint (l.v | r.v); }
		public static nuint operator ^ (nuint l, nuint r) { return new nuint (l.v ^ r.v); }

		public static nuint operator << (nuint l, int r) { return new nuint (l.v << r); }
		public static nuint operator >> (nuint l, int r) { return new nuint (l.v >> r); }
#endif

#if NINT_JIT_OPTIMIZED
		public static bool operator == (nuint l, nuint r) { throw new NotImplementedException (); }
		public static bool operator != (nuint l, nuint r) { throw new NotImplementedException (); }
		public static bool operator <  (nuint l, nuint r) { throw new NotImplementedException (); }
		public static bool operator >  (nuint l, nuint r) { throw new NotImplementedException (); }
		public static bool operator <= (nuint l, nuint r) { throw new NotImplementedException (); }
		public static bool operator >= (nuint l, nuint r) { throw new NotImplementedException (); }
#else
		public static bool operator == (nuint l, nuint r) { return l.v == r.v; }
		public static bool operator != (nuint l, nuint r) { return l.v != r.v; }
		public static bool operator < (nuint l, nuint r) { return l.v < r.v; }
		public static bool operator > (nuint l, nuint r) { return l.v > r.v; }
		public static bool operator <= (nuint l, nuint r) { return l.v <= r.v; }
		public static bool operator >= (nuint l, nuint r) { return l.v >= r.v; }
#endif

		public int CompareTo (nuint value) { return v.CompareTo (value.v); }
		public int CompareTo (object value) { return v.CompareTo (value); }
		public bool Equals (nuint obj) { return v.Equals (obj.v); }
		public override bool Equals (object obj) { return v.Equals (obj); }
		public override int GetHashCode () { return v.GetHashCode (); }

#if ARCH_32
		public static nuint Parse (string s, IFormatProvider provider) { return UInt32.Parse (s, provider); }
		public static nuint Parse (string s, NumberStyles style) { return UInt32.Parse (s, style); }
		public static nuint Parse (string s) { return UInt32.Parse (s); }
		public static nuint Parse (string s, NumberStyles style, IFormatProvider provider) {
		return UInt32.Parse (s, style, provider);
		}

		public static bool TryParse (string s, out nuint result)
		{
		UInt32 v;
		var r = UInt32.TryParse (s, out v);
		result = v;
		return r;
		}

		public static bool TryParse (string s, NumberStyles style, IFormatProvider provider, out nuint result)
		{
		UInt32 v;
		var r = UInt32.TryParse (s, style, provider, out v);
		result = v;
		return r;
		}
#else
		public static nuint Parse (string s, IFormatProvider provider) { return UInt64.Parse (s, provider); }
		public static nuint Parse (string s, NumberStyles style) { return UInt64.Parse (s, style); }
		public static nuint Parse (string s) { return UInt64.Parse (s); }
		public static nuint Parse (string s, NumberStyles style, IFormatProvider provider)
		{
			return UInt64.Parse (s, style, provider);
		}

		public static bool TryParse (string s, out nuint result)
		{
			UInt64 v;
			var r = UInt64.TryParse (s, out v);
			result = v;
			return r;
		}

		public static bool TryParse (string s, NumberStyles style, IFormatProvider provider, out nuint result)
		{
			UInt64 v;
			var r = UInt64.TryParse (s, style, provider, out v);
			result = v;
			return r;
		}
#endif

		public override string ToString () { return v.ToString (); }
		public string ToString (IFormatProvider provider) { return v.ToString (provider); }
		public string ToString (string format) { return v.ToString (format); }
		public string ToString (string format, IFormatProvider provider) { return v.ToString (format, provider); }

		public TypeCode GetTypeCode () { return v.GetTypeCode (); }

		bool IConvertible.ToBoolean (IFormatProvider provider) { return ((IConvertible)v).ToBoolean (provider); }
		byte IConvertible.ToByte (IFormatProvider provider) { return ((IConvertible)v).ToByte (provider); }
		char IConvertible.ToChar (IFormatProvider provider) { return ((IConvertible)v).ToChar (provider); }
		DateTime IConvertible.ToDateTime (IFormatProvider provider) { return ((IConvertible)v).ToDateTime (provider); }
		decimal IConvertible.ToDecimal (IFormatProvider provider) { return ((IConvertible)v).ToDecimal (provider); }
		double IConvertible.ToDouble (IFormatProvider provider) { return ((IConvertible)v).ToDouble (provider); }
		short IConvertible.ToInt16 (IFormatProvider provider) { return ((IConvertible)v).ToInt16 (provider); }
		int IConvertible.ToInt32 (IFormatProvider provider) { return ((IConvertible)v).ToInt32 (provider); }
		long IConvertible.ToInt64 (IFormatProvider provider) { return ((IConvertible)v).ToInt64 (provider); }
		sbyte IConvertible.ToSByte (IFormatProvider provider) { return ((IConvertible)v).ToSByte (provider); }
		float IConvertible.ToSingle (IFormatProvider provider) { return ((IConvertible)v).ToSingle (provider); }
		ushort IConvertible.ToUInt16 (IFormatProvider provider) { return ((IConvertible)v).ToUInt16 (provider); }
		uint IConvertible.ToUInt32 (IFormatProvider provider) { return ((IConvertible)v).ToUInt32 (provider); }
		ulong IConvertible.ToUInt64 (IFormatProvider provider) { return ((IConvertible)v).ToUInt64 (provider); }

		object IConvertible.ToType (Type targetType, IFormatProvider provider)
		{
			return ((IConvertible)v).ToType (targetType, provider);
		}
	}
}