// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.Diagnostics;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace Metalama.Framework.Engine.Aspects;

/// <summary>
/// Collects the outgoing actions of a fabric or aspect (in user code, accessed through <see cref="IAspectBuilder{TAspectTarget}.Outbound"/>
/// i.e. child aspects, aspect exclusions, validators, or options.
/// </summary>
internal sealed class AspectInstanceCollector
{
    private volatile ConcurrentQueue<AspectInstance>? _aspectInstances;
    private volatile ConcurrentQueue<IRef<IDeclaration>>? _exclusions;
    private volatile ConcurrentQueue<AspectRequirement>? _requirements;

    public IAspectClass AspectClass { get; }

    public CompilationModel Compilation { get; }

    public IDiagnosticAdder Diagnostics { get; }

    public CancellationToken CancellationToken { get; }

    public AspectInstanceCollector(
        IAspectClass aspectClass,
        CompilationModel compilation,
        IDiagnosticAdder diagnostics,
        CancellationToken cancellationToken )
    {
        this.AspectClass = aspectClass;
        this.Compilation = compilation;
        this.Diagnostics = diagnostics;
        this.CancellationToken = cancellationToken;
    }

    internal IReadOnlyCollection<AspectInstance> AspectInstances
        => this._aspectInstances ?? (IReadOnlyCollection<AspectInstance>) Array.Empty<AspectInstance>();

    internal IReadOnlyCollection<IRef<IDeclaration>> AspectExclusions
        => this._exclusions ?? (IReadOnlyCollection<IRef<IDeclaration>>) Array.Empty<IRef<IDeclaration>>();

    internal IReadOnlyCollection<AspectRequirement> AspectRequirements
        => this._requirements ?? (IReadOnlyCollection<AspectRequirement>) Array.Empty<AspectRequirement>();

    public void AddAspectInstance( AspectInstance aspectInstance )
    {
        var aspectInstances = this._aspectInstances;

        if ( aspectInstances == null )
        {
            Interlocked.CompareExchange( ref this._aspectInstances, new ConcurrentQueue<AspectInstance>(), null );
            aspectInstances = this._aspectInstances;
        }

        aspectInstances.Enqueue( aspectInstance );
    }

    public void AddExclusion( IRef<IDeclaration> exclusion )
    {
        var exclusions = this._exclusions;

        if ( exclusions == null )
        {
            Interlocked.CompareExchange( ref this._exclusions, new ConcurrentQueue<IRef<IDeclaration>>(), null );
            exclusions = this._exclusions;
        }

        exclusions.Enqueue( exclusion );
    }

    public void AddAspectRequirement( AspectRequirement requirement )
    {
        var requirements = this._requirements;

        if ( requirements == null )
        {
            Interlocked.CompareExchange( ref this._requirements, new ConcurrentQueue<AspectRequirement>(), null );
            requirements = this._requirements;
        }

        requirements.Enqueue( requirement );
    }
}