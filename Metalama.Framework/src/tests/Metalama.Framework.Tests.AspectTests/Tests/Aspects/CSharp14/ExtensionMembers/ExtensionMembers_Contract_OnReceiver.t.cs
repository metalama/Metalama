[MyTypeAspect]
internal static class C
{
  extension([MyContractAspect] int test)
  {
    public int Property
    {
      get
      {
        Console.WriteLine("Original.");
        return 42;
      }
      set
      {
        Console.WriteLine("Original.");
      }
    }
  }
  extension(string test)
  {
    public int Property
    {
      get
      {
        global::System.Console.WriteLine($"Contract on: {test}");
        Console.WriteLine("Original.");
        return 42;
      }
      set
      {
        global::System.Console.WriteLine($"Contract on: {test}");
        Console.WriteLine("Original.");
      }
    }
  }
}
