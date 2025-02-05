[Test]
public class TestClass
{
  public string DoSomething(string text)
  {
    if (text is null)
    {
      throw new global::System.ArgumentNullException("text");
    }
    Console.WriteLine("Hello");
    return null !;
  }
  public async Task DoSomethingAsync(string text)
  {
    if (text is null)
    {
      throw new global::System.ArgumentNullException("text");
    }
    await Task.Yield();
    Console.WriteLine("Hello");
  }
  public async Task<string> DoSomethingAsyncT(string text)
  {
    if (text is null)
    {
      throw new global::System.ArgumentNullException("text");
    }
    await Task.Yield();
    Console.WriteLine("Hello");
    return null !;
  }
  public async void DoSomethingAsyncVoid(string text)
  {
    if (text is null)
    {
      throw new global::System.ArgumentNullException("text");
    }
    await Task.Yield();
    Console.WriteLine("Hello");
  }
}