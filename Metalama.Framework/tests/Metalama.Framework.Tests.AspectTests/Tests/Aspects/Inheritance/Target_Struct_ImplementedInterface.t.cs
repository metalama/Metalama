internal struct Targets
{
  [Aspect]
  private interface I
  {
  }
  private struct DerivedStruct : I
  {
    private void N()
    {
      global::System.Console.WriteLine("Overridden!");
      return;
    }
  }
}