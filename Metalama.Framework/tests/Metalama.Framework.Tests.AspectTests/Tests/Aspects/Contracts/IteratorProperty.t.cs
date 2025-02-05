[Test]
public class TestClass
{
  public IEnumerable<string> Enumerable
  {
    get
    {
      yield return "Hello";
    }
    set
    {
      if (value is null)
      {
        throw new global::System.ArgumentNullException();
      }
    }
  }
  public IEnumerator<string> Enumerator
  {
    get
    {
      yield return "Hello";
    }
    set
    {
      if (value is null)
      {
        throw new global::System.ArgumentNullException();
      }
    }
  }
}