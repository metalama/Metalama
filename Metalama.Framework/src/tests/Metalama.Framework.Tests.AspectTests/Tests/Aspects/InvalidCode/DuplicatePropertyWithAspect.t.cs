// Final Compilation.Emit failed.
// Error CS1061 on `_property`: `'TargetCode' does not contain a definition for '_property' and no accessible extension method '_property' accepting a first argument of type 'TargetCode' could be found (are you missing a using directive or an assembly reference?)`
// Error CS1061 on `_property`: `'TargetCode' does not contain a definition for '_property' and no accessible extension method '_property' accepting a first argument of type 'TargetCode' could be found (are you missing a using directive or an assembly reference?)`
// Error CS0102 on `Property`: `The type 'TargetCode' already contains a definition for 'Property'`
// Error CS0229 on `Property`: `Ambiguity between 'TargetCode.Property' and 'TargetCode.Property'`
// Error CS0229 on `Property`: `Ambiguity between 'TargetCode.Property' and 'TargetCode.Property'`
internal class TargetCode
{
  [Aspect]
  private int Property
  {
    get
    {
      return this._property;
    }
    set
    {
      this._property = value;
    }
  }
  [Aspect]
  private int Property
  {
    get
    {
      global::System.Console.WriteLine("Aspect");
      return this.Property;
    }
    set
    {
      global::System.Console.WriteLine("Aspect");
      this.Property = value;
    }
  }
}