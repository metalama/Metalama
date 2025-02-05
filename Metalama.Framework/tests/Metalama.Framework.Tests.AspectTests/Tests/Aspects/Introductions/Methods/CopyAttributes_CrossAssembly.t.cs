[Introduction]
internal class TargetClass
{
  [global::Metalama.Framework.IntegrationTests.Aspects.Introductions.Methods.CopyAttributes_CrossAssembly.FooAttribute(1)]
  [return: global::Metalama.Framework.IntegrationTests.Aspects.Introductions.Methods.CopyAttributes_CrossAssembly.FooAttribute(2)]
  public void DeclarativeMethod<
  [global::Metalama.Framework.IntegrationTests.Aspects.Introductions.Methods.CopyAttributes_CrossAssembly.FooAttribute(3)]
  T>([global::Metalama.Framework.IntegrationTests.Aspects.Introductions.Methods.CopyAttributes_CrossAssembly.FooAttribute(4)] global::System.Int32 x)
  {
    global::System.Console.WriteLine("This is introduced method.");
  }
  [global::Metalama.Framework.IntegrationTests.Aspects.Introductions.Methods.CopyAttributes_CrossAssembly.FooAttribute(1)]
  [return: global::Metalama.Framework.IntegrationTests.Aspects.Introductions.Methods.CopyAttributes_CrossAssembly.FooAttribute(2)]
  public void ProgrammaticMethod<
  [global::Metalama.Framework.IntegrationTests.Aspects.Introductions.Methods.CopyAttributes_CrossAssembly.FooAttribute(3)]
  T>([global::Metalama.Framework.IntegrationTests.Aspects.Introductions.Methods.CopyAttributes_CrossAssembly.FooAttribute(4)] global::System.Int32 y, [global::Metalama.Framework.IntegrationTests.Aspects.Introductions.Methods.CopyAttributes_CrossAssembly.FooAttribute(5)] global::System.Int32 z)
  {
    global::System.Console.WriteLine("This is introduced method.");
  }
}