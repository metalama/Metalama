internal class Targets
{
  [Aspect]
  private class BaseClass<T>
  {
    private T M(T a)
    {
      global::System.Console.WriteLine("Overridden!");
      return a;
    }
  }
  private class DerivedClass : BaseClass<int>
  {
    private void N()
    {
      global::System.Console.WriteLine("Overridden!");
      return;
    }
  }
}