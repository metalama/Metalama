using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Eligibility;
using Metalama.Framework.Tests.AspectTests.Aspects.Diagnostics.IncorrectMetaMember;
using System;
using System.Linq;

#pragma warning disable CS0169

[assembly:
    AspectOrder(
        AspectOrderDirection.RunTime,
        typeof(FieldAspectTest),
        typeof(FieldOrPropertyAspectTest),
        typeof(IndexerAspectTest),
        typeof(PropertyAspectTest) )]

namespace Metalama.Framework.Tests.AspectTests.Aspects.Diagnostics.IncorrectMetaMember;

internal abstract class AspectBase : Aspect, IAspect<IFieldOrPropertyOrIndexer>
{
    public void BuildAspect( IAspectBuilder<IFieldOrPropertyOrIndexer> builder )
    {
        builder.OverrideAccessors( getTemplate: nameof(Template) );
    }

    public void BuildEligibility( IEligibilityBuilder<IFieldOrPropertyOrIndexer> builder ) { }

    protected abstract string TargetName { get; }

    [Template]
    private dynamic? Template()
    {
        Console.WriteLine( TargetName );

        return meta.Proceed();
    }
}

internal class FieldAspectTest : AspectBase
{
    [CompileTime]
    protected override string TargetName => meta.Target.Field.Name;
}

internal class PropertyAspectTest : AspectBase
{
    [CompileTime]
    protected override string TargetName => meta.Target.Property.Name;
}

internal class FieldOrPropertyAspectTest : AspectBase
{
    [CompileTime]
    protected override string TargetName => meta.Target.FieldOrProperty.Name;
}

internal class IndexerAspectTest : AspectBase
{
    [CompileTime]
    protected override string TargetName => meta.Target.Indexer.Name;
}


internal class TargetCode
{
    [FieldAspectTest]
    [PropertyAspectTest]
    [FieldOrPropertyAspectTest]
    [IndexerAspectTest]
    private int field;

    [FieldAspectTest]
    [PropertyAspectTest]
    [FieldOrPropertyAspectTest]
    [IndexerAspectTest]
    private int property { get; set; }

    [FieldAspectTest]
    [PropertyAspectTest]
    [FieldOrPropertyAspectTest]
    [IndexerAspectTest]
    private int this[ int i ] => 42;
}