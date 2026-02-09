internal class TargetCode
{
  // Method WITH a matching parameter - should resolve to token.ToString().
  [Aspect]
  private string? MethodWithToken(string token)
  {
    var tokenParameters = token?.ToString();
    return (global::System.String?)tokenParameters;
  }
  // Method WITHOUT a matching parameter - should resolve to (string?)null.
  [Aspect]
  private string? MethodWithoutToken(int x)
  {
    var tokenParameters = (object?)null?.ToString();
    return (global::System.String?)tokenParameters;
  }
}
