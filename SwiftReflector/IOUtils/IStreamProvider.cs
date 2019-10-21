using System.IO;

namespace SwiftReflector.IOUtils {
	public interface IStreamProvider<T> {
		Stream ProvideStreamFor (T thing);
		void NotifyStreamDone (T thing, Stream stm);
		void RemoveStream (T thing, Stream stm);
	}
}

