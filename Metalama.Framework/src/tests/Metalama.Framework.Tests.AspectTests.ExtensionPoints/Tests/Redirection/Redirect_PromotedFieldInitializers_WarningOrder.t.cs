// Warning LAMA0660 on `Source.Compute( 2 )`: `The linker could not apply the call 'Source.Compute( 2 )' redirected to 'Interceptors.Compute(int)' by aspect [RedirectAttribute] applied to 'Program', so the source code is kept unchanged. The call site is in a position that the linker does not rewrite, for instance the initializer of a field that an aspect has promoted to a property.`
// Warning LAMA0660 on `Source.Compute( 1 )`: `The linker could not apply the call 'Source.Compute( 1 )' redirected to 'Interceptors.Compute(int)' by aspect [RedirectAttribute] applied to 'Program', so the source code is kept unchanged. The call site is in a position that the linker does not rewrite, for instance the initializer of a field that an aspect has promoted to a property.`
[Redirect]
internal class Program
{
  private global::System.Int32 _second1 = Source.Compute(2);
  private global::System.Int32 _second
  {
    get
    {
      return this._second1;
    }
    set
    {
      this._second1 = value;
    }
  }
  private global::System.Int32 _first1 = Source.Compute(1);
  private global::System.Int32 _first
  {
    get
    {
      return this._first1;
    }
    set
    {
      this._first1 = value;
    }
  }
  public static void TestMain() => Console.WriteLine(new Program()._first + new Program()._second);
}