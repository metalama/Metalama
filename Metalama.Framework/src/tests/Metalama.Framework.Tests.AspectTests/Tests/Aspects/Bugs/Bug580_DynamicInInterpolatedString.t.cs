internal class Target
{
  [Log]
  public dynamic DoSomething(dynamic input)
  {
    dynamic result;
    result = input;
    global::System.Console.WriteLine($"Method returned: {(object)result}");
    var result2 = input;
    global::System.Console.WriteLine($"  First param value: {(object)result2}");
    return (dynamic)result;
  }
}