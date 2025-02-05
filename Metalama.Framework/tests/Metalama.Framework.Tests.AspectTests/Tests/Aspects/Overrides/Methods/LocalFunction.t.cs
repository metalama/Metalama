internal class TargetClass
{
  [Override]
  public int Foo(int x)
  {
    global::System.Console.WriteLine("This is overridden method.");
    return (global::System.Int32)Quz();
    int Quz()
    {
      global::System.Int32 x_1;
      x_1 = Bar(Bar(x));
      int Bar(int x)
      {
        return x + 1;
      }
      return (global::System.Int32)(x_1 + 1);
    }
  }
}