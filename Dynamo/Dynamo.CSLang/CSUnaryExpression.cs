using System;

namespace Dynamo.CSLang {
	public class CSUnaryExpression : CSBaseExpression {
		public CSUnaryExpression (CSUnaryOperator op, ICSExpression expr)
		{
			Operation = op;
			Expr = Exceptions.ThrowOnNull (expr, nameof(expr));
		}
		protected override void LLWrite (ICodeWriter writer, object o)
		{
			writer.Write (string.Format (Operation == CSUnaryOperator.Ref || Operation == CSUnaryOperator.Out ? "{0} " : "{0}", OperatorToString (Operation)), true);
			Expr.WriteAll (writer);
		}

		public CSUnaryOperator Operation { get; private set; }
		public ICSExpression Expr { get; private set; }

		static string OperatorToString (CSUnaryOperator op)
		{
			switch (op) {
			case CSUnaryOperator.At:
				return "@";
			case CSUnaryOperator.BitNot:
				return "~";
			case CSUnaryOperator.Neg:
				return "-";
			case CSUnaryOperator.Not:
				return "!";
			case CSUnaryOperator.Out:
				return "out";
			case CSUnaryOperator.Pos:
				return "+";
			case CSUnaryOperator.Ref:
				return "ref";
			case CSUnaryOperator.AddressOf:
				return "&";
			case CSUnaryOperator.Indirection:
				return "*";
			default:
				throw new ArgumentOutOfRangeException (nameof(op));
			}
		}

		public static CSUnaryExpression Out (CSIdentifier id)
		{
			return new CSUnaryExpression (CSUnaryOperator.Out, id);
		}

		public static CSUnaryExpression Out (string id)
		{
			return Out (new CSIdentifier (id));
		}

		public static CSUnaryExpression Ref (CSIdentifier id)
		{
			return new CSUnaryExpression (CSUnaryOperator.Ref, id);
		}

		public static CSUnaryExpression Ref (string id)
		{
			return Ref (new CSIdentifier (id));
		}


	}
}

