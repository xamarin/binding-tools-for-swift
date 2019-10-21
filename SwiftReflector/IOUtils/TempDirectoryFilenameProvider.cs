
namespace SwiftReflector.IOUtils {
	public class TempDirectoryFilenameProvider : TempDirectoryStreamProvider<string> {

		// directoryName can be null
		public TempDirectoryFilenameProvider (string directoryName = null, bool prependGuid = true)
			: base (directoryName, prependGuid)
		{
		}

		#region implemented abstract members of TempDirectoryStreamProvider
		protected override string FromThing (string thing)
		{
			return thing;
		}
		#endregion

	}
}

