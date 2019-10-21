using System;
using System.Collections.Generic;
using System.Linq;

namespace SwiftRuntimeLibrary.SwiftMarshal {
	public class SwiftTupleMap {
		SwiftTupleMap ()
		{
		}

		public Type [] Types { get; private set; }
		public int [] Offsets { get; private set; }
		public int Size { get; private set; }
		public int Stride { get; private set; }

		static Dictionary<Type [], SwiftTupleMap> cache = new Dictionary<Type [], SwiftTupleMap> ();
		static object cachelock = new object ();

		public static SwiftTupleMap FromTypes (Type [] types)
		{
			lock (cachelock) {
				SwiftTupleMap map;
				if (cache.TryGetValue (types, out map))
					return map;
				map = new SwiftTupleMap ();
				map.Types = types;
				var metatypes = new SwiftMetatype [types.Length];
				for (int i = 0; i < types.Length; i++) {
					metatypes [i] = StructMarshal.Marshaler.Metatypeof (types [i]);
				}
				var mt = SwiftCore.TupleMetatype (metatypes);
				map.Offsets = mt.GetTupleElementOffsets ();
				map.Size = (int)SwiftCore.SizeOf (mt);
				map.Stride = (int)SwiftCore.StrideOf (mt);

				cache.Add (types, map);

				return map;
			}
		}
	}
}

