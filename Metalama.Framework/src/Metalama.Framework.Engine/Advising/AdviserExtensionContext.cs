// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.Extensibility;
using Metalama.Framework.Engine.Queries;
using Metalama.Framework.Fabrics;
using System;

namespace Metalama.Framework.Engine.Advising;

/// <summary>
/// Exposes to an extension the state of the engine behind an <see cref="IAdviser"/>.
/// </summary>
/// <remarks>
/// An instance references the compilation of the aspect layer, so it must not be stored in an object that outlives the pipeline execution.
/// Get an instance with <see cref="AdviserExtensibility.GetExtensionContext"/>.
/// </remarks>
[PublicAPI]
public sealed class AdviserExtensionContext
{
    /// <summary>
    /// The state of the advice factory behind the adviser.
    /// </summary>
    private readonly AdviceFactoryState _state;

    /// <summary>
    /// The template class instance set on the adviser, or <c>null</c> when templates are resolved against the aspect instance.
    /// </summary>
    private readonly TemplateClassInstance? _templateClassInstance;

    /// <summary>
    /// Initializes a new instance of the <see cref="AdviserExtensionContext"/> class.
    /// </summary>
    internal AdviserExtensionContext(
        IQueryOwner owner,
        AdviceFactoryState state,
        TemplateClassInstance? templateClassInstance,
        IDeclaration aspectTarget )
    {
        this.Owner = owner;
        this._state = state;
        this._templateClassInstance = templateClassInstance;
        this.AspectTarget = aspectTarget;
    }

    /// <summary>
    /// Gets the owner of the contributions made through the adviser: the aspect builder for an aspect, or the amender for a type fabric. The
    /// predecessor of the contributions is the <see cref="IQueryOwner.AspectPredecessor"/> of the owner.
    /// </summary>
    public IQueryOwner Owner { get; }

    /// <summary>
    /// Gets the target declaration of the aspect instance whose <c>BuildAspect</c> method is executing, in the compilation of the adviser.
    /// For a type fabric, this is the target type of the fabric.
    /// </summary>
    public IDeclaration AspectTarget { get; }

    /// <summary>
    /// Gets the template provider against which template names are resolved. It takes
    /// <see cref="AdviserExtensions.WithTemplateProvider{TDeclaration}(IAdviser{TDeclaration}, in TemplateProvider)"/> into account.
    /// </summary>
    public TemplateProvider TemplateProvider => this._templateClassInstance?.TemplateProvider ?? TemplateProvider.FromInstance( this._state.AspectInstance.Aspect );

    /// <summary>
    /// Throws an <see cref="ObjectDisposedException"/> when the aspect or fabric has finished executing.
    /// </summary>
    public void ThrowIfDisposed()
    {
        if ( this._state.IsDisposed )
        {
            throw new ObjectDisposedException(
                nameof(IAdviser),
                "The adviser can no longer be used, because the aspect or fabric that received it has finished executing." );
        }
    }

    /// <summary>
    /// Creates a query that selects a single declaration and whose owner is <see cref="Owner"/>.
    /// </summary>
    /// <param name="declaration">The declaration to select.</param>
    public IQuery<T> CreateQuery<T>( T declaration )
        where T : class, IDeclaration
        => new RootQuery<T>( declaration.ToRef(), this.Owner, CompilationModelVersion.Current );

    /// <summary>
    /// Captures the attribution information of a contribution made now through the adviser.
    /// </summary>
    /// <exception cref="ObjectDisposedException">The aspect or fabric that received the adviser has finished executing.</exception>
    public ExtensionContributionOrigin CaptureOrigin()
    {
        this.ThrowIfDisposed();

        return new ExtensionContributionOrigin(
            this.Owner.AspectPredecessor,
            this.Owner.DiagnosticSourceDescription,
            this.TemplateProvider,
            this._templateClassInstance,
            this._state.AspectLayerInstance.AspectLayerId,
            this._state.AspectInstance );
    }
}
