using System;
using System.Collections.Generic;
using Metalama.Framework.Code;
using Metalama.Framework.Options;
using Metalama.Framework.Eligibility;
using Metalama.Framework.Tests.AspectTests.Tests.Options.CrossProject;

[assembly: MyOptions( "FromDependencyAssembly" )]

namespace Metalama.Framework.Tests.AspectTests.Tests.Options.CrossProject;

public record MyOptions : IHierarchicalOptions<IDeclaration>
{
    public string? Value { get; init; }

    public bool? BaseWins { get; init; }

#if !NET5_0_OR_GREATER
    public IHierarchicalOptions GetDefaultOptions( OptionsInitializationContext context ) => this;
#endif

    public object ApplyChanges( object changes, in ApplyChangesContext context )
    {
        var other = (MyOptions)changes;

        return new MyOptions { Value = other.Value ?? Value };
    }

    public void BuildEligibility( IEligibilityBuilder<IDeclaration> declaration ) { }
}

[AttributeUsage( AttributeTargets.All, AllowMultiple = true )]
public class MyOptionsAttribute : Attribute, IHierarchicalOptionsProvider
{
    private string _value;

    public MyOptionsAttribute( string value )
    {
        _value = value;
    }

    public IEnumerable<IHierarchicalOptions> GetOptions( in OptionsProviderContext context )
    {
        return new[] { new MyOptions { Value = _value } };
    }
}

[MyOptions( "FromBaseClass" )]
public class BaseClass { }

[MyOptions( "BaseDeclaringClass" )]
public class BaseNestingClass
{
    public class BaseNestedClass { }
}

public class BaseClassWithoutDirectOptions { }