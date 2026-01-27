[MyTypeAspect]
internal static class C
{
  extension(ref MyStruct test)
  {
    public void Method()
    {
      global::System.Console.WriteLine($"Contract on receiver: {test}");
      Console.WriteLine("Method.");
    }
    public int Property
    {
      get
      {
        global::System.Console.WriteLine($"Contract on receiver: {test}");
        Console.WriteLine("Property get.");
        return 42;
      }
      set
      {
        global::System.Console.WriteLine($"Contract on receiver: {test}");
        Console.WriteLine("Property set.");
      }
    }
  }
}