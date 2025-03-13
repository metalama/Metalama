internal class EmptyOverrideFieldOrPropertyExample
{
  [Aspect]
  private IEnumerable<int> M()
  {
    global::System.Console.WriteLine("enumerable");
    return new[]
    {
      42
    };
  }
}