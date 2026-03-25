// Final Compilation.Emit failed.
// Error CS0102 on `Property`: `The type 'TargetCode' already contains a definition for 'Property'`
internal class TargetCode
{
  private int _property;
  [Aspect]
  private int Property
  {
    get
    {
      global::System.Console.WriteLine("Aspect");
      return this._property;
    }
    set
    {
      global::System.Console.WriteLine("Aspect");
      this._property = value;
    }
  }
  private int Property { get; set; }
}