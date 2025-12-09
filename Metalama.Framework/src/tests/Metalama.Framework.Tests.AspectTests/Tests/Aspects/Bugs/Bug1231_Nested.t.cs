public class MyClass<T>
{
  [Log]
  public Dictionary<string, List<T?>> GetValues()
  {
    global::System.Console.WriteLine($"Return type: {typeof(global::System.Collections.Generic.Dictionary<global::System.String, global::System.Collections.Generic.List<T>>)}");
    return new Dictionary<string, List<T?>>();
  }
}
