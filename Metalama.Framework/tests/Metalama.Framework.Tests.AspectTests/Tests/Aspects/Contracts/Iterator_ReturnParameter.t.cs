[Test]
public class TestClass
{
  public IEnumerable Enumerable(string text)
  {
    var returnValue = global::Metalama.Framework.RunTime.RunTimeAspectHelper.Buffer(this.Enumerable_Source(text));
    global::System.Console.WriteLine($"Advice");
    foreach (var item in returnValue)
    {
      if (item is null)
      {
        throw new global::System.ArgumentNullException("<return>");
      }
    }
    return returnValue;
  }
  private IEnumerable Enumerable_Source(string text)
  {
    yield return "Hello";
    yield return text;
  }
  public IEnumerator Enumerator(string text)
  {
    var bufferedEnumerator = global::Metalama.Framework.RunTime.RunTimeAspectHelper.Buffer(this.Enumerator_Source(text));
    var returnValue = bufferedEnumerator;
    global::System.Console.WriteLine($"Advice");
    while (returnValue.MoveNext())
    {
      if (returnValue.Current is null)
      {
        throw new global::System.ArgumentNullException("<return>");
      }
    }
    while (bufferedEnumerator.MoveNext())
    {
      yield return bufferedEnumerator.Current;
    }
  }
  private IEnumerator Enumerator_Source(string text)
  {
    yield return "Hello";
    yield return text;
  }
  public IEnumerable<string> EnumerableT(string text)
  {
    var returnValue = global::Metalama.Framework.RunTime.RunTimeAspectHelper.Buffer(this.EnumerableT_Source(text));
    global::System.Console.WriteLine($"Advice");
    foreach (var item in returnValue)
    {
      if (item is null)
      {
        throw new global::System.ArgumentNullException("<return>");
      }
    }
    return returnValue;
  }
  private IEnumerable<string> EnumerableT_Source(string text)
  {
    yield return "Hello";
    yield return text;
  }
  public IEnumerator<string> EnumeratorT(string text)
  {
    var bufferedEnumerator = global::Metalama.Framework.RunTime.RunTimeAspectHelper.Buffer(this.EnumeratorT_Source(text));
    var returnValue = bufferedEnumerator;
    global::System.Console.WriteLine($"Advice");
    while (returnValue.MoveNext())
    {
      if (returnValue.Current is null)
      {
        throw new global::System.ArgumentNullException("<return>");
      }
    }
    while (bufferedEnumerator.MoveNext())
    {
      yield return bufferedEnumerator.Current;
    }
  }
  private IEnumerator<string> EnumeratorT_Source(string text)
  {
    yield return "Hello";
    yield return text;
  }
}