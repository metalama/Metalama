[MyTypeAspect]
internal static class C
{
  extension(TestClass test)
  {
    // Instance method - should have contract
    public void InstanceMethod()
    {
      global::System.Console.WriteLine($"Contract on receiver: {test}");
      Console.WriteLine("Instance method.");
    }
    // Operators are static - should NOT have contract
    public static TestClass operator +(TestClass a, TestClass b)
    {
      return new TestClass
      {
        Value = a.Value + b.Value
      };
    }
    public static TestClass operator -(TestClass a, TestClass b)
    {
      return new TestClass
      {
        Value = a.Value - b.Value
      };
    }
    public static TestClass operator *(TestClass a, int scalar)
    {
      return new TestClass
      {
        Value = a.Value * scalar
      };
    }
    // Unary operators
    public static TestClass operator ++(TestClass a)
    {
      return new TestClass
      {
        Value = a.Value + 1
      };
    }
    public static TestClass operator --(TestClass a)
    {
      return new TestClass
      {
        Value = a.Value - 1
      };
    }
    public static bool operator ==(TestClass a, TestClass b)
    {
      return a.Value == b.Value;
    }
    public static bool operator !=(TestClass a, TestClass b)
    {
      return a.Value != b.Value;
    }
  }
}