internal class TargetCode
{
  // Method WITH a matching parameter - should resolve to token?.ToLower().
  [Aspect]
  private string? MethodWithToken(string token)
  {
    var tokenParameters = token?.ToLower();
    return (global::System.String? )tokenParameters;
  }
  // Method WITHOUT a matching parameter - should resolve to (object?)null since
  // the compile-time expression is null and the type cannot be determined.
  [Aspect]
  private string? MethodWithoutToken(int x)
  {
    var tokenParameters = ((global::System.Object? )null);
    return (global::System.String? )tokenParameters;
  }
}
