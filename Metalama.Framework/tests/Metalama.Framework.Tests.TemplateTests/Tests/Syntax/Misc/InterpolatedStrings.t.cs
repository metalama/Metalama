private int Method(int a)
{
  var neutral = $"Zero={0, -5:x}";
  global::System.Console.WriteLine(neutral);
  global::System.Console.WriteLine("ParameterCount=1    ");
  var dy = $"Value={a, -5:x}";
  var rt = $"Value={(global::System.Environment.Version)}";
  var both = $"field={a}";
  return this.Method(a);
}