public class MyClass<T>
{
  [Log]
  public T? [] GetValues()
  {
    global::System.Console.WriteLine($"Return type: {typeof(T[])}");
    return new T? [0];
  }
}
