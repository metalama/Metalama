using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Options;

namespace Metalama.Framework.Tests.AspectTests.Tests.Options.GetOptions_CrossProject;

public record Options : IHierarchicalOptions<INamedType>
{
    public IHierarchicalOptions GetDefaultOptions( OptionsInitializationContext context ) => this;

    public object ApplyChanges( object changes, in ApplyChangesContext context )
    {
        return this;
    }
}

internal class Aspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        base.BuildAspect( builder );

        var externalType = (INamedType)TypeFactory.GetType( typeof(C) );

        var options = externalType.Enhancements().GetOptions<Options>();

        if (options is null)
        {
            throw new Exception();
        }
    }
}

// <target>
[Aspect]
internal class Target { }