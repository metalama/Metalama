// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @RequiredConstant(NET6_0_OR_GREATER)
#endif

#if NET6_0_OR_GREATER
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Interfaces.IntroduceIndexerPrivate;

public class IntroductionAttribute : TypeAspect
{
    public override void BuildAspect(IAspectBuilder<INamedType> builder)
    {
        var @interface = builder.IntroduceInterface( "ITest");
        var interfacePrivateIndexer = @interface.IntroduceIndexer(typeof(int), nameof(TestIndexerGet), nameof(TestIndexerSet));
        @interface.IntroduceMethod( nameof(TestUsageMethod), args: new { privateIndexer = interfacePrivateIndexer.Declaration } );

    }

    [Template]
    private int TestIndexerGet()
    {
        Console.WriteLine("Default");
        return 0;
    }

    [Template]
    private void TestIndexerSet(int value)
    {
        Console.WriteLine("Default");
    }

    [Template]
    public virtual void TestUsageMethod( [CompileTime] IIndexer privateIndexer)
    {
        privateIndexer.SetValue(privateIndexer.GetValue(42) + 1, 42);
    }
}

// <target>
[IntroductionAttribute]
public class TargetType { }
#endif