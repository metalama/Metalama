[Test]
public class TestClass
{
  public IEnumerable Enumerable(string text)
  {
    global::System.Console.WriteLine($"Advice");
    if (text is null)
    {
      throw new global::System.ArgumentNullException("text");
    }
    yield return "Hello";
    yield return text;
  }
  public IEnumerator Enumerator(string text)
  {
    global::System.Console.WriteLine($"Advice");
    if (text is null)
    {
      throw new global::System.ArgumentNullException("text");
    }
    yield return "Hello";
    yield return text;
  }
  public IEnumerable<string> EnumerableT(string text)
  {
    global::System.Console.WriteLine($"Advice");
    if (text is null)
    {
      throw new global::System.ArgumentNullException("text");
    }
    yield return "Hello";
    yield return text;
  }
  public IEnumerator<string> EnumeratorT(string text)
  {
    global::System.Console.WriteLine($"Advice");
    if (text is null)
    {
      throw new global::System.ArgumentNullException("text");
    }
    yield return "Hello";
    yield return text;
  }
}