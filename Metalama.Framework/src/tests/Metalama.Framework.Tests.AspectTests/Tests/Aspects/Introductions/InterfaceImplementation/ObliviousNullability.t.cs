[Introduction]
public class TargetClass : global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.InterfaceImplementation.ObliviousNullability.IObliviousInterface
{
  public global::System.String? ObliviousProperty { get; set; }
  public global::System.String? ObliviousMethod(global::System.String? x)
  {
    return x;
  }
  public event global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.InterfaceImplementation.ObliviousNullability.ObliviousHandler? PropertyChanged;
}
