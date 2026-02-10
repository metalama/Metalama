internal class Target
{
  [Log]
  public dynamic DoSomething(dynamic input)
  {
    dynamic result;
    result = input;
    void LogResult()
    {
      global::System.Console.WriteLine($"Method returned: {(object)result}");
    }
    LogResult();
    return (dynamic)result;
  }
}
