private int Method(int a)
{
  // a1 = True
  var a2 = a is >= 0 and < 5;
  return this.Method(a);
}