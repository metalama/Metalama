// Final Compilation.Emit failed.
// Error CS0229 on `Property`: `Ambiguity between 'TargetCode.Property' and 'TargetCode.Property'`
// Error CS0229 on `Property`: `Ambiguity between 'TargetCode.Property' and 'TargetCode.Property'`
// Error CS0102 on `Property`: `The type 'TargetCode' already contains a definition for 'Property'`
internal class TargetCode
{
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
  private int Property { get; set; }
}