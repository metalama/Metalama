private int Method(int a, int b)
{
  global::System.Func<global::System.Int32, global::System.Int32> action = x => x + 1;
  var result = this.Method(a, b);
  action(result);
  return (global::System.Int32)result;
}