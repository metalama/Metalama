[Introduction]
internal class TargetClass
{
  public int ExistingMethod(int x)
  {
    return x;
  }
  public global::System.Int32 ExistingMethod()
  {
    global::System.Console.WriteLine("This is introduced method.");
    return (global::System.Int32)42;
  }
}