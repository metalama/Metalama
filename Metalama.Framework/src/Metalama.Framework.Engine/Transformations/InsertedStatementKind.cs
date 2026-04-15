// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Framework.Engine.Transformations;

internal enum InsertedStatementKind
{
    /// <summary>
    /// Insert statement into an introduced <c>Initialize</c> or <c>OnConstructed</c> method <em>before</em>
    /// the aggregated call to the base initializer (<c>base.Initialize(...)</c> / <c>base.OnConstructed(...)</c>).
    /// Emitted by <c>AddInitializer</c> advice with <c>InitializerPosition.BeforeBase</c>.
    /// </summary>
    InitializerBeforeBase = -400,

    /// <summary>
    /// The aggregated call to the base initializer (<c>base.Initialize(...)</c> / <c>base.OnConstructed(...)</c>),
    /// or any other anchor statement that must sit between <see cref="InitializerBeforeBase"/> and
    /// <see cref="InitializerAfterBase"/>.
    /// </summary>
    InitializerBase = -300,

    /// <summary>
    /// Describes a statement that is semantically positioned <em>after</em> the base initializer call.
    /// This is a <em>kind</em> label (semantic position relative to the base call); the physical emission
    /// site varies by target:
    /// <list type="bullet">
    /// <item>For <c>Initialize</c> / <c>OnConstructed</c> methods the base call is a real body statement
    /// (<c>base.Initialize(...)</c> / <c>base.OnConstructed(...)</c>), so these statements are emitted at
    /// method <em>exit</em>, after the user body.</item>
    /// <item>For source constructors (<c>BeforeInstanceConstructor</c> / <c>BeforeTypeConstructor</c> /
    /// <c>AfterObjectInitializer</c>) the base call lives in the constructor header <c>:base(...)</c> —
    /// there is no post-body seam — so these statements are emitted at the <em>start</em> of the
    /// constructor body (pre-user-body).</item>
    /// </list>
    /// Emitted by <c>AddInitializer</c> advice with <c>InitializerPosition.AfterBase</c> (the default)
    /// and by all constructor-targeting initialize advice (which always uses this kind).
    /// </summary>
    InitializerAfterBase = -250,

    /// <summary>
    /// Insert statement into the beginning of the current version of the target declaration (source, introduction or latest override). 
    /// Statements added by one layer have their order preserved.
    /// </summary>
    InputContract = -100,

    /// <summary>
    /// Insert statement at the end of the user body of a source instance constructor, after all user code has run.
    /// Used by <c>AfterLastInstanceConstructor</c> to emit a trailing <c>this.OnConstructed(context);</c> call.
    /// </summary>
    InitializerEpilogue = 50,

    /// <summary>
    /// Insert statement into the end of an auxiliary declaration for the current version of the target declaration (source, introduction or latest override).
    /// Statements added by one layer have their order preserved.
    /// </summary>
    OutputContract = 100
}