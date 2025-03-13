internal class Targets
{
  private interface I
  {
    [Aspect]
    void M();
  }
  private class C : I
  {
    public void M()
    {
      global::System.Console.WriteLine("Overridden!");
      return;
    }
  }
}