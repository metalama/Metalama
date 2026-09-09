[MyTypeAspect]
internal static class C
{
  extension(string test)
  {
    // Indexer with both accessors.
    public int this[int index]
    {
      get
      {
        global::System.Console.WriteLine($"Contract on receiver: {test}, Member: C.extension(string).this[int].get");
        Console.WriteLine("ReadWriteIndexer get.");
        return 42;
      }
      set
      {
        global::System.Console.WriteLine($"Contract on receiver: {test}, Member: C.extension(string).this[int].set");
        Console.WriteLine($"ReadWriteIndexer set: {value}");
      }
    }
    // Indexer with a getter only.
    public int this[string index]
    {
      get
      {
        global::System.Console.WriteLine($"Contract on receiver: {test}, Member: C.extension(string).this[string].get");
        Console.WriteLine("ReadOnlyIndexer get.");
        return 42;
      }
    }
  }
}