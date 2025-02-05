using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Contracts.Inheritance_Indexer;

internal class NotNullAttribute : ContractAspect
{
    public override void Validate( dynamic? value )
    {
        if (value == null)
        {
            throw new ArgumentNullException();
        }
    }
}

internal interface ITarget
{
    [NotNull]
    string this[ int i ] { get; }
}

// <target>
internal class Target : ITarget
{
    public string this[ int i ]
    {
        get => "42";
        set { }
    }
}