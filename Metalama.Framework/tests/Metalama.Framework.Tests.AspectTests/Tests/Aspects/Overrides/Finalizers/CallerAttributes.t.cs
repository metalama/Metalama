internal class TargetClass
{
  [Override]
  ~TargetClass()
  {
    this.Finalize_Source();
    global::System.Console.WriteLine("This is the overridden method.");
    this.Finalize_Source();
    return;
  }
  private void Finalize_Source()
  {
    MethodWithCallerMemberName(42, name1: "Finalize", name2: "Finalize");
    MethodWithCallerMemberName(42, y: 27, name1: "Finalize", name2: "Finalize");
    MethodWithCallerMemberName(42, name1: "foo", y: 27, name2: "Finalize");
    MethodWithCallerMemberName(42, "foo", 27, name2: "Finalize");
    MethodWithCallerMemberName(42, "foo", 27, "bar");
  }
  public void MethodWithCallerMemberName(int x, [CallerMemberName] string name1 = "", int y = 0, [CallerMemberName] string name2 = "")
  {
  }
}