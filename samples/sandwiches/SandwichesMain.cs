
using System;
using System.IO;
using SwiftRuntimeLibrary;
using SwiftRuntimeLibrary.SwiftMarshal;
using System.Runtime.InteropServices;
using Sandwiches;

namespace SandwichesCSharp
{

    public class WholeWheat : IBread {
	public SwiftString Name => (SwiftString)"whole wheat";
	public bool Sliced => true;
    }

    public class SharpCheddar : IFilling {
        public SwiftString Stuff => (SwiftString)"sharp cheddar";
    }

    public class SandwichesMain
    {
        public static void Main(string[] args)
        {
		TopLevelEntities.PrintSandwich (new WholeWheat (), new SharpCheddar ());
        }
    }
}
