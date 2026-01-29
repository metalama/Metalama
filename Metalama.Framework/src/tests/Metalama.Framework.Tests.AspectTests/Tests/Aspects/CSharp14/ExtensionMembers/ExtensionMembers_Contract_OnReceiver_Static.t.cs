[MyTypeAspect]
internal static class C
{
  extension(string test)
  {
    // Instance method - should have contract
    public void InstanceMethod()
    {
      global::System.Console.WriteLine($"Contract on receiver: {test}");
      Console.WriteLine("Instance method.");
    }
    // Static method - should NOT have contract (no access to receiver)
    public static void StaticMethod()
    {
      Console.WriteLine("Static method.");
    }
    // Instance property - should have contract
    public int InstanceProperty
    {
      get
      {
        global::System.Console.WriteLine($"Contract on receiver: {test}");
        Console.WriteLine("Instance property get.");
        return 42;
      }
      set
      {
        global::System.Console.WriteLine($"Contract on receiver: {test}");
        Console.WriteLine("Instance property set.");
      }
    }
    // Static property - should NOT have contract
    public static int StaticProperty
    {
      get
      {
        Console.WriteLine("Static property get.");
        return 42;
      }
      set
      {
        Console.WriteLine("Static property set.");
      }
    }
  }
}