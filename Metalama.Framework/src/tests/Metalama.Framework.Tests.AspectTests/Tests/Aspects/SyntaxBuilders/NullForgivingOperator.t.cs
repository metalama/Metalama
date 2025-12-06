internal class C
{
  [Aspect]
  private int M()
  {
    // Test with nullable expression - should add !
    var nullableString = (object? )GetNullableString()!;
    // Test with null literal - should add !
    var nullLiteral = (object? )null !;
    // Test with default expression - should add !
    var defaultExpr = (object? )default(global::System.String)!;
    // Test with force=true on non-nullable - should add !
    var forced = (object? )"hello"!;
    // Test with non-nullable - should NOT add !
    var nonNullable = (object? )"hello";
    return 0;
  }

  private static string? GetNullableString() => null;
}