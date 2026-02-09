internal class TargetCode
{
  // Method WITH a matching parameter - should resolve to token?.ToLower().
  [Aspect]
  private string? MethodWithToken(string token)
  {
    var tokenParameters = token?.ToLower();
    return (global::System.String? )tokenParameters;
  }
  // Method WITHOUT a matching parameter - should resolve to ((dynamic)null)?.ToLower() since
  // the compile-time expression is null. The chain is preserved to maintain the correct result type.
  [Aspect]
  private string? MethodWithoutToken(int x)
  {
    var tokenParameters = ((dynamic)null)?.ToLower();
    return (global::System.String? )tokenParameters;
  }
}
