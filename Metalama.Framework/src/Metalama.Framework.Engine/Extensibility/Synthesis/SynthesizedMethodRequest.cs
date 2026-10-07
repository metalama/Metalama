// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Code;
using Metalama.Framework.Code.DeclarationBuilders;
using Microsoft.CodeAnalysis;
using System;

namespace Metalama.Framework.Engine.Extensibility.Synthesis;

/// <summary>
/// Describes a request to declare a method whose body is generated from a template, with
/// <see cref="CallSites.ExtensionTransformationFactory.DeclareMethod"/>.
/// </summary>
[PublicAPI]
public sealed class SynthesizedMethodRequest
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SynthesizedMethodRequest"/> class.
    /// </summary>
    /// <param name="placement">The type in which the method is declared.</param>
    /// <param name="nameHint">The name of the method. The factory adds a numeric suffix when the name is already used in the type or in one of its
    /// base types.</param>
    /// <param name="buildSignature">A delegate that sets the signature of the method: its accessibility, whether it is static, its return type, its
    /// parameters and its type parameters. The name of the method cannot be changed.</param>
    /// <param name="template">The template that generates the body of the method.</param>
    /// <param name="createProceedBinding">A delegate that returns the expression of <c>meta.Proceed()</c>, given the declared method.</param>
    /// <exception cref="ArgumentNullException">An argument is <c>null</c>.</exception>
    /// <exception cref="ArgumentException"><paramref name="nameHint"/> is not a valid identifier.</exception>
    public SynthesizedMethodRequest(
        SynthesizedMethodPlacement placement,
        string nameHint,
        Action<IMethodBuilder> buildSignature,
        SynthesizedMethodTemplate template,
        Func<IMethod, ProceedBinding> createProceedBinding )
    {
        this.Placement = placement ?? throw new ArgumentNullException( nameof(placement) );
        this.NameHint = SynthesisNames.ValidateIdentifier( nameHint, nameof(nameHint) );
        this.BuildSignature = buildSignature ?? throw new ArgumentNullException( nameof(buildSignature) );
        this.Template = template ?? throw new ArgumentNullException( nameof(template) );
        this.CreateProceedBinding = createProceedBinding ?? throw new ArgumentNullException( nameof(createProceedBinding) );
    }

    /// <summary>
    /// Gets the type in which the method is declared.
    /// </summary>
    public SynthesizedMethodPlacement Placement { get; }

    /// <summary>
    /// Gets the name of the method. The factory adds a numeric suffix when the name is already used.
    /// </summary>
    public string NameHint { get; }

    /// <summary>
    /// Gets the delegate that sets the signature of the method.
    /// </summary>
    public Action<IMethodBuilder> BuildSignature { get; }

    /// <summary>
    /// Gets the template that generates the body of the method.
    /// </summary>
    public SynthesizedMethodTemplate Template { get; }

    /// <summary>
    /// Gets the delegate that returns the expression of <c>meta.Proceed()</c>, given the declared method.
    /// </summary>
    public Func<IMethod, ProceedBinding> CreateProceedBinding { get; }

    /// <summary>
    /// Gets the location of the diagnostics that the template reports during its expansion, or <c>null</c> to report them on the declared method.
    /// </summary>
    /// <remarks>
    /// A method that several call sites share has no source code, so its diagnostics are best reported at one of these call sites.
    /// </remarks>
    public Location? DiagnosticLocation { get; init; }

    /// <summary>
    /// Gets a delegate that returns <c>false</c> for a name that the method must not have, or <c>null</c>. The factory calls it for each candidate
    /// name, in addition to its own checks, for instance to avoid hiding a member of a derived type.
    /// </summary>
    public Func<string, bool>? IsNameAvailable { get; init; }
}
