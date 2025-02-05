private int Method(int a)
{
  global::System.Tuple<global::System.String, global::System.Int32> tuple = new("string", 0);
  global::System.Console.WriteLine(tuple.Item1);
  return this.Method(a);
}