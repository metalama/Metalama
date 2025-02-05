internal struct Targets
{
  private interface I
  {
    [Aspect]
    void M();
  }
  private struct S : I
  {
    public void M()
    {
      global::System.Console.WriteLine("Overridden!");
      return;
    }
  }
}