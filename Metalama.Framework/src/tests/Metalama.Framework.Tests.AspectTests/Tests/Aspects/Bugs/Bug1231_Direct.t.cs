public class MyClass<T>
{
  [Log]
  public T? GetValue()
  {
    global::System.Console.WriteLine($"Return type: {typeof(T)}");
    return default;
  }
}
