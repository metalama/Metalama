internal class TargetClass
{
  [Override]
  public static int operator -(TargetClass x)
  {
    global::System.Console.WriteLine("This is the overridden method.");
    MethodWithCallerMemberName(42);
    MethodWithCallerMemberName(42, y: 27);
    MethodWithCallerMemberName(42, name1: "foo", y: 27);
    MethodWithCallerMemberName(42, "foo", 27);
    MethodWithCallerMemberName(42, "foo", 27, "bar");
    return 42;
  }
  [Override]
  public static implicit operator int (TargetClass x)
  {
    global::System.Console.WriteLine("This is the overridden method.");
    MethodWithCallerMemberName(42);
    MethodWithCallerMemberName(42, y: 27);
    MethodWithCallerMemberName(42, name1: "foo", y: 27);
    MethodWithCallerMemberName(42, "foo", 27);
    MethodWithCallerMemberName(42, "foo", 27, "bar");
    return 42;
  }
  public static void MethodWithCallerMemberName(int x, [CallerMemberName] string name1 = "", int y = 0, [CallerMemberName] string name2 = "")
  {
  }
}