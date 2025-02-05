#if TEST_OPTIONS
// @TestScenario(DesignTime)
// @AcceptInvalidInput
#endif

using System;
using System.Collections.Generic;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Options;

namespace Metalama.Framework.IntegrationTests.Aspects.DesignTime.InheritableOptions;

public class MyOptions : IHierarchicalOptions<INamedType>
{
    public string? MyOption1 { get; set; }
    public string? MyOption2 { get; set; }

    public object ApplyChanges(object changes, in ApplyChangesContext context)
    {
        var other = (MyOptions)changes;

        return new MyOptions
        {
            MyOption1 = other.MyOption1 ?? this.MyOption1,
            MyOption2 = other.MyOption2 ?? this.MyOption2,
        };
    }

    public IHierarchicalOptions? GetDefaultOptions(OptionsInitializationContext context)
    {
        return new MyOptions() { MyOption1 = null, MyOption2 = null };
    }
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = true)]
public class MyOptionsAttribute : Attribute, IHierarchicalOptionsProvider
{
    public string? MyOption1 { get; set; }
    public string? MyOption2 { get; set; }

    public IEnumerable<IHierarchicalOptions> GetOptions(in OptionsProviderContext context)
    {
        return new[] { new MyOptions { MyOption1 = MyOption1, MyOption2 = MyOption2 } };
    }
}

[MyOptions(MyOption1 = "Value1", MyOption2 = "Value2")]
public class A
{
}

#if TESTRUNNER

[MyOptions(MyOption1 = "Value1", MyOption2 = "Value2")]
public class A
{
}
#endif