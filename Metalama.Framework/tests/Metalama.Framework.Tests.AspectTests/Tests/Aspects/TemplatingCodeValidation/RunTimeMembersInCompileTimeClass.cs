// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.TemplatingCodeValidation.RunTimeMembersInCompileTimeClass;

#pragma warning disable CS8618, CS0067, CS0169

[CompileTime]
public class CompileTimeClass
{
    // The following members are implicitly CompileTime because they are unmarked and they are contained in a compile-time type.
    private RunTimeClass _runTimeField;

    private RunTimeClass RunTimeProperty { get; set; }

    private void RunTimeMethod( RunTimeClass c ) { }

    private event Action<RunTimeClass> RunTimeEvent;
}

public class RunTimeClass { }