[MyAspect]
internal class C
{
  public void SomeMethod(global::System.Object nonNullable, global::System.Object? nullable)
  {
    // With original nullability
    _ = nullable!.ToString();
    _ = nonNullable.ToString();
    // With inverse nullability
    _ = nullable.ToString();
    _ = nonNullable!.ToString();
  }
}