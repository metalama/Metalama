[Introduction]
internal class TargetClass
{
  [global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Introductions.Events.CopyAttributes_CrossAssembly.FooAttribute(1)]
  public event global::System.EventHandler? Event
  {
    [global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Introductions.Events.CopyAttributes_CrossAssembly.FooAttribute(4)]
    [return: global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Introductions.Events.CopyAttributes_CrossAssembly.FooAttribute(2)]
    [param: global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Introductions.Events.CopyAttributes_CrossAssembly.FooAttribute(3)]
    add
    {
      global::System.Console.WriteLine("Original add accessor.");
    }
    [global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Introductions.Events.CopyAttributes_CrossAssembly.FooAttribute(7)]
    [return: global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Introductions.Events.CopyAttributes_CrossAssembly.FooAttribute(5)]
    [param: global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Introductions.Events.CopyAttributes_CrossAssembly.FooAttribute(6)]
    remove
    {
      global::System.Console.WriteLine("Original remove accessor.");
    }
  }
  [global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Introductions.Events.CopyAttributes_CrossAssembly.FooAttribute(1)]
  [method: global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Introductions.Events.CopyAttributes_CrossAssembly.FooAttribute(2)]
  public event global::System.EventHandler? FieldLikeEvent;
  private event global::System.EventHandler? IntroducedEvent
  {
    [global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Introductions.Events.CopyAttributes_CrossAssembly.FooAttribute(1)]
    [return: global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Introductions.Events.CopyAttributes_CrossAssembly.FooAttribute(2)]
    [param: global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Introductions.Events.CopyAttributes_CrossAssembly.FooAttribute(3)]
    add
    {
      value?.Invoke(null, new global::System.EventArgs());
    }
    [global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Introductions.Events.CopyAttributes_CrossAssembly.FooAttribute(1)]
    [return: global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Introductions.Events.CopyAttributes_CrossAssembly.FooAttribute(2)]
    [param: global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Introductions.Events.CopyAttributes_CrossAssembly.FooAttribute(3)]
    remove
    {
      value?.Invoke(null, new global::System.EventArgs());
    }
  }
}