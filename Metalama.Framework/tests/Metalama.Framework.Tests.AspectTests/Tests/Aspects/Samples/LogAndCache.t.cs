internal class TargetCode
{
  [Log]
  [Cache]
  private static int Add(int a, int b)
  {
    Console.WriteLine("TargetCode.Add(int, int) started.");
    try
    {
      int result_1;
      string cacheKey = string.Format("TargetCode.Add({0}, {1})", new object[] { a, b });
      if (SampleCache.Cache.TryGetValue(cacheKey, out var value))
      {
        Console.WriteLine("Cache hit.");
        result_1 = (int)value;
      }
      else
      {
        Console.WriteLine("Cache miss.");
        int result;
        Console.WriteLine("Thinking...");
        result = a + b;
        SampleCache.Cache.TryAdd(cacheKey, result);
        result_1 = result;
      }
      Console.WriteLine("TargetCode.Add(int, int) succeeded.");
      return result_1;
    }
    catch (Exception e)
    {
      Console.WriteLine("TargetCode.Add(int, int) failed: " + e.Message);
      throw;
    }
  }
}