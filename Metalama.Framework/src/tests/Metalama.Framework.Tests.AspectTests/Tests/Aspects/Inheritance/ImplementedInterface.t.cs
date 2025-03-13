internal class Targets
{
  [Aspect]
  private interface I
  {
  }
  private class DerivedClass : I
  {
    private void N()
    {
      global::System.Console.WriteLine("Overridden!");
      return;
    }
  }
}