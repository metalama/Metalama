internal class TargetCode
{
  [Aspect]
  public IEnumerable<int> Enumerable
  {
    get
    {
      global::System.Console.WriteLine("Before get_Enumerable");
      var result = global::Metalama.Framework.RunTime.RunTimeAspectHelper.Buffer(this.Enumerable_Source);
      global::System.Console.WriteLine("After get_Enumerable");
      return (global::System.Collections.Generic.IEnumerable<global::System.Int32>)result;
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
      global::System.Console.WriteLine("Before get_Enumerator");
      var result = global::Metalama.Framework.RunTime.RunTimeAspectHelper.Buffer(this.Enumerator_Source);
      global::System.Console.WriteLine("After get_Enumerator");
      return (global::System.Collections.Generic.IEnumerator<global::System.Int32>)result;
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
      global::System.Console.WriteLine("Before get_OldEnumerable");
      var result = global::Metalama.Framework.RunTime.RunTimeAspectHelper.Buffer(this.OldEnumerable_Source);
      global::System.Console.WriteLine("After get_OldEnumerable");
      return (global::System.Collections.IEnumerable)result;
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
      global::System.Console.WriteLine("Before get_OldEnumerator");
      var result = global::Metalama.Framework.RunTime.RunTimeAspectHelper.Buffer(this.OldEnumerator_Source);
      global::System.Console.WriteLine("After get_OldEnumerator");
      return (global::System.Collections.IEnumerator)result;
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
