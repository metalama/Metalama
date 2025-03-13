private int Method(int a, int b)
{
  var action = (int x) => x + 1;
  var result = this.Method(a, b);
  action(result);
  return (global::System.Int32)result;
}