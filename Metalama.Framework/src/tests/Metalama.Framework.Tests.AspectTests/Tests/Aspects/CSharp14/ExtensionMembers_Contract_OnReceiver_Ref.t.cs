[MyTypeAspect]
internal static class C
{
  extension(ref MyStruct test)
  {
    public void Method()
    {
      Console.WriteLine("Method.");
    }
    private void Method_MyTypeAspect()
    {
      global::System.Console.WriteLine($"Contract on receiver: {test}");
      test.Method();
      global::System.Console.WriteLine($"Contract on receiver: {test}");
    }
    public int Property
    {
      get
      {
        Console.WriteLine("Property get.");
        return 42;
      }
      set
      {
        Console.WriteLine("Property set.");
      }
    }
    private global::System.Int32 Property_MyTypeAspect1
    {
      get
      {
        global::System.Console.WriteLine($"Contract on receiver: {test}");
        var returnValue = test.Property;
        global::System.Console.WriteLine($"Contract on receiver: {test}");
        return returnValue;
      }
      set
      {
        global::System.Console.WriteLine($"Contract on receiver: {test}");
        test.Property = value;
        global::System.Console.WriteLine($"Contract on receiver: {test}");
      }
    }
    private global::System.Int32 Property_MyTypeAspect
    {
      get
      {
        var returnValue = test.Property;
        return returnValue;
      }
      set
      {
        test.Property = value;
      }
    }
  }
}