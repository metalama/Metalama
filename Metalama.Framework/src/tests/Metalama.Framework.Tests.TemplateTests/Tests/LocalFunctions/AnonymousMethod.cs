// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.AspectTests.Tests.Templating.LocalFunctions.AnonymousMethod;

[CompileTime]
internal class Aspect
{
    [TestTemplate]
    private dynamic? Template()
    {
        object? result = null;
        RunTimeClass.Execute( delegate { result = meta.Proceed(); } );

        return result;
    }
}

internal class RunTimeClass
{
    public static void Execute( Action action ) => action();
}

internal class TargetCode
{
    private int Method( int a )
    {
        return a;
    }
}