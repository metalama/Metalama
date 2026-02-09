// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.DeclarationBuilders;
using System.Linq;
using Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Classes.AwaitableType;

[assembly: AspectOrder( AspectOrderDirection.RunTime, typeof(OverrideAttribute), typeof(IntroductionAttribute) )]

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Classes.AwaitableType;

/// <summary>
/// Introduces an awaitable type with a full async method builder pattern and a method returning it.
/// Applied first in the pipeline.
/// </summary>
public class IntroductionAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        // Introduce the awaiter type implementing INotifyCompletion with all required members per C# spec.
        var awaiterResult = builder.IntroduceClass( "MyAwaiter", buildType: t => { t.Accessibility = Code.Accessibility.Public; } );

        awaiterResult.ImplementInterface( typeof(INotifyCompletion) );

        builder.With( awaiterResult.Declaration ).IntroduceMethod(
            nameof(GetResult),
            buildMethod: b => { b.Accessibility = Code.Accessibility.Public; } );

        builder.With( awaiterResult.Declaration ).IntroduceProperty(
            nameof(IsCompleted),
            buildProperty: b => { b.Accessibility = Code.Accessibility.Public; } );

        builder.With( awaiterResult.Declaration ).IntroduceMethod(
            nameof(OnCompleted),
            buildMethod: b => { b.Accessibility = Code.Accessibility.Public; },
            whenExists: OverrideStrategy.Ignore );

        // Introduce the awaitable type with GetAwaiter() returning the introduced awaiter.
        var awaitableResult = builder.IntroduceClass( "MyAwaitable", buildType: t => { t.Accessibility = Code.Accessibility.Public; } );

        builder.With( awaitableResult.Declaration ).IntroduceMethod(
            nameof(DefaultTemplate),
            buildMethod: b =>
            {
                b.Name = "GetAwaiter";
                b.Accessibility = Code.Accessibility.Public;
                b.ReturnType = awaiterResult.Declaration;
            } );

        // Introduce the async method builder type with all required members.
        var builderResult = builder.IntroduceClass( "MyAwaitableMethodBuilder", buildType: t => { t.Accessibility = Code.Accessibility.Public; } );

        builder.With( builderResult.Declaration ).IntroduceMethod(
            nameof(DefaultTemplate),
            buildMethod: b =>
            {
                b.Name = "Create";
                b.Accessibility = Code.Accessibility.Public;
                b.IsStatic = true;
                b.ReturnType = builderResult.Declaration;
            } );

        builder.With( builderResult.Declaration ).IntroduceMethod(
            nameof(EmptyTemplate),
            buildMethod: b =>
            {
                b.Name = "SetResult";
                b.Accessibility = Code.Accessibility.Public;
            } );

        builder.With( builderResult.Declaration ).IntroduceMethod(
            nameof(EmptyTemplate),
            buildMethod: b =>
            {
                b.Name = "SetException";
                b.Accessibility = Code.Accessibility.Public;
                b.AddParameter( "exception", typeof(Exception) );
            } );

        builder.With( builderResult.Declaration ).IntroduceMethod(
            nameof(EmptyTemplate),
            buildMethod: b =>
            {
                b.Name = "SetStateMachine";
                b.Accessibility = Code.Accessibility.Public;
                b.AddParameter( "stateMachine", typeof(IAsyncStateMachine) );
            } );

        builder.With( builderResult.Declaration ).IntroduceMethod(
            nameof(EmptyTemplate),
            buildMethod: b =>
            {
                b.Name = "Start";
                b.Accessibility = Code.Accessibility.Public;
                var typeParam = b.AddTypeParameter( "TStateMachine" );
                typeParam.AddTypeConstraint( typeof(IAsyncStateMachine) );
                b.AddParameter( "stateMachine", typeParam, RefKind.Ref );
            } );

        builder.With( builderResult.Declaration ).IntroduceMethod(
            nameof(EmptyTemplate),
            buildMethod: b =>
            {
                b.Name = "AwaitOnCompleted";
                b.Accessibility = Code.Accessibility.Public;
                var tAwaiter = b.AddTypeParameter( "TAwaiter" );
                tAwaiter.AddTypeConstraint( typeof(INotifyCompletion) );
                var tStateMachine = b.AddTypeParameter( "TStateMachine" );
                tStateMachine.AddTypeConstraint( typeof(IAsyncStateMachine) );
                b.AddParameter( "awaiter", tAwaiter, RefKind.Ref );
                b.AddParameter( "stateMachine", tStateMachine, RefKind.Ref );
            } );

        builder.With( builderResult.Declaration ).IntroduceMethod(
            nameof(EmptyTemplate),
            buildMethod: b =>
            {
                b.Name = "AwaitUnsafeOnCompleted";
                b.Accessibility = Code.Accessibility.Public;
                var tAwaiter = b.AddTypeParameter( "TAwaiter" );
                tAwaiter.AddTypeConstraint( typeof(ICriticalNotifyCompletion) );
                var tStateMachine = b.AddTypeParameter( "TStateMachine" );
                tStateMachine.AddTypeConstraint( typeof(IAsyncStateMachine) );
                b.AddParameter( "awaiter", tAwaiter, RefKind.Ref );
                b.AddParameter( "stateMachine", tStateMachine, RefKind.Ref );
            } );

        builder.With( builderResult.Declaration ).IntroduceProperty(
            nameof(DefaultPropertyTemplate),
            buildProperty: b =>
            {
                b.Name = "Task";
                b.Accessibility = Code.Accessibility.Public;
                b.Type = awaitableResult.Declaration;
            } );

        // Add [AsyncMethodBuilder(typeof(MyAwaitableMethodBuilder))] to MyAwaitable.
        builder.With( awaitableResult.Declaration ).IntroduceAttribute(
            AttributeConstruction.Create(
                typeof(AsyncMethodBuilderAttribute),
                constructorArguments: new object?[] { builderResult.Declaration } ) );

        // Introduce a method returning the introduced awaitable type.
        builder.IntroduceMethod(
            nameof(DefaultTemplate),
            buildMethod: b =>
            {
                b.Name = "GetValue";
                b.Accessibility = Code.Accessibility.Public;
                b.ReturnType = awaitableResult.Declaration;
            } );
    }

    [Template]
    public void GetResult()
    {
    }

    [Template]
    public bool IsCompleted => true;

    [Template]
    public void OnCompleted( Action continuation )
    {
    }

    [Template]
    public void EmptyTemplate()
    {
    }

    [Template]
    public dynamic? DefaultTemplate()
    {
        return default;
    }

    [Template]
    public dynamic? DefaultPropertyTemplate => default;
}

/// <summary>
/// Overrides the introduced method with an async template using await meta.ProceedAsync().
/// Applied second in the pipeline. Because MyAwaitable is recognized as awaitable
/// (via TryGetAsyncInfoFromCodeModel) and has AsyncMethodBuilderAttribute, the pipeline
/// correctly handles the async override.
/// </summary>
public class OverrideAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        var method = builder.Target.Methods.OfName( "GetValue" ).Single();

        builder.With( method ).Override( nameof(OverrideTemplate) );
    }

    [Template]
    public async Task<dynamic?> OverrideTemplate()
    {
        Console.WriteLine( "Override" );

        return await meta.ProceedAsync();
    }
}

// <target>
[IntroductionAttribute]
[OverrideAttribute]
public class TargetType { }
