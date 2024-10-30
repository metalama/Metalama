[assembly: GenerateProxyAspect(typeof(ISomeInterface), "Metalama.Samples.Proxy.Tests", "SomeProxy")]
public interface ISomeInterface
{
  void VoidMethod(int a, string b);
  int NonVoidMethod(int a, string b);
  void VoidNoParamMethod();
}