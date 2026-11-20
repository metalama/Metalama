// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

// The metalamaTests.json file of this directory enables the RunAnalyzers option. The aspect declares an introduced field
// of a run-time-only interface type. The field is generated into the target and is not held by the aspect instance, so
// the DurableContractAnalyzer does not report it, although the aspect is bound by the [Durable] contract of IAspect.

using Metalama.Framework.Aspects;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.TestFramework.RunAnalyzersEnabled.RunAnalyzers_AdviceMembers;

public interface IMessageWriter
{
    void Write( string message );
}

public class LogAttribute : OverrideMethodAspect
{
    [Introduce]
    private readonly IMessageWriter? _messageWriter;

    public override dynamic? OverrideMethod()
    {
        this._messageWriter?.Write( $"Entering {meta.Target.Method}." );

        return meta.Proceed();
    }
}

// <target>
public partial class Customer
{
    [Log]
    public void Save() => Console.WriteLine( "Saving." );
}
