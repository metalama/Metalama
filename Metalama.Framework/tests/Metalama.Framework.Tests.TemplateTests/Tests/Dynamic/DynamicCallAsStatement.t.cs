private int Method(int a)
{
  this.Method(1 + 1).Foo();
  this.Method(a).Foo();
  this.Method(1 + 1);
  this.Method(a);
  _ = this.Method(1 + 1).Foo();
  _ = this.Method(a).Foo();
  _ = this.Method(1 + 1);
  _ = this.Method(a);
  var x = this.Method(1 + 1).Foo();
  var y = this.Method(a).Foo();
  var a_1 = this.Method(1 + 1);
  var b = this.Method(a);
  return default;
}