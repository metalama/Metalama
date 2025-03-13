[Override]
[Introduction]
internal class TargetClass
{
  public int ExistingProperty
  {
    get
    {
      global::System.Console.WriteLine("Override");
      Console.WriteLine("Original");
      return 42;
    }
    set
    {
      global::System.Console.WriteLine("Override");
      Console.WriteLine("Original");
      return;
    }
  }
  public int ExistingProperty_Expression
  {
    get
    {
      global::System.Console.WriteLine("Override");
      return 42;
    }
  }
  private int _existingProperty_Auto;
  public int ExistingProperty_Auto
  {
    get
    {
      global::System.Console.WriteLine("Override");
      return this._existingProperty_Auto;
    }
    set
    {
      global::System.Console.WriteLine("Override");
      this._existingProperty_Auto = value;
      return;
    }
  }
  private int _existingProperty_AutoInitializer = 42;
  public int ExistingProperty_AutoInitializer
  {
    get
    {
      global::System.Console.WriteLine("Override");
      return this._existingProperty_AutoInitializer;
    }
    set
    {
      global::System.Console.WriteLine("Override");
      this._existingProperty_AutoInitializer = value;
      return;
    }
  }
  public int ExistingProperty_InitOnly
  {
    get
    {
      global::System.Console.WriteLine("Override");
      Console.WriteLine("Original");
      return 42;
    }
    init
    {
      global::System.Console.WriteLine("Override");
      Console.WriteLine("Original");
      return;
    }
  }
  public IEnumerable<int> ExistingProperty_Iterator
  {
    get
    {
      global::System.Console.WriteLine("Override");
      return global::Metalama.Framework.RunTime.RunTimeAspectHelper.Buffer(this.ExistingProperty_Iterator_Source);
    }
  }
  private IEnumerable<int> ExistingProperty_Iterator_Source
  {
    get
    {
      Console.WriteLine("Original");
      yield return 42;
    }
  }
  public global::System.Int32 IntroducedProperty
  {
    get
    {
      global::System.Console.WriteLine("Override");
      global::System.Console.WriteLine("Original");
      return (global::System.Int32)42;
    }
    set
    {
      global::System.Console.WriteLine("Override");
      global::System.Console.WriteLine("Original");
      return;
    }
  }
  private global::System.Int32 _introducedProperty_Auto;
  public global::System.Int32 IntroducedProperty_Auto
  {
    get
    {
      global::System.Console.WriteLine("Override");
      return this._introducedProperty_Auto;
    }
    set
    {
      global::System.Console.WriteLine("Override");
      this._introducedProperty_Auto = value;
      return;
    }
  }
  private global::System.Int32 _introducedProperty_AutoInitializer = (global::System.Int32)42;
  public global::System.Int32 IntroducedProperty_AutoInitializer
  {
    get
    {
      global::System.Console.WriteLine("Override");
      return this._introducedProperty_AutoInitializer;
    }
    set
    {
      global::System.Console.WriteLine("Override");
      this._introducedProperty_AutoInitializer = value;
      return;
    }
  }
  public global::System.Int32 IntroducedProperty_Expression
  {
    get
    {
      global::System.Console.WriteLine("Override");
      return (global::System.Int32)42;
    }
  }
  public global::System.Int32 IntroducedProperty_InitOnly
  {
    get
    {
      global::System.Console.WriteLine("Override");
      global::System.Console.WriteLine("Original");
      return (global::System.Int32)42;
    }
    init
    {
      global::System.Console.WriteLine("Override");
      global::System.Console.WriteLine("Original");
      return;
    }
  }
  private global::System.Collections.Generic.IEnumerable<global::System.Int32> IntroducedProperty_Iterator_Introduction
  {
    get
    {
      global::System.Console.WriteLine("Original");
      yield return 42;
    }
  }
  public global::System.Collections.Generic.IEnumerable<global::System.Int32> IntroducedProperty_Iterator
  {
    get
    {
      global::System.Console.WriteLine("Override");
      return global::Metalama.Framework.RunTime.RunTimeAspectHelper.Buffer(this.IntroducedProperty_Iterator_Introduction);
    }
  }
}