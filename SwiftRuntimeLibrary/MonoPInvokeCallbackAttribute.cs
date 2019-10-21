using System;

namespace Xamarin.iOS {
	// this is not the actual attribute - FIXME
	public class MonoPInvokeCallbackAttribute : Attribute {
		public MonoPInvokeCallbackAttribute (Type delegateType)
		{
			DelegateType = delegateType;
		}

		public Type DelegateType { get; set; }
	}
}