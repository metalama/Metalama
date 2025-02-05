[Introduction]
internal class TargetClass
{
  public global::System.String IntroducedField = (global::System.String)Foo();
  public static global::System.String IntroducedField_Static = (global::System.String)Foo();
  private static global::System.String Foo()
  {
    return (global::System.String)"foo";
  }
}