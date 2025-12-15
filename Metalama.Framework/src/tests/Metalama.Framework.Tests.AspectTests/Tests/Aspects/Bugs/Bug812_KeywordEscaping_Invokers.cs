// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @IgnoredDiagnostic(CS0649)
// @IgnoredDiagnostic(CS0067)
#endif

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.Invokers;
using System;
using System.Linq;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug812_KeywordEscaping_Invokers;

// Issue #812: Tests invokers referencing members with C# keyword names

internal class InvokeKeywordFieldAspect : FieldOrPropertyAspect
{
    public override void BuildAspect( IAspectBuilder<IFieldOrProperty> builder )
    {
        builder.OverrideAccessors(
            nameof(GetTemplate),
            nameof(SetTemplate),
            new { target = builder.Target.DeclaringType!.Fields.OfName( "const" ).Single() } );
    }

    [Template]
    public dynamic? GetTemplate( [CompileTime] IField target )
    {
        // Invoke field with keyword name via invoker
        _ = target.Value;
        _ = target.WithOptions( InvokerOptions.Final ).Value;

        return meta.Proceed();
    }

    [Template]
    public void SetTemplate( [CompileTime] IField target )
    {
        // Invoke field with keyword name via invoker
        target.Value = 42;
        target.WithOptions( InvokerOptions.Final ).Value = 42;

        meta.Proceed();
    }
}

internal class InvokeKeywordPropertyAspect : FieldOrPropertyAspect
{
    public override void BuildAspect( IAspectBuilder<IFieldOrProperty> builder )
    {
        builder.OverrideAccessors(
            nameof(GetTemplate),
            nameof(SetTemplate),
            new { target = builder.Target.DeclaringType!.Properties.OfName( "int" ).Single() } );
    }

    [Template]
    public dynamic? GetTemplate( [CompileTime] IProperty target )
    {
        // Invoke property with keyword name via invoker
        _ = target.Value;

        return meta.Proceed();
    }

    [Template]
    public void SetTemplate( [CompileTime] IProperty target )
    {
        // Invoke property with keyword name via invoker
        target.Value = "test";

        meta.Proceed();
    }
}

internal class InvokeKeywordMethodAspect : MethodAspect
{
    public override void BuildAspect( IAspectBuilder<IMethod> builder )
    {
        builder.Override( nameof(OverrideTemplate), new { target = builder.Target.DeclaringType!.Methods.OfName( "void" ).Single() } );
    }

    [Template]
    public dynamic? OverrideTemplate( [CompileTime] IMethod target )
    {
        // Invoke method with keyword name via invoker
        target.Invoke();

        return meta.Proceed();
    }
}

// <target>
internal class TargetClass
{
    public int @const;
    public string @int { get; set; }
    public void @void() { }
    public void MethodWithParam( string @class ) { }
    public event Action @event;

    [InvokeKeywordFieldAspect]
    public int FieldInvoker { get; set; }

    [InvokeKeywordPropertyAspect]
    public string PropertyInvoker { get; set; }

    [InvokeKeywordMethodAspect]
    public void MethodInvoker() { }
}
