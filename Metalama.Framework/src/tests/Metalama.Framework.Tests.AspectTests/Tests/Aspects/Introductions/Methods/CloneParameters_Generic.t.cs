[Copy]
public class Target
{
  public void Source<T, U>(List<T> items, T[] array, U? extra, IComparer<T>? comparer, Dictionary<T, U[]> map)
    where T : IComparer<U> where U : class
  {
  }
  public void Copy<T, U>(global::System.Collections.Generic.List<T> items, T[] array, U? extra, global::System.Collections.Generic.IComparer<T>? comparer, global::System.Collections.Generic.Dictionary<T, U[]> map)
    where T : global::System.Collections.Generic.IComparer<U> where U : class
  {
    global::System.Console.WriteLine($"items: {typeof(global::System.Collections.Generic.List<T>)}");
    global::System.Console.WriteLine($"array: {typeof(T[])}");
    global::System.Console.WriteLine($"extra: {typeof(U)}");
    global::System.Console.WriteLine($"comparer: {typeof(global::System.Collections.Generic.IComparer<T>)}");
    global::System.Console.WriteLine($"map: {typeof(global::System.Collections.Generic.Dictionary<T, U[]>)}");
  }
}