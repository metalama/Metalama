[MyAspect]
internal class Target
{
  internal global::System.String Internal()
  {
    return "Metalama.Framework.Tests.AspectTests.Tests.Aspects.Misc.CompiledTemplateAccessilibty_CrossAssembly.InternalCompileTimeType";
  }
  private global::System.String Private()
  {
    return "Metalama.Framework.Tests.AspectTests.Tests.Aspects.Misc.CompiledTemplateAccessilibty_CrossAssembly.MyAspect+PrivateCompileTimeType";
  }
  private protected global::System.String PrivateProtected()
  {
    return "Metalama.Framework.Tests.AspectTests.Tests.Aspects.Misc.CompiledTemplateAccessilibty_CrossAssembly.MyAspect+ProtectedCompileTimeType, Metalama.Framework.Tests.AspectTests.Tests.Aspects.Misc.CompiledTemplateAccessilibty_CrossAssembly.InternalCompileTimeType";
  }
  protected global::System.String Protected()
  {
    return "Metalama.Framework.Tests.AspectTests.Tests.Aspects.Misc.CompiledTemplateAccessilibty_CrossAssembly.MyAspect+ProtectedCompileTimeType";
  }
  protected internal global::System.String ProtectedInternal()
  {
    return "Metalama.Framework.Tests.AspectTests.Tests.Aspects.Misc.CompiledTemplateAccessilibty_CrossAssembly.MyAspect+ProtectedInternalCompileTimeType";
  }
  public global::System.String Public()
  {
    return "Metalama.Framework.Tests.AspectTests.Tests.Aspects.Misc.CompiledTemplateAccessilibty_CrossAssembly.PublicCompileTimeType";
  }
}