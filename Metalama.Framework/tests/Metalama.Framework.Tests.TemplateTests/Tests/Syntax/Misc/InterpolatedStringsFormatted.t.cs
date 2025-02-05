using System;
private int Method(int a)
{
  var neutral = $"Zero={0, -5:x}";
  Console.WriteLine("ParameterCount=1    ");
  var rt = $"Value={a, -5:x}";
  var both = $"field={a}";
  return this.Method(a);
}