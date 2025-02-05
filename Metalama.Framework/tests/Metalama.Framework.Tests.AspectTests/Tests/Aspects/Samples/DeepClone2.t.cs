[DeepClone]
internal class AutomaticallyCloneable : ICloneable
{
  private int _a;
  private ManuallyCloneable? _b;
  private AutomaticallyCloneable? _c;
  public virtual AutomaticallyCloneable Clone()
  {
    var clone = (AutomaticallyCloneable)MemberwiseClone();
    clone._b = (ManuallyCloneable? )_b?.Clone();
    clone._c = (_c?.Clone());
    return clone;
  }
  object ICloneable.Clone()
  {
    return Clone();
  }
}
internal class DerivedCloneable : AutomaticallyCloneable
{
  private string? _d;
  public override DerivedCloneable Clone()
  {
    var clone = (DerivedCloneable)base.Clone();
    return clone;
  }
}