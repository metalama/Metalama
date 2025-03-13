[Introduction]
internal class TargetClass
{
  public global::Metalama.Framework.IntegrationTests.Aspects.Introductions.Properties.DeclarativeRunTimeOnly.RunTimeOnlyClass? IntroducedProperty_Accessors
  {
    get
    {
      global::System.Console.WriteLine("Get");
      return null;
    }
    set
    {
      global::System.Console.WriteLine(value);
    }
  }
}