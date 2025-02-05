internal class TargetCode
{
  [Aspect]
  public IEnumerable<int> Enumerable
  {
    get
    {
      global::System.Console.WriteLine("Starting get_Enumerable");
      foreach (var item in this.Enumerable_Source)
      {
        global::System.Console.WriteLine($" Intercepting {item}");
        yield return item;
      }
      global::System.Console.WriteLine("Ending get_Enumerable");
    }
  }
  private IEnumerable<int> Enumerable_Source
  {
    get
    {
      Console.WriteLine("Yield 1");
      yield return 1;
      Console.WriteLine("Yield 2");
      yield return 2;
      Console.WriteLine("Yield 3");
      yield return 3;
    }
  }
  [Aspect]
  public IEnumerator<int> Enumerator
  {
    get
    {
      global::System.Console.WriteLine("Starting get_Enumerator");
      var enumerator = this.Enumerator_Source;
      while (enumerator.MoveNext())
      {
        global::System.Console.WriteLine($" Intercepting {enumerator.Current}");
        yield return enumerator.Current;
      }
      global::System.Console.WriteLine("Ending get_Enumerator");
    }
  }
  private IEnumerator<int> Enumerator_Source
  {
    get
    {
      Console.WriteLine("Yield 1");
      yield return 1;
      Console.WriteLine("Yield 2");
      yield return 2;
      Console.WriteLine("Yield 3");
      yield return 3;
    }
  }
  [Aspect]
  public IEnumerable OldEnumerable
  {
    get
    {
      global::System.Console.WriteLine("Starting get_OldEnumerable");
      foreach (var item in this.OldEnumerable_Source)
      {
        global::System.Console.WriteLine($" Intercepting {item}");
        yield return item;
      }
      global::System.Console.WriteLine("Ending get_OldEnumerable");
    }
  }
  private IEnumerable OldEnumerable_Source
  {
    get
    {
      Console.WriteLine("Yield 1");
      yield return 1;
      Console.WriteLine("Yield 2");
      yield return 2;
      Console.WriteLine("Yield 3");
      yield return 3;
    }
  }
  [Aspect]
  public IEnumerator OldEnumerator
  {
    get
    {
      global::System.Console.WriteLine("Starting get_OldEnumerator");
      var enumerator = this.OldEnumerator_Source;
      while (enumerator.MoveNext())
      {
        global::System.Console.WriteLine($" Intercepting {enumerator.Current}");
        yield return enumerator.Current;
      }
      global::System.Console.WriteLine("Ending get_OldEnumerator");
    }
  }
  private IEnumerator OldEnumerator_Source
  {
    get
    {
      Console.WriteLine("Yield 1");
      yield return 1;
      Console.WriteLine("Yield 2");
      yield return 2;
      Console.WriteLine("Yield 3");
      yield return 3;
    }
  }
}