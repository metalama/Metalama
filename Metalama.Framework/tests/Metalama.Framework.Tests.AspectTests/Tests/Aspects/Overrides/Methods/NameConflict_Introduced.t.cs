[InnerOverride]
[OuterOverride]
[Introduction]
internal class TargetClass
{
  public global::System.Object IntroducedMethod_ConflictBetweenOverrides()
  {
    var i_1 = 27;
    var i = 42;
    return default(global::System.Object);
  }
  public global::System.Object IntroducedMethod_ConflictWithParameter(global::System.Int32 i)
  {
    var i_2 = 27;
    var i_1 = 42;
    return default(global::System.Object);
  }
}