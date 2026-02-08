[Introduction]
public class TargetClass : global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.InterfaceImplementation.ObliviousNullability.IObliviousInterface
{
  public global::System.Int32? NullableValueTypeProperty { get; set; }
  public global::System.String? ObliviousProperty { get; set; }
  public global::System.Int32 ValueTypeProperty { get; set; }
  public global::System.Int32? NullableValueTypeMethod(global::System.Int32? x)
  {
    return x;
  }
  public global::System.String? ObliviousMethod(global::System.String? x)
  {
    return x;
  }
  public global::System.Int32 ValueTypeMethod(global::System.Int32 x)
  {
    return x;
  }
  public event global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.InterfaceImplementation.ObliviousNullability.ObliviousHandler? PropertyChanged;
}
