internal class Targets
{
  private interface I<T>
  {
    [Aspect]
    void M(T x);
  }
  private class C : I<int>
  {
    public void M(int x)
    {
      global::System.Console.WriteLine("Overridden!");
      return;
    }
    // This one should not be transformed.
    public void M(string x)
    {
    }
  }
}