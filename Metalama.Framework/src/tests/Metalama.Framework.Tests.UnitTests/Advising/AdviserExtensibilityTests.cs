// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Diagnostics;
using Metalama.Framework.Engine.Advising;
using System;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.Advising;

/// <summary>
/// Tests of <see cref="AdviserExtensibility"/>. The behavior with the advisers of the engine is tested by the aspect tests of
/// <c>Metalama.Framework.Tests.AspectTests.ExtensionPoints</c>, which use the public API only.
/// </summary>
public sealed class AdviserExtensibilityTests
{
    /// <summary>
    /// Verifies that <see cref="AdviserExtensibility.GetExtensionContext"/> throws an <see cref="ArgumentException"/> for an adviser that was not
    /// created by the engine.
    /// </summary>
    [Fact]
    public void ForeignAdviser_Throws()
        => Assert.Throws<ArgumentException>( () => new ForeignAdviser().GetExtensionContext() );

    /// <summary>
    /// An implementation of <see cref="IAdviser"/> that was not created by the engine.
    /// </summary>
    private sealed class ForeignAdviser : IAdviser
    {
        /// <inheritdoc />
        public ScopedDiagnosticSink Diagnostics => throw new NotSupportedException();

        /// <inheritdoc />
        public IDeclaration Target => throw new NotSupportedException();

        /// <inheritdoc />
        public ICompilation Compilation => throw new NotSupportedException();

        /// <inheritdoc />
        public ICompilation MutableCompilation => throw new NotSupportedException();

        /// <inheritdoc />
        public IAdviser<TNewDeclaration> With<TNewDeclaration>( TNewDeclaration declaration )
            where TNewDeclaration : class, IDeclaration
            => throw new NotSupportedException();
    }
}
