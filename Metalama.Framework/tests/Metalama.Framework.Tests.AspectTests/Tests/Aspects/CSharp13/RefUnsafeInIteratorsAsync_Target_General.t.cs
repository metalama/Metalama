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
    try
    {
      var result = global::Metalama.Framework.RunTime.RunTimeAspectHelper.Buffer(this.Iterator_Source());
      global::System.Console.WriteLine($"Target.Iterator() succeeded with result {result}.");
      return (global::System.Collections.Generic.IEnumerable<global::System.Int32>)result;
    }
    catch (global::System.Exception ex)
    {
      global::System.Console.WriteLine($"Target.Iterator() failed with exception {ex}.");
      throw;
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
// async iterator would generate invalid code, see #31108
}