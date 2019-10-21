using System;

namespace Dynamo.CSLang {
	public class CSFunctionCall : CSBaseExpression, ICSLineable {
		public CSFunctionCall (CSIdentifier ident, CommaListElementCollection<CSBaseExpression> paramList, bool isConstructor = false)
		{
			Name = Exceptions.ThrowOnNull (ident, "ident");
			Parameters = Exceptions.ThrowOnNull (paramList, "paramList");
			IsConstructor = isConstructor;
		}

		public CSFunctionCall (string identifier, bool isConstructor, params CSBaseExpression [] parameters)
			: this (new CSIdentifier (identifier), new CommaListElementCollection<CSBaseExpression> (parameters), isConstructor)
		{
		}

		public static CSFunctionCall Function (string identifier, params CSBaseExpression [] parameters)
		{
			return new CSFunctionCall (identifier, false, parameters);
		}
		public static CSLine FunctionLine (string identifier, params CSBaseExpression [] parameters) => new CSLine (Function (identifier, parameters));

		public static CSFunctionCall Ctor (string identifier, params CSBaseExpression [] parameters)
		{
			return new CSFunctionCall (identifier, true, parameters);
		}
		public static CSLine CtorLine (string identifier, params CSBaseExpression [] parameters) => new CSLine (Ctor (identifier, parameters));

		public static CSLine ConsoleWriteLine (params CSBaseExpression [] parameters) => FunctionLine ("Console.WriteLine", parameters);

		protected override void LLWrite (ICodeWriter writer, object o)
		{
			if (IsConstructor)
				writer.Write ("new ", false);
			Name.WriteAll (writer);
			writer.Write ("(", false);
			Parameters.WriteAll (writer);
			writer.Write (")", false);
		}

		public bool IsConstructor { get; private set; }
		public CSIdentifier Name { get; private set; }
		public CommaListElementCollection<CSBaseExpression> Parameters { get; private set; }

		public static CSLine FunctionCallLine (CSIdentifier identifier, params CSBaseExpression [] parameters)
		{
			return FunctionCallLine (identifier, false, parameters);
		}

		public static CSLine FunctionCallLine (CSIdentifier identifier, bool isConstructor, params CSBaseExpression [] parameters)
		{
			return new CSLine (new CSFunctionCall (identifier,
				new CommaListElementCollection<CSBaseExpression> (parameters), isConstructor));
		}

		public static CSLine FunctionCallLine (string identifier, params CSBaseExpression [] parameters)
		{
			return FunctionCallLine (identifier, false, parameters);
		}

		public static CSLine FunctionCallLine (string identifier, bool isConstructor, params CSBaseExpression [] parameters)
		{
			return new CSLine (new CSFunctionCall (new CSIdentifier (Exceptions.ThrowOnNull (identifier, "identifier")),
				new CommaListElementCollection<CSBaseExpression> (parameters), isConstructor));
		}

		static CSIdentifier iTypeof = new CSIdentifier ("typeof");

		public static CSFunctionCall Typeof (Type t)
		{
			return Typeof (t.Name);
		}

		public static CSFunctionCall Typeof (string t)
		{
			CommaListElementCollection<CSBaseExpression> parms = new CommaListElementCollection<CSBaseExpression> ();
			parms.Add (new CSIdentifier (t));
			return new CSFunctionCall (iTypeof, parms, false);
		}

		public static CSFunctionCall Typeof (CSSimpleType t)
		{
			CommaListElementCollection<CSBaseExpression> parms = new CommaListElementCollection<CSBaseExpression> ();
			parms.Add (new CSIdentifier (t.Name));
			return new CSFunctionCall (iTypeof, parms, false);
		}


		static CSIdentifier iSizeof = new CSIdentifier ("sizeof");

		public static CSFunctionCall Sizeof (CSBaseExpression expr)
		{
			CommaListElementCollection<CSBaseExpression> parms = new CommaListElementCollection<CSBaseExpression> ();
			parms.Add (expr);
			return new CSFunctionCall (iSizeof, parms, false);
		}

	}

}

