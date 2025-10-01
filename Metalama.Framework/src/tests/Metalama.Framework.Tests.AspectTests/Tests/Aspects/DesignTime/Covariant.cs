// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.DesignTime.Covariant;

#if TEST_OPTIONS
// @TestScenario(DesignTime)
// @RequiredConstant(NET5_0_OR_GREATER)
#endif


using Metalama.Framework.Aspects; 
using Metalama.Framework.Advising;
using Metalama.Framework.Code;
using Metalama.Framework.Diagnostics;
using Metalama.Framework.Eligibility;

class MyAspect : TypeAspect
{
    public override void BuildAspect(IAspectBuilder<INamedType> builder)
    {
        base.BuildAspect(builder);

        var targetType = builder.Target;
        
        // Method
        builder.IntroduceMethod(
            nameof(this.GetCovariantMethodTemplate),
            whenExists: OverrideStrategy.Override,
            buildMethod: m =>
            {
                m.Name = "CovariantGetMethod";
                m.ReturnType = targetType.ToNullable();
            });
        builder.IntroduceMethod(
            nameof(this.GetNonCovariantMethodTemplate),
            whenExists: OverrideStrategy.Override,
            buildMethod: m =>
            {
                m.Name = "NonCovariantGetMethod";
            });
        
        // Property
        builder.IntroduceProperty(
            nameof(this.CovariantPropertyTemplate),
            whenExists: OverrideStrategy.Override,
            buildProperty: m =>
            {
                m.Name = "CovariantProperty";
                m.Type = targetType.ToNullable();
            });
        builder.IntroduceProperty(
            nameof(this.NonCovariantPropertyTemplate),
            whenExists: OverrideStrategy.Override,
            buildProperty: m =>
            {
                m.Name = "NonCovariantProperty";
            });
        
        // Indexer
        builder.IntroduceIndexer(
            typeof(int),
            nameof(this.CovariantIndexerTemplate),
            null,
            whenExists: OverrideStrategy.Override,
            buildIndexer: m =>
            {
                m.IsVirtual = true;
                m.Type = targetType.ToNullable();
            });
        builder.IntroduceIndexer(
            typeof(string),
            null,
            nameof(this.NonCovariantIndexerTemplate),
            whenExists: OverrideStrategy.Override,
            buildIndexer: m =>
            {
                m.IsVirtual = true;
            });
    }

    [Template]
    public virtual dynamic? GetCovariantMethodTemplate() => null;

    [Template]
    public virtual string GetNonCovariantMethodTemplate() => null;

    [Template]
    public virtual dynamic? CovariantPropertyTemplate => null;

    [Template]
    public virtual string NonCovariantPropertyTemplate => null;
    
    [Template]
    public virtual dynamic? CovariantIndexerTemplate(int index) => null;

    [Template]
    public virtual string NonCovariantIndexerTemplate(string index) => null;

}

// <target>
[MyAspect]
partial class B { }

// <target>
[MyAspect]
partial class D : B { }