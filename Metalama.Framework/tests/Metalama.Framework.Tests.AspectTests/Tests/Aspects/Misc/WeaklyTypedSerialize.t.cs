internal class TargetCode
{
  private global::System.Int32 _f;
  [global::Metalama.Framework.Tests.AspectTests.Aspects.Misc.WeaklyTypedSerialize.IgnoreValuesAttribute(0)]
  public global::System.Int32 F
  {
    get
    {
      return this._f;
    }
    set
    {
      if (value == ((global::System.Int32)0))
      {
        return;
      }
      this._f = value;
    }
  }
  private global::System.String? _s;
  [global::Metalama.Framework.Tests.AspectTests.Aspects.Misc.WeaklyTypedSerialize.IgnoreValuesAttribute("")]
  public global::System.String? S
  {
    get
    {
      return this._s;
    }
    set
    {
      if (value == ((global::System.String? )""))
      {
        return;
      }
      this._s = value;
    }
  }
  private global::Metalama.Framework.Tests.AspectTests.Aspects.Misc.WeaklyTypedSerialize.MyEnum _e;
  [global::Metalama.Framework.Tests.AspectTests.Aspects.Misc.WeaklyTypedSerialize.IgnoreValuesAttribute(global::Metalama.Framework.Tests.AspectTests.Aspects.Misc.WeaklyTypedSerialize.MyEnum.None)]
  public global::Metalama.Framework.Tests.AspectTests.Aspects.Misc.WeaklyTypedSerialize.MyEnum E
  {
    get
    {
      return this._e;
    }
    set
    {
      if (value == ((global::Metalama.Framework.Tests.AspectTests.Aspects.Misc.WeaklyTypedSerialize.MyEnum)0))
      {
        return;
      }
      this._e = value;
    }
  }
  private global::Metalama.Framework.Tests.AspectTests.Aspects.Misc.WeaklyTypedSerialize.MyEnum _e2;
  [global::Metalama.Framework.Tests.AspectTests.Aspects.Misc.WeaklyTypedSerialize.IgnoreValuesAttribute(global::Metalama.Framework.Tests.AspectTests.Aspects.Misc.WeaklyTypedSerialize.MyEnum.Something)]
  public global::Metalama.Framework.Tests.AspectTests.Aspects.Misc.WeaklyTypedSerialize.MyEnum E2
  {
    get
    {
      return this._e2;
    }
    set
    {
      if (value == ((global::Metalama.Framework.Tests.AspectTests.Aspects.Misc.WeaklyTypedSerialize.MyEnum)1))
      {
        return;
      }
      this._e2 = value;
    }
  }
}