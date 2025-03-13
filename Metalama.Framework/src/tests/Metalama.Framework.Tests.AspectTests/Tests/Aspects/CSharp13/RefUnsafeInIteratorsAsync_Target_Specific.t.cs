class Target
{
  [TheAspect]
  private async Task Async()
  {
    global::System.Console.WriteLine("Entering Target.Async().");
    try
    {
      await this.Async_Source();
      object result = null;
      global::System.Console.WriteLine($"Target.Async() succeeded with result {result}.");
      return;
    }
    catch (global::System.Exception ex)
    {
      global::System.Console.WriteLine($"Target.Async() failed with exception {ex}.");
      throw;
    }
  }
  private async Task Async_Source()
  {
    await Task.Yield();
    // unsafe
    unsafe
    {
      fixed (int* p = new int[1])
      {
      }
    }
    // ref
    ref int r = ref (new int[1])[0];
    // ref struct
    Span<int> s = stackalloc int[1];
    await Task.Yield();
  }
  [TheAspect]
  private IEnumerable<int> Iterator()
  {
    global::System.Console.WriteLine("Entering Target.Iterator().");
    foreach (var item in this.Iterator_Source())
    {
      global::System.Console.WriteLine($"Target.Iterator() yielded {item}.");
      yield return item;
    }
  }
  private IEnumerable<int> Iterator_Source()
  {
    yield return 1;
    // unsafe
    unsafe
    {
      fixed (int* p = new int[1])
      {
      }
    }
    // ref
    ref int r = ref (new int[1])[0];
    // ref struct
    Span<int> s = stackalloc int[1];
    yield return 2;
  }
  [TheAspect]
  private async IAsyncEnumerable<int> AsyncIterator()
  {
    global::System.Console.WriteLine("Entering Target.AsyncIterator().");
    await foreach (var item in this.AsyncIterator_Source())
    {
      global::System.Console.WriteLine($"Target.AsyncIterator() yielded {item}.");
      yield return item;
    }
  }
  private async IAsyncEnumerable<int> AsyncIterator_Source()
  {
    await Task.Yield();
    yield return 1;
    // unsafe
    unsafe
    {
      fixed (int* p = new int[1])
      {
      }
    }
    // ref
    ref int r = ref (new int[1])[0];
    // ref struct
    Span<int> s = stackalloc int[1];
    await Task.Yield();
    yield return 2;
  }
}