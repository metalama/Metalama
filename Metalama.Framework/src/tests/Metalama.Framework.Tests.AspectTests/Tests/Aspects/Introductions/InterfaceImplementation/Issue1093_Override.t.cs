// Warning LAMA0119 on `ParentAttribute`: `The compile-time declaration 'ParentAttribute' contains compile-time code but it does not explicitly import any of the the 'Metalama.Framework' namespaces. This may cause an inconsistent design-time experience. Add the [RunTimeOrCompileTimeAttribute] attribute to 'ParentAttribute' and import this namespace explicitly.`
// Warning LAMA0119 on `BuildAspect`: `The compile-time declaration 'ParentAttribute.BuildAspect(IAspectBuilder<INamedType>)' contains compile-time code but it does not explicitly import any of the the 'Metalama.Framework' namespaces. This may cause an inconsistent design-time experience. Add the [CompileTimeAttribute] attribute to 'ParentAttribute.BuildAspect(IAspectBuilder<INamedType>)' and import this namespace explicitly.`
// Warning LAMA0119 on `GetParentTemplate`: `The compile-time declaration 'ParentAttribute.GetParentTemplate()' contains compile-time code but it does not explicitly import any of the the 'Metalama.Framework' namespaces. This may cause an inconsistent design-time experience. Add the [CompileTimeAttribute] attribute to 'ParentAttribute.GetParentTemplate()' and import this namespace explicitly.`
// Warning LAMA0119 on `Event`: `The compile-time declaration 'ParentAttribute.Event' contains compile-time code but it does not explicitly import any of the the 'Metalama.Framework' namespaces. This may cause an inconsistent design-time experience. Add the [CompileTimeAttribute] attribute to 'ParentAttribute.Event' and import this namespace explicitly.`
// Warning LAMA0119 on `add`: `The compile-time declaration 'ParentAttribute.Event.add' contains compile-time code but it does not explicitly import any of the the 'Metalama.Framework' namespaces. This may cause an inconsistent design-time experience. Add the [CompileTimeAttribute] attribute to 'ParentAttribute.Event.add' and import this namespace explicitly.`
// Warning LAMA0119 on `remove`: `The compile-time declaration 'ParentAttribute.Event.remove' contains compile-time code but it does not explicitly import any of the the 'Metalama.Framework' namespaces. This may cause an inconsistent design-time experience. Add the [CompileTimeAttribute] attribute to 'ParentAttribute.Event.remove' and import this namespace explicitly.`
// Warning LAMA0119 on `Method`: `The compile-time declaration 'ParentAttribute.Method()' contains compile-time code but it does not explicitly import any of the the 'Metalama.Framework' namespaces. This may cause an inconsistent design-time experience. Add the [CompileTimeAttribute] attribute to 'ParentAttribute.Method()' and import this namespace explicitly.`
[Parent]
internal partial class Foo : IGotParent
{
  object? IGotParent.Property
  {
    get
    {
      return null;
    }
  }
  event Action IGotParent.Event
  {
    add
    {
      global::System.Console.WriteLine("Adding");
    }
    remove
    {
      global::System.Console.WriteLine("Removing");
    }
  }
  int IGotParent.Method()
  {
    return (global::System.Int32)1;
  }
}