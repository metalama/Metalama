// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @TestScenario(DesignTime)
#endif

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Initialization.OnConstructed_Forwarder_WithBeforeInstanceConstructor_DesignTime;

[Inheritable]
public class TheAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        if ( builder.Target.TypeKind == TypeKind.Interface )
        {
            return;
        }

        builder.AddInitializer( nameof(BeforeTemplate), InitializerKind.BeforeInstanceConstructor );
        builder.AddInitializer( nameof(AfterTemplate), InitializerKind.AfterLastInstanceConstructor );
        builder.AddInitializer( nameof(InitializeTemplate), InitializerKind.AfterObjectInitializer );
    }

    [Template]
    private void BeforeTemplate()
    {
        Console.WriteLine( $"BeforeInstanceConstructor {meta.Target.Type.Name}" );
    }

    [Template]
    private void AfterTemplate()
    {
        Console.WriteLine( $"AfterLastInstanceConstructor {meta.Target.Type.Name}" );
    }

    [Template]
    private void InitializeTemplate()
    {
        Console.WriteLine( $"AfterObjectInitializer {meta.Target.Type.Name}" );
    }
}

[TheAspect]
public interface ISecurable
{
}

// <target>
public partial class Document : ISecurable
{
}

// <target>
public partial class Report : ISecurable
{
    public Report()
    {
    }

    public Report( string title )
    {
        _ = title;
    }
}

// <target>
public partial class WeightedReport : Report
{
}

internal class Usage
{
    private static T Create<T>()
        where T : new()
        => new T();

    public void M()
    {
        _ = Create<Document>();
        _ = Create<Report>();
        _ = Create<WeightedReport>();
    }
}
