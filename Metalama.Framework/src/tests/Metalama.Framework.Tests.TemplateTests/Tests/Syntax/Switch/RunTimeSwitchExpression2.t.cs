private int Method(int a)
{
  object o = new();
  var y = o switch
  {
    int i => 1,
    _ => 0
  };
  return this.Method(a);
}