// Error CS1061 on `_cast<ITest>`: `'Target' does not contain a definition for '_cast' and no accessible extension method '_cast' accepting a first argument of type 'Target' could be found (are you missing a using directive or an assembly reference?)`
// Error CS1061 on `_cast<ITest>`: `'Target' does not contain a definition for '_cast' and no accessible extension method '_cast' accepting a first argument of type 'Target' could be found (are you missing a using directive or an assembly reference?)`
// Error CS1061 on `_cast<ITest>`: `'Target' does not contain a definition for '_cast' and no accessible extension method '_cast' accepting a first argument of type 'Target' could be found (are you missing a using directive or an assembly reference?)`
// Error CS1061 on `_cast<ITest>`: `'Target' does not contain a definition for '_cast' and no accessible extension method '_cast' accepting a first argument of type 'Target' could be found (are you missing a using directive or an assembly reference?)`
// Error CS1061 on `_cast<ITest>`: `'Target' does not contain a definition for '_cast' and no accessible extension method '_cast' accepting a first argument of type 'Target' could be found (are you missing a using directive or an assembly reference?)`
internal class Target : ITest
{
  int ITest.Foo
  {
    get
    {
      return this._cast<ITest>().Foo;
    }
    set
    {
      this._cast<ITest>().Foo = value;
    }
  }
  int ITest.Bar()
  {
    return this._cast<ITest>().Bar();
  }
  event EventHandler ITest.Quz
  {
    add
    {
      this._cast<ITest>().Quz += value;
    }
    remove
    {
      this._cast<ITest>().Quz -= value;
    }
  }
}