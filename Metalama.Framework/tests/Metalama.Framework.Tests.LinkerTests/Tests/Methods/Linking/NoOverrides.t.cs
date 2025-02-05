public class Target : Base
{
  public override void BaseVirtualOverriddenMethod()
  {
    this.BaseVirtualOverriddenMethod_Source();
  }
  private void BaseVirtualOverriddenMethod_Source()
  {
  }
  public new virtual void BaseVirtualHiddenMethod()
  {
    this.BaseVirtualHiddenMethod_Source();
  }
  private void BaseVirtualHiddenMethod_Source()
  {
  }
  public new void BaseHiddenMethod()
  {
  }
  public new static void BaseStaticHiddenMethod()
  {
  }
  public void LocalMethod()
  {
  }
  public virtual void LocalVirtualMethod()
  {
    this.LocalVirtualMethod_Source();
  }
  private void LocalVirtualMethod_Source()
  {
  }
  public static void LocalStaticMethod()
  {
  }
  public void Foo()
  {
    // Should invoke this.
    this.BaseMethod();
    // Should invoke this.
    this.BaseMethod();
    // Should invoke this.
    this.BaseMethod();
    // Should invoke this.
    this.BaseMethod();
    // Should invoke current type.
    Target.BaseStaticMethod();
    // Should invoke current type.
    Target.BaseStaticMethod();
    // Should invoke current type.
    Target.BaseStaticMethod();
    // Should invoke current type.
    Target.BaseStaticMethod();
    // Should invoke base.
    base.BaseVirtualMethod();
    // Should invoke base.
    base.BaseVirtualMethod();
    // Should invoke base.
    base.BaseVirtualMethod();
    // Should invoke this.
    this.BaseVirtualMethod();
    // Should invoke _Source.
    this.BaseVirtualOverriddenMethod_Source();
    // Should invoke _Source.
    this.BaseVirtualOverriddenMethod_Source();
    // Should invoke _Source.
    this.BaseVirtualOverriddenMethod_Source();
    // Should invoke this.
    this.BaseVirtualOverriddenMethod();
    // Should invoke _Source.
    this.BaseVirtualHiddenMethod_Source();
    // Should invoke _Source.
    this.BaseVirtualHiddenMethod_Source();
    // Should invoke _Source.
    this.BaseVirtualHiddenMethod_Source();
    // Should invoke this.
    this.BaseVirtualHiddenMethod();
    // Should invoke this.
    this.BaseHiddenMethod();
    // Should invoke this.
    this.BaseHiddenMethod();
    // Should invoke this.
    this.BaseHiddenMethod();
    // Should invoke this.
    this.BaseHiddenMethod();
    // Should invoke current type.
    Target.BaseStaticHiddenMethod();
    // Should invoke current type.
    Target.BaseStaticHiddenMethod();
    // Should invoke current type.
    Target.BaseStaticHiddenMethod();
    // Should invoke current type.
    Target.BaseStaticHiddenMethod();
    // Should invoke this.
    this.LocalMethod();
    // Should invoke this.
    this.LocalMethod();
    // Should invoke this.
    this.LocalMethod();
    // Should invoke this.
    this.LocalMethod();
    // Should invoke _Source.
    this.LocalVirtualMethod_Source();
    // Should invoke _Source.
    this.LocalVirtualMethod_Source();
    // Should invoke _Source.
    this.LocalVirtualMethod_Source();
    // Should invoke this.
    this.LocalVirtualMethod();
    // Should invoke current type.
    Target.LocalStaticMethod();
    // Should invoke current type.
    Target.LocalStaticMethod();
    // Should invoke current type.
    Target.LocalStaticMethod();
    // Should invoke current type.
    Target.LocalStaticMethod();
  }
}