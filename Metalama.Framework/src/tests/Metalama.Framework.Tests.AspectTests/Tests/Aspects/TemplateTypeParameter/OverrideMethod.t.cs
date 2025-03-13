public class Target
{
  [Aspect]
  public string M()
  {
    var x = default(global::System.String);
    var t = (global::System.String)null !;
    var t2 = (global::System.String? )null;
    var t3 = (global::System.Collections.Generic.List<global::System.String>)null !;
    var t4 = (global::System.String[])null !;
    var t5 = (global::System.Collections.Generic.List<global::System.String[]>)null !;
    var t6 = (global::System.Collections.Generic.List<global::System.String>[])null !;
    return (global::System.String)global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.TemplateTypeParameters.OverrideMethod.Target.GenericMethod<global::System.String>((global::System.String)this.M_Source());
  }
  private string M_Source() => "";
  public static T GenericMethod<T>(T x) => x;
}