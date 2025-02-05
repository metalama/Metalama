namespace Metalama.Framework.IntegrationTests.Aspects.Introductions.Fields.PrivateField_CrossAssembly;

// <target>
internal class Program
{
    [IntroducePrivateField]
    public void Foo() { }
}