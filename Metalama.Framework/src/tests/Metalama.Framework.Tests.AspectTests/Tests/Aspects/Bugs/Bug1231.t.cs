public class MyClass<T>
{
  [Log]
  public List<T?> GetValues()
  {
    global::System.Console.WriteLine($"Return type: {typeof(global::System.Collections.Generic.List<T>)}");
    return new List<T?>();
  }
}
