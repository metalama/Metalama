internal class TargetCode
{
  [Aspect]
  private class NullableTarget
  {
    private global::System.String? Introduced(global::System.String? a)
    {
      return (global::System.String? )a?.ToString();
    }
  }
#nullable disable
  [Aspect]
  private class NonNullableTarget
  {
    private global::System.String Introduced(global::System.String a)
    {
      return (global::System.String)a?.ToString();
    }
  }
}