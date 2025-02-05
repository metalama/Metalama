private int Method(int a, int b)
{
  var action = (int a_1, int b_1) => a_1 + b_1;
  var result = this.Method(a, b);
  result = action(result, 2);
  return (global::System.Int32)result;
}