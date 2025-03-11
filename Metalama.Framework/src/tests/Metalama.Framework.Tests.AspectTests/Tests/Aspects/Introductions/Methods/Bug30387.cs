// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Methods.Bug30387;

#pragma warning disable CS0169, CS8618

internal class InjectAttribute : FieldOrPropertyAspect
{
    [Introduce( WhenExists = OverrideStrategy.Ignore )]
    private readonly IServiceProvider? _serviceProvider;
}

// <target>
public class Commerce
{
    [Inject]
    private IDisposable? _BillingProcessor;

    [Inject]
    private IDisposable? _CustomerProcessor;

    [Inject]
    private IDisposable? _Notifier;
}