internal class TargetClass
{
  private int _implicitlyPrivateProperty;
  [Override]
  private int ImplicitlyPrivateProperty
  {
    get
    {
      global::System.Console.WriteLine("This is the overridden getter.");
      return this._implicitlyPrivateProperty;
    }
    set
    {
      global::System.Console.WriteLine("This is the overridden setter.");
      this._implicitlyPrivateProperty = value;
    }
  }
  private int _privateProperty;
  [Override]
  private int PrivateProperty
  {
    get
    {
      global::System.Console.WriteLine("This is the overridden getter.");
      return this._privateProperty;
    }
    set
    {
      global::System.Console.WriteLine("This is the overridden setter.");
      this._privateProperty = value;
    }
  }
  private int _privateProtectedProperty;
  [Override]
  private protected int PrivateProtectedProperty
  {
    get
    {
      global::System.Console.WriteLine("This is the overridden getter.");
      return this._privateProtectedProperty;
    }
    set
    {
      global::System.Console.WriteLine("This is the overridden setter.");
      this._privateProtectedProperty = value;
    }
  }
  private int _protectedProperty;
  [Override]
  protected int ProtectedProperty
  {
    get
    {
      global::System.Console.WriteLine("This is the overridden getter.");
      return this._protectedProperty;
    }
    set
    {
      global::System.Console.WriteLine("This is the overridden setter.");
      this._protectedProperty = value;
    }
  }
  private int _protectedInternalProperty;
  [Override]
  protected internal int ProtectedInternalProperty
  {
    get
    {
      global::System.Console.WriteLine("This is the overridden getter.");
      return this._protectedInternalProperty;
    }
    set
    {
      global::System.Console.WriteLine("This is the overridden setter.");
      this._protectedInternalProperty = value;
    }
  }
  private int _internalProperty;
  [Override]
  internal int InternalProperty
  {
    get
    {
      global::System.Console.WriteLine("This is the overridden getter.");
      return this._internalProperty;
    }
    set
    {
      global::System.Console.WriteLine("This is the overridden setter.");
      this._internalProperty = value;
    }
  }
  private int _publicProperty;
  [Override]
  public int PublicProperty
  {
    get
    {
      global::System.Console.WriteLine("This is the overridden getter.");
      return this._publicProperty;
    }
    set
    {
      global::System.Console.WriteLine("This is the overridden setter.");
      this._publicProperty = value;
    }
  }
  private int _restrictedSetterProperty;
  [Override]
  public int RestrictedSetterProperty
  {
    get
    {
      global::System.Console.WriteLine("This is the overridden getter.");
      return this._restrictedSetterProperty;
    }
    private set
    {
      global::System.Console.WriteLine("This is the overridden setter.");
      this._restrictedSetterProperty = value;
    }
  }
  private readonly int _restrictedInitProperty;
  [Override]
  public int RestrictedInitProperty
  {
    get
    {
      global::System.Console.WriteLine("This is the overridden getter.");
      return this._restrictedInitProperty;
    }
    private init
    {
      global::System.Console.WriteLine("This is the overridden setter.");
      this._restrictedInitProperty = value;
    }
  }
  private int _restrictedGetterProperty;
  [Override]
  public int RestrictedGetterProperty
  {
    private get
    {
      global::System.Console.WriteLine("This is the overridden getter.");
      return this._restrictedGetterProperty;
    }
    set
    {
      global::System.Console.WriteLine("This is the overridden setter.");
      this._restrictedGetterProperty = value;
    }
  }
}