[MyTypeAspect]
internal static class C
{
  extension(string test)
  {
    public void Method()
    {
      if (test == null)
      {
        throw new global::System.ArgumentNullException();
      }
      global::System.Console.WriteLine($"NotNull contract passed: {test}");
      if (((string)test).Length == 0)
      {
        throw new global::System.ArgumentException("String cannot be empty.");
      }
      global::System.Console.WriteLine($"Length contract passed: {test}");
      Console.WriteLine("Method.");
    }
    public int Property
    {
      get
      {
        if (test == null)
        {
          throw new global::System.ArgumentNullException();
        }
        global::System.Console.WriteLine($"NotNull contract passed: {test}");
        if (((string)test).Length == 0)
        {
          throw new global::System.ArgumentException("String cannot be empty.");
        }
        global::System.Console.WriteLine($"Length contract passed: {test}");
        Console.WriteLine("Property get.");
        return 42;
      }
      set
      {
        if (test == null)
        {
          throw new global::System.ArgumentNullException();
        }
        global::System.Console.WriteLine($"NotNull contract passed: {test}");
        if (((string)test).Length == 0)
        {
          throw new global::System.ArgumentException("String cannot be empty.");
        }
        global::System.Console.WriteLine($"Length contract passed: {test}");
        Console.WriteLine("Property set.");
      }
    }
  }
}