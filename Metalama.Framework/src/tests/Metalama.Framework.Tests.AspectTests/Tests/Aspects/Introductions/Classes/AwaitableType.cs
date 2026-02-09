// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Threading.Tasks;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Classes.AwaitableType;

public class IntroductionAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        // Introduce the awaiter type with all required members per C# spec.
        var awaiterResult = builder.IntroduceClass( "MyAwaiter", buildType: t => { t.Accessibility = Code.Accessibility.Public; } );

        builder.With( awaiterResult.Declaration ).IntroduceMethod(
            nameof(GetResult),
            buildMethod: b => { b.Accessibility = Code.Accessibility.Public; } );

        builder.With( awaiterResult.Declaration ).IntroduceProperty(
            nameof(IsCompleted),
            buildProperty: b => { b.Accessibility = Code.Accessibility.Public; } );

        builder.With( awaiterResult.Declaration ).IntroduceMethod(
            nameof(OnCompleted),
            buildMethod: b => { b.Accessibility = Code.Accessibility.Public; } );

        // Introduce the awaitable type with GetAwaiter() returning the introduced awaiter.
        var awaitableResult = builder.IntroduceClass( "MyAwaitable", buildType: t => { t.Accessibility = Code.Accessibility.Public; } );

        builder.With( awaitableResult.Declaration ).IntroduceMethod(
            nameof(GetAwaiterTemplate),
            buildMethod: b =>
            {
                b.Name = "GetAwaiter";
                b.Accessibility = Code.Accessibility.Public;
                b.ReturnType = awaiterResult.Declaration;
            } );

        // Introduce an async method that returns Task<int> and override it with an async template.
        // This demonstrates the async pipeline works for introduced methods alongside introduced awaitable types.
        var methodResult = builder.IntroduceMethod(
            nameof(GetValueAsyncTemplate),
            buildMethod: b =>
            {
                b.Name = "GetValueAsync";
                b.Accessibility = Code.Accessibility.Public;
            } );

        builder.With( methodResult.Declaration ).Override( nameof(OverrideAsyncTemplate) );
    }

    [Template]
    public int GetResult()
    {
        return default;
    }

    [Template]
    public bool IsCompleted => true;

    [Template]
    public void OnCompleted( Action continuation )
    {
    }

    [Template]
    public dynamic? GetAwaiterTemplate()
    {
        return default;
    }

    [Template]
    public async Task<int> GetValueAsyncTemplate()
    {
        await Task.Yield();

        return 42;
    }

    [Template]
    public async Task<dynamic?> OverrideAsyncTemplate()
    {
        Console.WriteLine( "Override" );

        return await meta.ProceedAsync();
    }
}

// <target>
[IntroductionAttribute]
public class TargetType { }
