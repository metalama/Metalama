// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @RequiredConstant(NET6_0_OR_GREATER)
#endif

#if NET6_0_OR_GREATER
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.SyntaxBuilders;
using System;
using System.Linq;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Interfaces.IntroducePropertyStaticVirtual;

public class IntroductionAttribute : TypeAspect
{
    public override void BuildAspect(IAspectBuilder<INamedType> builder)
    {
        var @interface = builder.IntroduceInterface( "ITest");
        @interface.IntroduceProperty( nameof(TestProperty), buildProperty: b => { b.IsVirtual = true; });

        // Implementation type.
        var implementation = builder.IntroduceClass("TestImplementation");
        implementation.ImplementInterface( @interface.Declaration);
        var implementationProperty = implementation.IntroduceProperty( nameof(TestPropertyImplementation), buildProperty: b => { b.Name = nameof(TestProperty); });

        // Usage type.
        var usage = builder.Advice.IntroduceClass(
            builder.Target,
            "TestUsage",
            buildType: b =>
            {
                var typeParam = b.AddTypeParameter("T");
                typeParam.AddTypeConstraint(@interface.Declaration);
            });

        // Method that uses the interface.
        builder.Advice.IntroduceMethod(
            usage.Declaration,
            nameof(TestUsageMethod),
            args: new
            {
                genericParameter = usage.Declaration.TypeParameters.Single(),
                interfaceProperty = @interface.Declaration.Properties.Single(),
                implementationProperty = implementationProperty.Declaration,
            });
    }

    [Template]
    public static int TestProperty
    {
        get
        {
            Console.WriteLine("Default");
            return 0;
        }
        set
        {
            Console.WriteLine("Default");
        }
    }

    [Template]
    public static int TestPropertyImplementation
    {
        get
        {
            Console.WriteLine("Implementation");
            return 0;
        }

        set
        {
            Console.WriteLine("Implementation");
        }
    }

    [Template]
    public static void TestUsageMethod([CompileTime] ITypeParameter genericParameter, [CompileTime] IProperty interfaceProperty, [CompileTime] IProperty implementationProperty)
    {
        // Calling static members of type parameters is not currently supported (generic parameter does not "gain" members from constraints).
        ExpressionBuilder builder = new ExpressionBuilder();
        builder.AppendTypeName(genericParameter);
        builder.AppendVerbatim($".{interfaceProperty.Name}");
        builder.AppendVerbatim($" = ");
        builder.AppendTypeName(genericParameter);
        builder.AppendVerbatim($".{interfaceProperty.Name}");
        builder.AppendVerbatim($" + 1");
        meta.InsertStatement(builder.ToExpression());

        implementationProperty.Value = implementationProperty.Value + 1;
    }
}

// <target>
[IntroductionAttribute]
public class TargetType { }
#endif