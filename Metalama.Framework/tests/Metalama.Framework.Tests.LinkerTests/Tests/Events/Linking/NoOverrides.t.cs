public class Target : Base
{
  public override event EventHandler BaseVirtualOverriddenMethod
  {
    add
    {
      this.BaseVirtualOverriddenMethod_Source += value;
    }
    remove
    {
      this.BaseVirtualOverriddenMethod_Source -= value;
    }
  }
  private event EventHandler BaseVirtualOverriddenMethod_Source
  {
    add
    {
    }
    remove
    {
    }
  }
  public new virtual event EventHandler BaseVirtualHiddenMethod
  {
    add
    {
      this.BaseVirtualHiddenMethod_Source += value;
    }
    remove
    {
      this.BaseVirtualHiddenMethod_Source -= value;
    }
  }
  private event EventHandler BaseVirtualHiddenMethod_Source
  {
    add
    {
    }
    remove
    {
    }
  }
  public new event EventHandler BaseHiddenMethod
  {
    add
    {
    }
    remove
    {
    }
  }
  public new static event EventHandler BaseStaticHiddenMethod
  {
    add
    {
    }
    remove
    {
    }
  }
  public event EventHandler LocalMethod
  {
    add
    {
    }
    remove
    {
    }
  }
  public virtual event EventHandler LocalVirtualMethod
  {
    add
    {
      this.LocalVirtualMethod_Source += value;
    }
    remove
    {
      this.LocalVirtualMethod_Source -= value;
    }
  }
  private event EventHandler LocalVirtualMethod_Source
  {
    add
    {
    }
    remove
    {
    }
  }
  public static event EventHandler LocalStaticMethod
  {
    add
    {
    }
    remove
    {
    }
  }
  public event System.EventHandler Foo
  {
    add
    {
      // Should invoke this.
      this.BaseMethod += value;
      // Should invoke this.
      this.BaseMethod += value;
      // Should invoke this.
      this.BaseMethod += value;
      // Should invoke this.
      this.BaseMethod += value;
      // Should invoke current type.
      Target.BaseStaticMethod += value;
      // Should invoke current type.
      Target.BaseStaticMethod += value;
      // Should invoke current type.
      Target.BaseStaticMethod += value;
      // Should invoke current type.
      Target.BaseStaticMethod += value;
      // Should invoke base.
      base.BaseVirtualMethod += value;
      // Should invoke base.
      base.BaseVirtualMethod += value;
      // Should invoke base.
      base.BaseVirtualMethod += value;
      // Should invoke this.
      this.BaseVirtualMethod += value;
      // Should invoke _Source.
      this.BaseVirtualOverriddenMethod_Source += value;
      // Should invoke _Source.
      this.BaseVirtualOverriddenMethod_Source += value;
      // Should invoke _Source.
      this.BaseVirtualOverriddenMethod_Source += value;
      // Should invoke this.
      this.BaseVirtualOverriddenMethod += value;
      // Should invoke _Source.
      this.BaseVirtualHiddenMethod_Source += value;
      // Should invoke _Source.
      this.BaseVirtualHiddenMethod_Source += value;
      // Should invoke _Source.
      this.BaseVirtualHiddenMethod_Source += value;
      // Should invoke this.
      this.BaseVirtualHiddenMethod += value;
      // Should invoke this.
      this.BaseHiddenMethod += value;
      // Should invoke this.
      this.BaseHiddenMethod += value;
      // Should invoke this.
      this.BaseHiddenMethod += value;
      // Should invoke this.
      this.BaseHiddenMethod += value;
      // Should invoke current type.
      Target.BaseStaticHiddenMethod += value;
      // Should invoke current type.
      Target.BaseStaticHiddenMethod += value;
      // Should invoke current type.
      Target.BaseStaticHiddenMethod += value;
      // Should invoke current type.
      Target.BaseStaticHiddenMethod += value;
      // Should invoke this.
      this.LocalMethod += value;
      // Should invoke this.
      this.LocalMethod += value;
      // Should invoke this.
      this.LocalMethod += value;
      // Should invoke this.
      this.LocalMethod += value;
      // Should invoke _Source.
      this.LocalVirtualMethod_Source += value;
      // Should invoke _Source.
      this.LocalVirtualMethod_Source += value;
      // Should invoke _Source.
      this.LocalVirtualMethod_Source += value;
      // Should invoke this.
      this.LocalVirtualMethod += value;
      // Should invoke current type.
      Target.LocalStaticMethod += value;
      // Should invoke current type.
      Target.LocalStaticMethod += value;
      // Should invoke current type.
      Target.LocalStaticMethod += value;
      // Should invoke current type.
      Target.LocalStaticMethod += value;
    }
    remove
    {
      // Should invoke this.
      this.BaseMethod -= value;
      // Should invoke this.
      this.BaseMethod -= value;
      // Should invoke this.
      this.BaseMethod -= value;
      // Should invoke this.
      this.BaseMethod -= value;
      // Should invoke current type.
      Target.BaseStaticMethod -= value;
      // Should invoke current type.
      Target.BaseStaticMethod -= value;
      // Should invoke current type.
      Target.BaseStaticMethod -= value;
      // Should invoke current type.
      Target.BaseStaticMethod -= value;
      // Should invoke base.
      base.BaseVirtualMethod -= value;
      // Should invoke base.
      base.BaseVirtualMethod -= value;
      // Should invoke base.
      base.BaseVirtualMethod -= value;
      // Should invoke this.
      this.BaseVirtualMethod -= value;
      // Should invoke _Source.
      this.BaseVirtualOverriddenMethod_Source -= value;
      // Should invoke _Source.
      this.BaseVirtualOverriddenMethod_Source -= value;
      // Should invoke _Source.
      this.BaseVirtualOverriddenMethod_Source -= value;
      // Should invoke this.
      this.BaseVirtualOverriddenMethod -= value;
      // Should invoke _Source.
      this.BaseVirtualHiddenMethod_Source -= value;
      // Should invoke _Source.
      this.BaseVirtualHiddenMethod_Source -= value;
      // Should invoke _Source.
      this.BaseVirtualHiddenMethod_Source -= value;
      // Should invoke this.
      this.BaseVirtualHiddenMethod -= value;
      // Should invoke this.
      this.BaseHiddenMethod -= value;
      // Should invoke this.
      this.BaseHiddenMethod -= value;
      // Should invoke this.
      this.BaseHiddenMethod -= value;
      // Should invoke this.
      this.BaseHiddenMethod -= value;
      // Should invoke current type.
      Target.BaseStaticHiddenMethod -= value;
      // Should invoke current type.
      Target.BaseStaticHiddenMethod -= value;
      // Should invoke current type.
      Target.BaseStaticHiddenMethod -= value;
      // Should invoke current type.
      Target.BaseStaticHiddenMethod -= value;
      // Should invoke this.
      this.LocalMethod -= value;
      // Should invoke this.
      this.LocalMethod -= value;
      // Should invoke this.
      this.LocalMethod -= value;
      // Should invoke this.
      this.LocalMethod -= value;
      // Should invoke _Source.
      this.LocalVirtualMethod_Source -= value;
      // Should invoke _Source.
      this.LocalVirtualMethod_Source -= value;
      // Should invoke _Source.
      this.LocalVirtualMethod_Source -= value;
      // Should invoke this.
      this.LocalVirtualMethod -= value;
      // Should invoke current type.
      Target.LocalStaticMethod -= value;
      // Should invoke current type.
      Target.LocalStaticMethod -= value;
      // Should invoke current type.
      Target.LocalStaticMethod -= value;
      // Should invoke current type.
      Target.LocalStaticMethod -= value;
    }
  }
}