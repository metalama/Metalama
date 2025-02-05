internal class DerivedClass : BaseClass
{
  private int Method3(int a)
  {
    global::System.Console.WriteLine("overridden");
    return a;
  }
  private string Method4(string s)
  {
    global::System.Console.WriteLine("overridden");
    return s;
  }
}