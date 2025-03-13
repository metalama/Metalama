internal class TargetClass
{
  [Override]
  public event EventHandler OverriddenEvent
  {
    add
    {
      this.OverriddenEvent_Source += value;
      global::System.Console.WriteLine("This is the overridden method.");
      this.OverriddenEvent_Source += value;
      return;
    }
    remove
    {
      this.OverriddenEvent_Source -= value;
      global::System.Console.WriteLine("This is the overridden method.");
      this.OverriddenEvent_Source -= value;
      return;
    }
  }
  private event EventHandler OverriddenEvent_Source
  {
    add
    {
      MethodWithCallerMemberName(42, name1: "OverriddenEvent", name2: "OverriddenEvent");
      MethodWithCallerMemberName(42, y: 27, name1: "OverriddenEvent", name2: "OverriddenEvent");
      MethodWithCallerMemberName(42, name1: "foo", y: 27, name2: "OverriddenEvent");
      MethodWithCallerMemberName(42, "foo", 27, name2: "OverriddenEvent");
      MethodWithCallerMemberName(42, "foo", 27, "bar");
    }
    remove
    {
      MethodWithCallerMemberName(42, name1: "OverriddenEvent", name2: "OverriddenEvent");
      MethodWithCallerMemberName(42, y: 27, name1: "OverriddenEvent", name2: "OverriddenEvent");
      MethodWithCallerMemberName(42, name1: "foo", y: 27, name2: "OverriddenEvent");
      MethodWithCallerMemberName(42, "foo", 27, name2: "OverriddenEvent");
      MethodWithCallerMemberName(42, "foo", 27, "bar");
    }
  }
  public void MethodWithCallerMemberName(int x, [CallerMemberName] string name1 = "", int y = 0, [CallerMemberName] string name2 = "")
  {
  }
}