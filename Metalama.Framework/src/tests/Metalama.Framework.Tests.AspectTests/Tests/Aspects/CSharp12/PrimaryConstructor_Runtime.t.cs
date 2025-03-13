public class C(int x) : B(x)
{
  [TheAspect]
  public void M()
  {
    global::System.Console.WriteLine("Aspect");
    _ = new C(42);
  }
}