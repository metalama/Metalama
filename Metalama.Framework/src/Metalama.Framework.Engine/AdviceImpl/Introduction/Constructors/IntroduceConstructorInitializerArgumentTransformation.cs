// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.Transformations;
using Metalama.Framework.Introspection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;

namespace Metalama.Framework.Engine.AdviceImpl.Introduction.Constructors;

/// <summary>
/// A transformation that appends an argument to the initializer call of a constructor.
/// When <see cref="IsOverride"/> is set, the transformation replaces any earlier-appended
/// argument targeting the same parameter index — used by the <c>OnConstructed</c> advice
/// to rewrite the <c>context</c> argument pulled into a derived <c>:base(...)</c> call so it
/// descends with the <c>OnConstructed</c> slot.
/// </summary>
internal sealed class IntroduceConstructorInitializerArgumentTransformation : BaseSyntaxTreeTransformation, IMemberLevelTransformation
{
    private readonly IFullRef<IConstructor> _constructor;
    private readonly bool _requiresParameterName;

    IFullRef<IMember> IMemberLevelTransformation.TargetMember => this._constructor;

    public int ParameterIndex { get; }

    public string ParameterName { get; }

    /// <summary>
    /// Gets a value indicating whether this transformation should replace any earlier-appended
    /// argument targeting the same <see cref="ParameterIndex"/>. When <c>false</c> (the default),
    /// the transformation is a plain append.
    /// </summary>
    public bool IsOverride { get; }

    /// <summary>
    /// Gets the name of an aspect-introduced parameter on the target (caller) constructor that should
    /// be forwarded as this argument's value, if such a parameter exists at
    /// <c>LinkerInjectionStep.MemberLevelTransformations.Sort()</c> time. When <c>null</c> or when no matching parameter
    /// is found, <see cref="Value"/> is used as-is. This enables late binding: the pull machinery emits
    /// a fallback expression plus this hint, and the linker picks the forwarding identifier once every
    /// aspect-introduced parameter in the final mutable compilation is visible — removing the need for
    /// same-aspect <see cref="IsOverride"/> patching.
    /// </summary>
    public string? ForwardParameterName { get; }

    private ExpressionSyntax Value { get; }

    public IntroduceConstructorInitializerArgumentTransformation(
        AspectLayerInstance aspectLayerInstance,
        IFullRef<IConstructor> constructor,
        int parameterIndex,
        string parameterName,
        ExpressionSyntax value,
        bool requiresParameterName,
        bool isOverride = false,
        string? forwardParameterName = null ) : base( aspectLayerInstance, constructor )
    {
        this._constructor = constructor;
        this.ParameterName = parameterName;
        this._requiresParameterName = requiresParameterName;
        this.ParameterIndex = parameterIndex;
        this.Value = value;
        this.IsOverride = isOverride;
        this.ForwardParameterName = forwardParameterName;
    }

    /// <summary>
    /// Returns a copy of this transformation with its <see cref="Value"/> replaced and
    /// <see cref="ForwardParameterName"/> cleared. Used by the linker after a hint is resolved.
    /// </summary>
    public IntroduceConstructorInitializerArgumentTransformation WithResolvedValue( ExpressionSyntax value )
        => new(
            this.AspectLayerInstance,
            this._constructor,
            this.ParameterIndex,
            this.ParameterName,
            value,
            this._requiresParameterName,
            this.IsOverride,
            forwardParameterName: null );

    public ArgumentSyntax ToSyntax()
    {
        var argumentSyntax = SyntaxFactory.Argument( this.Value );

        if ( this._requiresParameterName )
        {
            argumentSyntax = argumentSyntax.WithNameColon( SyntaxFactory.NameColon( this.ParameterName ) );
        }

        return argumentSyntax.WithAdditionalAnnotations( this.AspectInstance.AspectClass.GeneratedCodeAnnotation );
    }

    public override IFullRef<IDeclaration> TargetDeclaration => this._constructor;

    public override TransformationObservability Observability => TransformationObservability.None;

    public override IntrospectionTransformationKind TransformationKind => IntrospectionTransformationKind.InsertConstructorInitializerArgument;

    public override FormattableString ToDisplayString() => $"Introduce an argument to the initializer of constructor '{this._constructor}'.";
}