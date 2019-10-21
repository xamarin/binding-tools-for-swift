namespace SwiftReflector.SwiftXmlReflection {
	public class StructDeclaration : TypeDeclaration {
		public StructDeclaration ()
		{
			Kind = TypeKind.Struct;
		}

		protected override TypeDeclaration UnrootedFactory ()
		{
			return new StructDeclaration ();
		}
	}
}

