[Aspect]
internal class TargetCode
{
  private global::System.String Default(global::System.Object arg)
  {
    return (global::System.String)(arg?.ToString() + arg.ToString());
  }
  private global::System.String NonNullable(global::System.Object arg)
  {
    return (global::System.String)(arg.ToString() + arg.ToString());
  }
  private global::System.String Nullable(global::System.Object? arg)
  {
    return (global::System.String)(arg?.ToString() + arg!.ToString());
  }
}