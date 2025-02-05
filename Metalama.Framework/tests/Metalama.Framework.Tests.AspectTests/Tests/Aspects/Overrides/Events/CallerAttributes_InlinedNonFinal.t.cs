internal class TargetClass
{
  [Override]
  public event EventHandler OverriddenEvent
  {
    add
    {
      this.OverriddenEvent_Override += value;
      global::System.Console.WriteLine("This is the overridden method.");
      this.OverriddenEvent_Override += value;
      return;
    }
    remove
    {
      this.OverriddenEvent_Override -= value;
      global::System.Console.WriteLine("This is the overridden method.");
      this.OverriddenEvent_Override -= value;
      return;
    }
  }
  private event global::System.EventHandler OverriddenEvent_Override
  {
    add
    {
      global::System.Console.WriteLine("This is the overridden method.");
      MethodWithCallerMemberName(42, name1: "OverriddenEvent", name2: "OverriddenEvent");
      MethodWithCallerMemberName(42, y: 27, name1: "OverriddenEvent", name2: "OverriddenEvent");
      MethodWithCallerMemberName(42, name1: "foo", y: 27, name2: "OverriddenEvent");
      MethodWithCallerMemberName(42, "foo", 27, name2: "OverriddenEvent");
      MethodWithCallerMemberName(42, "foo", 27, "bar");
      return;
    }
    remove
    {
      global::System.Console.WriteLine("This is the overridden method.");
      MethodWithCallerMemberName(42, name1: "OverriddenEvent", name2: "OverriddenEvent");
      MethodWithCallerMemberName(42, y: 27, name1: "OverriddenEvent", name2: "OverriddenEvent");
      MethodWithCallerMemberName(42, name1: "foo", y: 27, name2: "OverriddenEvent");
      MethodWithCallerMemberName(42, "foo", 27, name2: "OverriddenEvent");
      MethodWithCallerMemberName(42, "foo", 27, "bar");
      return;
    }
  }
  public void MethodWithCallerMemberName(int x, [CallerMemberName] string name1 = "", int y = 0, [CallerMemberName] string name2 = "")
  {
  }
}