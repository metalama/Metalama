// Warning TEST0003 on `Measure`: `The redirection of 'Source.Measure( "property" )' was refused with ArgumentException: The call site 'Source.Measure( "property" )' cannot be redirected to the instance method 'Program.Measure_Interceptor(string)', because the call site is in an attribute, a constructor initializer, the initializer of a field, an event or a property, or the default value of a parameter.`
// Warning TEST0003 on `Measure`: `The redirection of 'Source.Measure( 1 )' was refused with ArgumentException: The call site 'Source.Measure( 1 )' cannot be redirected to the instance method 'Program.Measure_Interceptor1(int)', because the call site is in an attribute, a constructor initializer, the initializer of a field, an event or a property, or the default value of a parameter.`
[Redirect]
internal class Program
{
  public int Length { get; } = Source.Measure("property");
  public event EventHandler? Changed = Source.Measure(1);
  public int Compute() => this.Measure_Interceptor("method");
  private global::System.Int32 Measure_Interceptor(global::System.String text)
  {
    return global::Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.TemplateRedirection.Instance_Initializers_Refused.Source.Measure(text);
  }
  private global::System.EventHandler? Measure_Interceptor1(global::System.Int32 value)
  {
    return global::Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.TemplateRedirection.Instance_Initializers_Refused.Source.Measure(value);
  }
}