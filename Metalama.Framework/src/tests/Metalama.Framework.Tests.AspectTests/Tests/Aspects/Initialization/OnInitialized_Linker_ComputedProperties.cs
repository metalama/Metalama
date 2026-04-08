// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

// Spec §11: Computed Derived Properties — hand-authored IInitializable with inheritance and with expression.

using Metalama.Framework.RunTime.Initialization;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Initialization.OnInitialized_Linker_ComputedProperties;

public record Isotope : IInitializable
{
    public required double HalfLifeSeconds { get; init; }

    public required int AtomicNumber { get; init; }

    public required int MassNumber { get; init; }

    public double DecayConstant { get; private set; }

    public double MeanLifetime { get; private set; }

    public virtual void Initialize( InitializationContext context )
    {
        DecayConstant = Math.Log( 2 ) / HalfLifeSeconds;
        MeanLifetime = 1.0 / DecayConstant;
    }
}

public record RadioactiveSample : Isotope
{
    public required double InitialAtoms { get; init; }

    public double Activity { get; private set; }

    public override void Initialize( InitializationContext context )
    {
        base.Initialize( context.Descend() );
        Activity = DecayConstant * InitialAtoms;
    }
}

// <target>
public class Caller
{
    public void Method()
    {
        // Object initializer with inheritance — no cast needed with generic Initialized<T>
        var sample = new RadioactiveSample
        {
            HalfLifeSeconds = 1.808e11,
            AtomicNumber = 6,
            MassNumber = 14,
            InitialAtoms = 1e24
        };

        // with expression — recomputes derived values
        var adjusted = sample with { InitialAtoms = 5e23 };
    }
}
