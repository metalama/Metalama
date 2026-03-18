internal class TargetClass
{
  private event EventHandler? _e1;
  // Without target specifier (should work).
  [MyAspect]
  public event EventHandler? E1
  {
    add
    {
      global::System.Console.WriteLine("Add");
      this._e1 += value;
    }
    remove
    {
      global::System.Console.WriteLine("Remove");
      this._e1 -= value;
    }
  }
  private event EventHandler? _e2;
  // With explicit event target specifier (should also work).
  [event: MyAspect]
  public event EventHandler? E2
  {
    add
    {
      global::System.Console.WriteLine("Add");
      this._e2 += value;
    }
    remove
    {
      global::System.Console.WriteLine("Remove");
      this._e2 -= value;
    }
  }
}
