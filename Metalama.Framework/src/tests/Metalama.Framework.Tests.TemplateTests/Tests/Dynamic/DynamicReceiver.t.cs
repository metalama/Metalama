private int Method(int a)
{
  this.MyMethod();
  this.MyMethod().More();
  this.Value = 5;
  this.MyMethod().More().Value = 5;
  a.MyMethod();
  a.MyMethod().More();
  global::Metalama.Framework.Tests.AspectTests.Templating.Dynamic.DynamicReceiver.TargetCode.Hello();
  return default;
}