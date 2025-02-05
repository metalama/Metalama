[Override]
[Introduction]
internal class TargetClass
{
  private global::System.String? _existingField;
  public global::System.String? ExistingField
  {
    get
    {
      global::System.Console.WriteLine("Override");
      return this._existingField;
    }
    set
    {
      global::System.Console.WriteLine("Override");
      this._existingField = value;
    }
  }
  private global::System.String? _existingField_ReadOnly;
  public global::System.String? ExistingField_ReadOnly
  {
    get
    {
      global::System.Console.WriteLine("Override");
      return this._existingField_ReadOnly;
    }
    set
    {
      global::System.Console.WriteLine("Override");
      this._existingField_ReadOnly = value;
    }
  }
  private global::System.String _existingField_Initializer = "42";
  public global::System.String ExistingField_Initializer
  {
    get
    {
      global::System.Console.WriteLine("Override");
      return this._existingField_Initializer;
    }
    set
    {
      global::System.Console.WriteLine("Override");
      this._existingField_Initializer = value;
    }
  }
  private static global::System.String? _existingField_Static;
  public static global::System.String? ExistingField_Static
  {
    get
    {
      global::System.Console.WriteLine("Override");
      return global::Metalama.Framework.IntegrationTests.Aspects.Overrides.Fields.CrossAssembly.TargetClass._existingField_Static;
    }
    set
    {
      global::System.Console.WriteLine("Override");
      global::Metalama.Framework.IntegrationTests.Aspects.Overrides.Fields.CrossAssembly.TargetClass._existingField_Static = value;
    }
  }
  private global::System.String? _introducedField;
  public global::System.String? IntroducedField
  {
    get
    {
      global::System.Console.WriteLine("Override");
      return this._introducedField;
    }
    set
    {
      global::System.Console.WriteLine("Override");
      this._introducedField = value;
    }
  }
  private global::System.String _introducedField_Initializer = "IntroducedField_Initializer";
  public global::System.String IntroducedField_Initializer
  {
    get
    {
      global::System.Console.WriteLine("Override");
      return this._introducedField_Initializer;
    }
    set
    {
      global::System.Console.WriteLine("Override");
      this._introducedField_Initializer = value;
    }
  }
  private readonly global::System.String? _introducedField_ReadOnly;
  public global::System.String? IntroducedField_ReadOnly
  {
    get
    {
      global::System.Console.WriteLine("Override");
      return this._introducedField_ReadOnly;
    }
    private init
    {
      global::System.Console.WriteLine("Override");
      this._introducedField_ReadOnly = value;
    }
  }
  private static global::System.String? _introducedField_Static;
  public static global::System.String? IntroducedField_Static
  {
    get
    {
      global::System.Console.WriteLine("Override");
      return global::Metalama.Framework.IntegrationTests.Aspects.Overrides.Fields.CrossAssembly.TargetClass._introducedField_Static;
    }
    set
    {
      global::System.Console.WriteLine("Override");
      global::Metalama.Framework.IntegrationTests.Aspects.Overrides.Fields.CrossAssembly.TargetClass._introducedField_Static = value;
    }
  }
}