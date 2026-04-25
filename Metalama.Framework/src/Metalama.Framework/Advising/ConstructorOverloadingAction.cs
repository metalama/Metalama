// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Advising;

/// <summary>
/// Describes what an <see cref="IConstructorOverloadingStrategy"/> wants the framework to do with
/// a mutated constructor: do nothing, generate a forwarding constructor to keep source and binary compatibility with the original constructor,
/// or generate a forwarding constructor decorated with <see cref="System.ObsoleteAttribute"/>.
/// </summary>
/// <seealso cref="IConstructorOverloadingStrategy"/>
/// <seealso cref="ConstructorOverloadingStrategy"/>
[CompileTime]
[PublicAPI]
public readonly struct ConstructorOverloadingAction
{
    internal ConstructorOverloadingActionKind Kind { get; }

    internal string? ObsoleteMessage { get; }

    internal bool ObsoleteIsError { get; }

    private ConstructorOverloadingAction(
        ConstructorOverloadingActionKind kind,
        string? obsoleteMessage = null,
        bool obsoleteIsError = false )
    {
        this.Kind = kind;
        this.ObsoleteMessage = obsoleteMessage;
        this.ObsoleteIsError = obsoleteIsError;
    }

    /// <summary>
    /// Gets a <see cref="ConstructorOverloadingAction"/> that means no forwarding constructor should
    /// be generated for the mutated constructor.
    /// </summary>
    /// <remarks>
    /// <strong>This is a source-breaking change.</strong> Existing C# call sites that match the pre-mutation
    /// signature will fail to compile because no overload with the original parameter list remains on the type.
    /// Choose this action only when you intentionally want to force callers to update their code (for example,
    /// in an internal codebase where every caller can be migrated in lockstep, or as the final step of a
    /// multi-release deprecation rolled out via <see cref="ForwardAndMarkObsolete"/>).
    /// </remarks>
    public static ConstructorOverloadingAction None => new( ConstructorOverloadingActionKind.None );

    /// <summary>
    /// Gets a <see cref="ConstructorOverloadingAction"/> that means the framework should generate a
    /// forwarding constructor that chains to the mutated constructor via <c>: this(...)</c>.
    /// </summary>
    public static ConstructorOverloadingAction Forward => new( ConstructorOverloadingActionKind.Forward );

    /// <summary>
    /// Creates a <see cref="ConstructorOverloadingAction"/> that means the framework should generate a
    /// forwarding constructor and decorate it with <see cref="System.ObsoleteAttribute"/> so
    /// downstream callers see a deprecation warning (or error) when they use the pre-mutation signature.
    /// </summary>
    /// <param name="description">Optional deprecation message displayed at the call site. When <c>null</c>,
    /// the <c>[Obsolete]</c> attribute is emitted without a message.</param>
    /// <param name="isError">When <c>true</c>, using the forwarding constructor is a compiler
    /// error instead of a warning.</param>
    public static ConstructorOverloadingAction ForwardAndMarkObsolete( string? description = null, bool isError = false )
        => new( ConstructorOverloadingActionKind.ForwardAndMarkObsolete, description, isError );
}