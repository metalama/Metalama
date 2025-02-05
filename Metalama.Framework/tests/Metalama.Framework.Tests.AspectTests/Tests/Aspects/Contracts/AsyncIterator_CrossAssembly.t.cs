[Test]
public class TestClass
{
  public async IAsyncEnumerable<string> AsyncEnumerable(string text)
  {
    global::System.Console.WriteLine($"Advice");
    if (text is null)
    {
      throw new global::System.ArgumentNullException("text");
    }
    await Task.Yield();
    yield return "Hello";
    await Task.Yield();
    yield return text;
  }
  public async IAsyncEnumerator<string> AsyncEnumerator(string text)
  {
    global::System.Console.WriteLine($"Advice");
    if (text is null)
    {
      throw new global::System.ArgumentNullException("text");
    }
    await Task.Yield();
    yield return "Hello";
    await Task.Yield();
    yield return text;
  }
}