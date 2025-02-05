[Introduction]
internal class TargetClass : DerivedClass
{
  public new global::System.Int32 BaseClassAbstractMethod()
  {
    // New keyword, call the base method of the same name.
    return base.BaseClassAbstractMethod();
  }
  public new global::System.Int32 BaseClassAbstractSealedMethod()
  {
    // New keyword, call the base method of the same name.
    return base.BaseClassAbstractSealedMethod();
  }
  public global::System.Int32 BaseClassInaccessibleMethod()
  {
    // No new keyword, return a default value.
    return default(global::System.Int32);
  }
  public new global::System.Int32 BaseClassMethod()
  {
    // New keyword, call the base method of the same name.
    return base.BaseClassMethod();
  }
  public new global::System.Int32 BaseClassMethodHiddenByInaccessibleMethod()
  {
    // New keyword, call the base method of the same name.
    return base.BaseClassMethodHiddenByInaccessibleMethod();
  }
  public new global::System.Int32 BaseClassMethodHiddenByMethod()
  {
    // New keyword, call the base method of the same name.
    return base.BaseClassMethodHiddenByMethod();
  }
  public new global::System.Int32 BaseClassMethodHiddenByVirtualMethod()
  {
    // New keyword, call the base method of the same name.
    return base.BaseClassMethodHiddenByVirtualMethod();
  }
  public new global::System.Int32 BaseClassVirtualMethod()
  {
    // New keyword, call the base method of the same name.
    return base.BaseClassVirtualMethod();
  }
  public new global::System.Int32 BaseClassVirtualMethodHiddenByMethod()
  {
    // New keyword, call the base method of the same name.
    return base.BaseClassVirtualMethodHiddenByMethod();
  }
  public new global::System.Int32 BaseClassVirtualOverriddenMethod()
  {
    // New keyword, call the base method of the same name.
    return base.BaseClassVirtualOverriddenMethod();
  }
  public new global::System.Int32 BaseClassVirtualSealedMethod()
  {
    // New keyword, call the base method of the same name.
    return base.BaseClassVirtualSealedMethod();
  }
  public global::System.Int32 DerivedClassInaccessibleMethod()
  {
    // No new keyword, return a default value.
    return default(global::System.Int32);
  }
  public new global::System.Int32 DerivedClassMethod()
  {
    // New keyword, call the base method of the same name.
    return base.DerivedClassMethod();
  }
  public new global::System.Int32 DerivedClassVirtualMethod()
  {
    // New keyword, call the base method of the same name.
    return base.DerivedClassVirtualMethod();
  }
  public new global::System.Int32 DerivedClassVirtualSealedMethod()
  {
    // New keyword, call the base method of the same name.
    return base.DerivedClassVirtualSealedMethod();
  }
  public global::System.Int32 NonExistentMethod()
  {
    // No new keyword, return a default value.
    return default(global::System.Int32);
  } // All methods in this class should contain a comment describing the correct output.
}