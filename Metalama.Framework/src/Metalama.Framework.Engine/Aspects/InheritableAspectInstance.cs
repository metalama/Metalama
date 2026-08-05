// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Utilities;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Metalama.Framework.Engine.Aspects;

[Durable]
public sealed partial class InheritableAspectInstance : IAspectInstance, IAspectPredecessorImpl
{
    /// <remarks>
    /// The four members of this class that are typed as an interface, this one, <see cref="Aspect"/>,
    /// <see cref="AspectState"/> and <see cref="SecondaryInstances"/>, are all instances of the same open question,
    /// which is recorded under "Should the contract propagate to the user-implementable interfaces?" in
    /// <c>design-time-memory.md</c>. Marking the interface is a real remedy rather than a workaround, but for the
    /// user-facing ones it would require every aspect and every aspect state a user writes to be durable.
    /// </remarks>
    [SuppressMessage(
        "Metalama",
        "LAMA0876:An interface or abstract type used by a durable type is not marked [Durable]",
        Justification = "Marking IAspectClass propagates the obligation to AspectClass and to everything it holds." )]
    private readonly IAspectClass? _aspectClass;

    /// <summary>
    /// Gets the declaration to which the inherited aspect applies.
    /// </summary>
    /// <remarks>
    /// Typed <see cref="IDurableRef{T}"/> rather than <see cref="IRef{T}"/> so that the requirement is stated in the
    /// type instead of being left to the caller, as <c>design-time-memory.md</c> prescribes and as
    /// <c>TransitiveAspectInstance.TargetDeclaration</c> already does. The conversion then cannot be forgotten,
    /// because the compiler asks for it at every assignment, including the one in the deserializer.
    /// </remarks>
    public IDurableRef<IDeclaration> TargetDeclaration { get; private set; }

    IRef<IDeclaration> IAspectPredecessor.TargetDeclaration => this.TargetDeclaration;

    // This member is not available after deserialization. We would need, if necessary, to have a post-deserialization initialization.
    // The AspectClass is the full type of the aspect, anyway.
    public IAspectClass AspectClass => this._aspectClass ?? throw new InvalidOperationException();

    bool IAspectInstance.IsSkipped => false;

    public bool IsInheritable => true;

    [SuppressMessage(
        "Metalama",
        "LAMA0876:An interface or abstract type used by a durable type is not marked [Durable]",
        Justification =
            "The elements are themselves InheritableAspectInstance, so marking IAspectInstance is bounded work rather "
            + "than a contract change. See the remark on _aspectClass." )]
    public ImmutableArray<IAspectInstance> SecondaryInstances { get; private set; }

    ImmutableArray<AspectPredecessor> IAspectPredecessor.Predecessors => ImmutableArray<AspectPredecessor>.Empty;

    [SuppressMessage(
        "Metalama",
        "LAMA0876:An interface or abstract type used by a durable type is not marked [Durable]",
        Justification = "The aspect state is user code. See the remark on _aspectClass." )]
    public IAspectState? AspectState { get; private set; }

    [SuppressMessage(
        "Metalama",
        "LAMA0876:An interface or abstract type used by a durable type is not marked [Durable]",
        Justification = "The aspect is user code. See the remark on _aspectClass." )]
    public IAspect Aspect { get; private set; }

    public int PredecessorDegree { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="InheritableAspectInstance"/> class from an aspect instance of the
    /// current compilation.
    /// </summary>
    /// <remarks>
    /// The target declaration is converted to a durable reference. A reference obtained from the code model is bound
    /// to the compilation it came from, through the symbol it holds and through its reference factory, and this class
    /// is stored in the design-time results of a syntax tree, which the pipeline carries forward from one version of
    /// the project to the next without re-analysing the file. A compilation-bound reference here therefore keeps the
    /// compilation in which the aspect was found alive for as long as the project stays open. See issue #1793. The
    /// reference is serialized as a declaration identifier in any case, so this only makes the in-memory form agree
    /// with the serialized one.
    /// </remarks>
    public InheritableAspectInstance( IAspectInstance aspectInstance )
    {
        var asPredecessor = (IAspectPredecessorImpl) aspectInstance;
        this.TargetDeclaration = asPredecessor.TargetDeclaration.ToDurable();
        this.TargetDeclarationDepth = asPredecessor.TargetDeclarationDepth;
        this.Aspect = aspectInstance.Aspect;
        this._aspectClass = aspectInstance.AspectClass;
        this.AspectState = aspectInstance.AspectState;
        this.PredecessorDegree = aspectInstance.PredecessorDegree + 1;

        this.SecondaryInstances = aspectInstance.SecondaryInstances.Select( i => new InheritableAspectInstance( i ) )
            .ToImmutableArray<IAspectInstance>();
    }

    private InheritableAspectInstance()
    {
        // This is the deserializing constructors. Fields are set by the deserializer, but here
        // we are suppressing warnings.
        this.TargetDeclaration = null!;
        this.SecondaryInstances = default;
        this.Aspect = null!;
        this.PredecessorDegree = 0;
    }

    public override string ToString() => $"{nameof(InheritableAspectInstance)}, Aspect={this.Aspect}, Target={this.TargetDeclaration}";

    public FormattableString FormatPredecessor( ICompilation compilation )
        => $"aspect '{this.AspectClass.ShortName}' applied to '{this.TargetDeclaration.GetTarget( compilation )}'";

    Location? IAspectPredecessorImpl.GetDiagnosticLocation( Compilation compilation ) => null;

    public int TargetDeclarationDepth { get; }

    ImmutableArray<SyntaxTree> IAspectPredecessorImpl.PredecessorTreeClosure => ImmutableArray<SyntaxTree>.Empty;
}