internal class TargetClass
{
  [Override]
  public int OverriddenProperty
  {
    get
    {
      _ = this.OverriddenProperty_Source;
      global::System.Console.WriteLine("This is the overridden method.");
      return this.OverriddenProperty_Source;
    }
    set
    {
      this.OverriddenProperty_Source = value;
      global::System.Console.WriteLine("This is the overridden method.");
      this.OverriddenProperty_Source = value;
      return;
    }
  }
  private int OverriddenProperty_Source
  {
    get
    {
      MethodWithCallerMemberName(42, name1: "OverriddenProperty", name2: "OverriddenProperty");
      MethodWithCallerMemberName(42, y: 27, name1: "OverriddenProperty", name2: "OverriddenProperty");
      MethodWithCallerMemberName(42, name1: "foo", y: 27, name2: "OverriddenProperty");
      MethodWithCallerMemberName(42, "foo", 27, name2: "OverriddenProperty");
      MethodWithCallerMemberName(42, "foo", 27, "bar");
      return 42;
    }
    set
    {
      MethodWithCallerMemberName(42, name1: "OverriddenProperty", name2: "OverriddenProperty");
      MethodWithCallerMemberName(42, y: 27, name1: "OverriddenProperty", name2: "OverriddenProperty");
      MethodWithCallerMemberName(42, name1: "foo", y: 27, name2: "OverriddenProperty");
      MethodWithCallerMemberName(42, "foo", 27, name2: "OverriddenProperty");
      MethodWithCallerMemberName(42, "foo", 27, "bar");
    }
  }
  public void MethodWithCallerMemberName(int x, [CallerMemberName] string name1 = "", int y = 0, [CallerMemberName] string name2 = "")
  {
  }
}