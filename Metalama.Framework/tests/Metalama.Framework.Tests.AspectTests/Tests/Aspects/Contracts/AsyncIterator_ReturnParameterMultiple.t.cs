[Test]
public class TestClass
{
  public async IAsyncEnumerable<string> AsyncEnumerable(string text)
  {
    var returnValue = this.AsyncEnumerable_Source(text);
    global::System.Console.WriteLine("Advice 1");
    await foreach (var item in (global::System.Collections.Generic.IAsyncEnumerable<global::System.Object?>)returnValue)
    {
      if (item is null)
      {
        throw new global::System.ArgumentNullException("<return>");
      }
    }
    global::System.Console.WriteLine("Advice 2");
    await foreach (var item_1 in (global::System.Collections.Generic.IAsyncEnumerable<global::System.Object?>)returnValue)
    {
      if (item_1 is null)
      {
        throw new global::System.ArgumentNullException("<return>");
      }
    }
    await foreach (var returnItem in returnValue)
    {
      yield return returnItem;
    }
  }
  private async IAsyncEnumerable<string> AsyncEnumerable_Source(string text)
  {
    await Task.Yield();
    yield return "Hello";
    await Task.Yield();
    yield return text;
  }
  public async IAsyncEnumerator<string> AsyncEnumerator(string text)
  {
    var bufferedEnumerator = (await global::Metalama.Framework.RunTime.RunTimeAspectHelper.BufferAsync(this.AsyncEnumerator_Source(text)));
    var returnValue = bufferedEnumerator;
    global::System.Console.WriteLine("Advice 1");
    while (await returnValue.MoveNextAsync())
    {
      if (returnValue.Current is null)
      {
        throw new global::System.ArgumentNullException("<return>");
      }
    }
    returnValue = bufferedEnumerator;
    global::System.Console.WriteLine("Advice 2");
    while (await returnValue.MoveNextAsync())
    {
      if (returnValue.Current is null)
      {
        throw new global::System.ArgumentNullException("<return>");
      }
    }
    while (await bufferedEnumerator.MoveNextAsync())
    {
      yield return bufferedEnumerator.Current;
    }
  }
  private async IAsyncEnumerator<string> AsyncEnumerator_Source(string text)
  {
    await Task.Yield();
    yield return "Hello";
    await Task.Yield();
    yield return text;
  }
}