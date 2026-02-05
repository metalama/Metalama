internal class TargetCode
{
  [Aspect]
  private async IAsyncEnumerable<int> AsyncIteratorMethod(int count)
  {
    global::System.Console.WriteLine("Before");
    await foreach (var item in this.AsyncIteratorMethod_Source(count))
    {
      yield return item;
    }
  }
  private async IAsyncEnumerable<int> AsyncIteratorMethod_Source(int count)
  {
    for (var i = 0; i < count; i++)
    {
      await Task.Delay(1);
      yield return i;
    }
  }
}
