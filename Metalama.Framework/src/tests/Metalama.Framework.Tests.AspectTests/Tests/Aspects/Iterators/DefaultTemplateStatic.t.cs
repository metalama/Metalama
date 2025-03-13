internal static class TargetCode
{
  [Aspect]
  public static IEnumerable<int> Enumerable(int a)
  {
    global::System.Console.WriteLine("Before Enumerable");
    var result = global::Metalama.Framework.RunTime.RunTimeAspectHelper.Buffer(global::Metalama.Framework.Tests.AspectTests.Templating.Aspects.Iterators.DefaultTemplateStatic.TargetCode.Enumerable_Source(a));
    global::System.Console.WriteLine("After Enumerable");
    return (global::System.Collections.Generic.IEnumerable<global::System.Int32>)result;
  }
  private static IEnumerable<int> Enumerable_Source(int a)
  {
    Console.WriteLine("Yield 1");
    yield return 1;
    Console.WriteLine("Yield 2");
    yield return 2;
    Console.WriteLine("Yield 3");
    yield return 3;
  }
  [Aspect]
  public static IEnumerator<int> Enumerator(int a)
  {
    global::System.Console.WriteLine("Before Enumerator");
    var result = global::Metalama.Framework.RunTime.RunTimeAspectHelper.Buffer(global::Metalama.Framework.Tests.AspectTests.Templating.Aspects.Iterators.DefaultTemplateStatic.TargetCode.Enumerator_Source(a));
    global::System.Console.WriteLine("After Enumerator");
    return (global::System.Collections.Generic.IEnumerator<global::System.Int32>)result;
  }
  private static IEnumerator<int> Enumerator_Source(int a)
  {
    Console.WriteLine("Yield 1");
    yield return 1;
    Console.WriteLine("Yield 2");
    yield return 2;
    Console.WriteLine("Yield 3");
    yield return 3;
  }
  [Aspect]
  public static IEnumerable OldEnumerable(int a)
  {
    global::System.Console.WriteLine("Before OldEnumerable");
    var result = global::Metalama.Framework.RunTime.RunTimeAspectHelper.Buffer(global::Metalama.Framework.Tests.AspectTests.Templating.Aspects.Iterators.DefaultTemplateStatic.TargetCode.OldEnumerable_Source(a));
    global::System.Console.WriteLine("After OldEnumerable");
    return (global::System.Collections.IEnumerable)result;
  }
  private static IEnumerable OldEnumerable_Source(int a)
  {
    Console.WriteLine("Yield 1");
    yield return 1;
    Console.WriteLine("Yield 2");
    yield return 2;
    Console.WriteLine("Yield 3");
    yield return 3;
  }
  [Aspect]
  public static IEnumerator OldEnumerator(int a)
  {
    global::System.Console.WriteLine("Before OldEnumerator");
    var result = global::Metalama.Framework.RunTime.RunTimeAspectHelper.Buffer(global::Metalama.Framework.Tests.AspectTests.Templating.Aspects.Iterators.DefaultTemplateStatic.TargetCode.OldEnumerator_Source(a));
    global::System.Console.WriteLine("After OldEnumerator");
    return (global::System.Collections.IEnumerator)result;
  }
  private static IEnumerator OldEnumerator_Source(int a)
  {
    Console.WriteLine("Yield 1");
    yield return 1;
    Console.WriteLine("Yield 2");
    yield return 2;
    Console.WriteLine("Yield 3");
    yield return 3;
  }
}