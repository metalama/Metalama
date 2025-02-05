internal class TargetClass
{
  [Override]
  public int OverriddenProperty
  {
    get
    {
      _ = this.OverriddenProperty_Override;
      global::System.Console.WriteLine("This is the overridden method.");
      return this.OverriddenProperty_Override;
    }
    set
    {
      this.OverriddenProperty_Override = value;
      global::System.Console.WriteLine("This is the overridden method.");
      this.OverriddenProperty_Override = value;
      return;
    }
  }
  private global::System.Int32 OverriddenProperty_Override
  {
    get
    {
      global::System.Console.WriteLine("This is the overridden method.");
      MethodWithCallerMemberName(42, name1: "OverriddenProperty", name2: "OverriddenProperty");
      MethodWithCallerMemberName(42, y: 27, name1: "OverriddenProperty", name2: "OverriddenProperty");
      MethodWithCallerMemberName(42, name1: "foo", y: 27, name2: "OverriddenProperty");
      MethodWithCallerMemberName(42, "foo", 27, name2: "OverriddenProperty");
      MethodWithCallerMemberName(42, "foo", 27, "bar");
      return 42;
    }
    set
    {
      global::System.Console.WriteLine("This is the overridden method.");
      MethodWithCallerMemberName(42, name1: "OverriddenProperty", name2: "OverriddenProperty");
      MethodWithCallerMemberName(42, y: 27, name1: "OverriddenProperty", name2: "OverriddenProperty");
      MethodWithCallerMemberName(42, name1: "foo", y: 27, name2: "OverriddenProperty");
      MethodWithCallerMemberName(42, "foo", 27, name2: "OverriddenProperty");
      MethodWithCallerMemberName(42, "foo", 27, "bar");
      return;
    }
  }
  public void MethodWithCallerMemberName(int x, [CallerMemberName] string name1 = "", int y = 0, [CallerMemberName] string name2 = "")
  {
  }
}