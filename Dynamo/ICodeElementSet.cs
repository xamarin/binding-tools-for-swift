using System.Collections.Generic;

namespace Dynamo {
	public interface ICodeElementSet : ICodeElement {
		IEnumerable<ICodeElement> Elements { get; }
	}
}

