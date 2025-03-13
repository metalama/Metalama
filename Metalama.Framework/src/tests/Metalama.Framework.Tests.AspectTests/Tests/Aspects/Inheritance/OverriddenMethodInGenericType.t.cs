internal class Targets
{
  private class BaseClass<T>
  {
    [Aspect]
    public virtual T M(T a)
    {
      global::System.Console.WriteLine("Overridden!");
      return a;
    }
  }
  private class DerivedClass : BaseClass<int>
  {
    public override int M(int a)
    {
      global::System.Console.WriteLine("Overridden!");
      return a + 1;
    }
  }
}