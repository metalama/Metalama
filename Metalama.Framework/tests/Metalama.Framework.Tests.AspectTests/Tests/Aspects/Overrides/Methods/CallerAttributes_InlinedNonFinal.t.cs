internal class TargetClass
{
  [Override]
  public void OverriddenMethod()
  {
    this.OverriddenMethod_Override();
    global::System.Console.WriteLine("This is the overridden method.");
    this.OverriddenMethod_Override();
    return;
  }
  private void OverriddenMethod_Override()
  {
    global::System.Console.WriteLine("This is the overridden method.");
    MethodWithCallerMemberName(42, name1: "OverriddenMethod", name2: "OverriddenMethod");
    MethodWithCallerMemberName(42, y: 27, name1: "OverriddenMethod", name2: "OverriddenMethod");
    MethodWithCallerMemberName(42, name1: "foo", y: 27, name2: "OverriddenMethod");
    MethodWithCallerMemberName(42, "foo", 27, name2: "OverriddenMethod");
    MethodWithCallerMemberName(42, "foo", 27, "bar");
    return;
  }
  public void MethodWithCallerMemberName(int x, [CallerMemberName] string name1 = "", int y = 0, [CallerMemberName] string name2 = "")
  {
  }
}