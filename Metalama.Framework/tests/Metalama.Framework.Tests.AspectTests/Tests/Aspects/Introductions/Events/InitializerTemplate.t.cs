[Introduction]
internal class TargetClass
{
  public static EventHandler Foo = new(Bar);
  public static void Bar(object? sender, EventArgs eventArgs)
  {
  }
  public event global::System.EventHandler? IntroducedEventField = global::Metalama.Framework.IntegrationTests.Aspects.Introductions.Events.InitializerTemplate.TargetClass.Foo;
  public static event global::System.EventHandler? IntroducedEventField_Static = global::Metalama.Framework.IntegrationTests.Aspects.Introductions.Events.InitializerTemplate.TargetClass.Foo;
}