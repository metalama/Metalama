public class MyClass<T>
{
  [Log]
  public (T?, string) GetValues()
  {
    global::System.Console.WriteLine($"Return type: {typeof((T, global::System.String))}");
    return (default, "test");
  }
}
