[MyAspect]
internal class C
{
  private int _instanceField = 5;
  private static int _staticField = 6;
  public void Method()
  {
    this._instanceField = this._instanceField;
    global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Misc.FieldOrPropertyValueShortcuts.C._staticField = global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Misc.FieldOrPropertyValueShortcuts.C._staticField;
  }
}