// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;

namespace Metalama.Framework.Advising
{
    /// <summary>
    /// Specifies the position of an initializer relative to the call to the base initializer, in advice operations
    /// that support it, i.e. <see cref="InitializerKind.AfterObjectInitializer"/> and
    /// <see cref="InitializerKind.AfterLastInstanceConstructor"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Initialization advice of those kinds emits a method (<c>Initialize</c> or <c>OnConstructed</c>) that
    /// calls <c>base.Initialize(...)</c> or <c>base.OnConstructed(...)</c> when applicable. Templates can be
    /// placed before or after that base call, matching the matryoshka-doll convention used throughout Metalama
    /// for ordering aspects: outer aspects observe inner aspects' effects both before and after the call.
    /// </para>
    /// <para>
    /// Across aspect instances, <see cref="AfterBase"/> templates run in compile-time order (first-applied aspect
    /// first, innermost layer of the matryoshka), while <see cref="BeforeBase"/> templates run in run-time order
    /// (last-applied aspect first, outermost layer). Within a single aspect instance, multiple
    /// <c>AddInitializer</c> calls preserve their programmatic add-order in both positions.
    /// </para>
    /// </remarks>
    /// <seealso cref="InitializerKind"/>
    /// <seealso cref="IAddInitializerAdviceResult"/>
    /// <seealso href="@initializers"/>
    [CompileTime]
    public enum InitializerPosition
    {
        /// <summary>
        /// The initializer runs after the call to the base initializer (<c>base.Initialize(...)</c> or
        /// <c>base.OnConstructed(...)</c>). This is the default.
        /// </summary>
        /// <seealso href="@initializers"/>
        AfterBase = 0,

        /// <summary>
        /// The initializer runs before the call to the base initializer.
        /// </summary>
        /// <seealso href="@initializers"/>
        BeforeBase
    }
}