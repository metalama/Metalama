[MyTypeAspect]
internal static class C
{
  extension(string test)
  {
    // Property with both getter and setter
    public int ReadWriteProperty
    {
      get
      {
        global::System.Console.WriteLine($"Contract on receiver: {test}, Member: C.extension(string).ReadWriteProperty.get");
        Console.WriteLine("ReadWriteProperty get.");
        return 42;
      }
      set
      {
        global::System.Console.WriteLine($"Contract on receiver: {test}, Member: C.extension(string).ReadWriteProperty.set");
        Console.WriteLine($"ReadWriteProperty set: {value}");
      }
    }
    // Property with only getter (computed property)
    public int ReadOnlyProperty
    {
      get
      {
        global::System.Console.WriteLine($"Contract on receiver: {test}, Member: C.extension(string).ReadOnlyProperty.get");
        Console.WriteLine("ReadOnlyProperty get.");
        return 42;
      }
    }
    // Property with only setter
    public int WriteOnlyProperty
    {
      set
      {
        global::System.Console.WriteLine($"Contract on receiver: {test}, Member: C.extension(string).WriteOnlyProperty.set");
        Console.WriteLine($"WriteOnlyProperty set: {value}");
      }
    }
  }
}