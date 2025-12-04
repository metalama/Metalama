[Aspect]
internal class TargetCode
{
  private global::System.String Default(global::System.Object defaultArg1, global::System.Object defaultArg2)
  {
    return (global::System.String)(defaultArg1?.ToString() + defaultArg2.ToString());
  }
  private global::System.String NonNullable(global::System.Object nonNullableArg1, global::System.Object nonNullableArg2)
  {
    return (global::System.String)(nonNullableArg1.ToString() + nonNullableArg2.ToString());
  }
  private global::System.String Nullable(global::System.Object? nullableArg1, global::System.Object? nullableArg2)
  {
    return (global::System.String)(nullableArg1?.ToString() + nullableArg2!.ToString());
  }
}