internal class TargetClass
{
  [Override]
  public event EventHandler OverriddenEvent
  {
    add
    {
      global::System.Console.WriteLine("This is the overridden method.");
      MethodWithCallerMemberName(42);
      MethodWithCallerMemberName(42, y: 27);
      MethodWithCallerMemberName(42, name1: "foo", y: 27);
      MethodWithCallerMemberName(42, "foo", 27);
      MethodWithCallerMemberName(42, "foo", 27, "bar");
      return;
    }
    remove
    {
      global::System.Console.WriteLine("This is the overridden method.");
      MethodWithCallerMemberName(42);
      MethodWithCallerMemberName(42, y: 27);
      MethodWithCallerMemberName(42, name1: "foo", y: 27);
      MethodWithCallerMemberName(42, "foo", 27);
      MethodWithCallerMemberName(42, "foo", 27, "bar");
      return;
    }
  }
  public void MethodWithCallerMemberName(int x, [CallerMemberName] string name1 = "", int y = 0, [CallerMemberName] string name2 = "")
  {
  }
}