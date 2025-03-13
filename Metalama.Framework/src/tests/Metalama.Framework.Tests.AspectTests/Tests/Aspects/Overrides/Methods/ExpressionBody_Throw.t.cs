internal partial class Target
{
  [Override]
  public void M1(string m)
  {
    global::System.Console.WriteLine("This is the overriding method.");
    throw new Exception();
    return;
  }
  [Override]
  public int M2(string m)
  {
    global::System.Console.WriteLine("This is the overriding method.");
    throw new Exception();
  }
}