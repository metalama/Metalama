// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Engine.Extensibility;
using Microsoft.CodeAnalysis;
using System.Diagnostics.CodeAnalysis;

namespace Metalama.Framework.Engine.Aspects;

internal sealed class TransitiveAspectInstance : ITransitivePipelineContributor, IExtensionPipelineContributor, IDesignTimePipelineResultExtension
{
    internal TransitiveAspectInstance(
        IAspect aspect,
        IDurableRef<IDeclaration> targetDeclaration,
        int targetDeclarationDepth,
        IAspectClassImpl aspectClass,
        IAspectState? aspectState,
        int predecessorDegree,
        SyntaxTree? syntaxTree )
    {
        this.Aspect = aspect;
        this.TargetDeclaration = targetDeclaration;
        this.AspectClass = aspectClass;
        this.TargetDeclarationDepth = targetDeclarationDepth;
        this.PredecessorDegree = predecessorDegree;
        this.AspectState = aspectState;
        this.SyntaxTree = syntaxTree;
    }

    public int PredecessorDegree { get; }

    /// <summary>
    /// Gets the declaration this aspect instance applies to.
    /// </summary>
    /// <remarks>
    /// Typed as <see cref="IDurableRef{T}"/> rather than <see cref="IRef{T}"/> on purpose. An instance of this class is
    /// stored in the design-time result of the file declaring its target, and the pipeline carries that result forward
    /// to every later version of the project, so a compilation-bound reference here would pin the version it was
    /// produced in for the whole editing session. Declaring the requirement in the type makes it the compiler's
    /// business rather than the caller's discipline. See issue #1797.
    /// </remarks>
    public IDurableRef<IDeclaration> TargetDeclaration { get; }

    [SuppressMessage(
        "Metalama",
        "LAMA0876:The durability of a type cannot be established",
        Justification =
            "The aspect is user code. The route by which it reaches a compilation is the open item tracked by #1797, "
            + "and is recorded in design-time-memory.md. See #1830." )]
    public IAspect Aspect { get; }

    [SuppressMessage(
        "Metalama",
        "LAMA0876:The durability of a type cannot be established",
        Justification =
            "An interface, so what an implementation holds cannot be established here. The known retention through "
            + "the template parameters was closed by #1803. Whether the type can be constrained is decided in #1830." )]
    public IAspectClassImpl AspectClass { get; }

    [SuppressMessage(
        "Metalama",
        "LAMA0876:The durability of a type cannot be established",
        Justification =
            "The aspect state is user code, so what it holds cannot be established here. Whether a constraint can be "
            + "stated for it is decided in #1830." )]
    public IAspectState? AspectState { get; }

    public int TargetDeclarationDepth { get; }

    /// <remarks>
    /// This member belongs to <see cref="ITransitivePipelineContributor"/>, which is under no durability constraint,
    /// but <see cref="ToDesignTime"/> returns <c>this</c>, so it survives into the object that the design-time
    /// pipeline stores per file. That is a real retention, and it is tracked by #1830.
    /// </remarks>
    [SuppressMessage(
        "Metalama",
        "LAMA0870:A member of a durable type is not durable",
        Justification =
            "A real retention, not a false positive. Separating the design-time form from the contributor also has to "
            + "keep the transitive manifest working and flips a control in TransitiveContributorMemoryLeakTests, so it "
            + "is done under #1830 rather than here." )]
    public SyntaxTree? SyntaxTree { get; }

    public IDesignTimePipelineResultExtension ToDesignTime() => this;

    public ContributorKind ContributorKind => ContributorKind.TransitiveAspectInstance;

    ITransitiveAspectsManifestExtension IDesignTimePipelineResultExtension.ToTransitiveAspectManifestExtension()
        => new SerializableTransitiveAspectInstance( this );
}