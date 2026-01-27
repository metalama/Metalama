[MyTypeAspect]
internal static class C
{
  extension(string test)
  {
    public void SimpleMethod()
    {
      global::System.Console.WriteLine($"Contract on receiver: {test}, Member: C.extension(string).SimpleMethod()");
      Console.WriteLine("Simple method.");
    }
    public int MethodWithReturn()
    {
      global::System.Console.WriteLine($"Contract on receiver: {test}, Member: C.extension(string).MethodWithReturn()");
      Console.WriteLine("Method with return.");
      return 42;
    }
    public void MethodWithParams(int x, string y)
    {
      global::System.Console.WriteLine($"Contract on receiver: {test}, Member: C.extension(string).MethodWithParams(int, string)");
      Console.WriteLine($"Method with params: {x}, {y}");
    }
    public async Task AsyncMethod()
    {
      global::System.Console.WriteLine($"Contract on receiver: {test}, Member: C.extension(string).AsyncMethod()");
      Console.WriteLine("Async method.");
      await Task.Yield();
    }
    public async Task<int> AsyncMethodWithReturn()
    {
      global::System.Console.WriteLine($"Contract on receiver: {test}, Member: C.extension(string).AsyncMethodWithReturn()");
      Console.WriteLine("Async method with return.");
      await Task.Yield();
      return 42;
    }
    public IEnumerable<int> IteratorMethod()
    {
      global::System.Console.WriteLine($"Contract on receiver: {test}, Member: C.extension(string).IteratorMethod()");
      Console.WriteLine("Iterator method.");
      yield return 1;
      yield return 2;
    }
  }
}