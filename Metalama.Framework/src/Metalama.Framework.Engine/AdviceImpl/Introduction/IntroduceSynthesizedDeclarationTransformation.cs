// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel.Introductions.BuilderData;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.Transformations;
using Metalama.Framework.Introspection;
using System;

namespace Metalama.Framework.Engine.AdviceImpl.Introduction;

/// <summary>
/// Registers a declaration in the code model without emitting any syntax for it.
/// </summary>
/// <remarks>
/// <para>
/// This transformation serves a member that the compiler synthesizes from a declaration that Metalama emits: the
/// <c>Invoke</c> method of a delegate, the <c>EqualityContract</c> property and the clone method of a record, the
/// <c>Value</c> property and the per-case constructors of a union, and the parameterless constructor of a struct.
/// Such a member has to exist in the code model, because the introduction pipeline never re-reads the final model
/// from Roslyn, and must not be emitted, because the compiler creates it from the declaration and a member that is
/// registered and also emitted is declared twice.
/// </para>
/// <para>
/// The mechanism is the absence of <see cref="IInjectMemberTransformation"/>. Both emitters are keyed on that
/// interface: <c>LinkerInjectionStep</c> at build time, and <c>DesignTimeSyntaxTreeGenerator</c> at design time,
/// whose switch emits the arm for that interface alone. Not implementing it therefore satisfies both at once, and
/// neither needs a rule of its own. <see cref="IntroduceDeclarationTransformation{T}"/> implements both interfaces
/// and must not be used for a synthesized member.
/// </para>
/// <para>
/// <see cref="TransformationObservability.Always"/> is what puts the declaration in the code model, because
/// <c>CompilationModel.AddTransformation</c> ignores a transformation whose observability is
/// <see cref="TransformationObservability.None"/>. The value decides whether the transformation reaches the code
/// model and the design-time pipeline, and not whether syntax is produced, which the interface decides.
/// </para>
/// <para>
/// The base class is <see cref="BaseTransformation"/> and not <see cref="BaseSyntaxTreeTransformation"/>, because
/// this transformation produces no syntax and therefore has no syntax tree to name. The declaration nevertheless
/// enters the map from builder data to transformation that <c>LinkerInjectionStep</c> builds, because a
/// transformation that replaces the declaration resolves it through that map, which happens when an aspect
/// introduces a field into an introduced struct and the implicit parameterless constructor of that struct is
/// replaced. <c>LinkerInjectionStep</c> indexes such a transformation in a pass of its own, rather than in the pass
/// that groups transformations by syntax tree.
/// </para>
/// </remarks>
/// <seealso href="@introducing-types"/>
internal sealed class IntroduceSynthesizedDeclarationTransformation : BaseTransformation, IIntroduceDeclarationTransformation
{
    private readonly NamedDeclarationBuilderData _introducedDeclaration;

    public IntroduceSynthesizedDeclarationTransformation( AspectLayerInstance aspectLayerInstance, NamedDeclarationBuilderData introducedDeclaration ) :
        base( aspectLayerInstance )
    {
        this._introducedDeclaration = introducedDeclaration.AssertNotNull();
    }

    public override TransformationObservability Observability => TransformationObservability.Always;

    DeclarationBuilderData IIntroduceDeclarationTransformation.DeclarationBuilderData => this._introducedDeclaration;

    public override IFullRef<IDeclaration> TargetDeclaration => this._introducedDeclaration.ContainingDeclaration.AssertNotNull();

    public override IntrospectionTransformationKind TransformationKind => IntrospectionTransformationKind.IntroduceMember;

    public override FormattableString ToDisplayString()
        => $"Register the synthesized {this._introducedDeclaration.DeclarationKind} '{this._introducedDeclaration}' in the code model.";
}
