internal class TargetCode
{
  [Aspect]
  private Expression? M()
  {
    global::System.Linq.Expressions.Expression.Property(global::System.Linq.Expressions.Expression.Parameter(typeof(global::System.Int32), "p"), "propertyName");
    var p = global::System.Linq.Expressions.Expression.Property(global::System.Linq.Expressions.Expression.Parameter(typeof(global::System.Int32), "p"), "propertyName");
    return (global::System.Linq.Expressions.Expression? )global::System.Linq.Expressions.Expression.Property(global::System.Linq.Expressions.Expression.Parameter(typeof(global::System.Int32), "p"), "propertyName");
  }
}