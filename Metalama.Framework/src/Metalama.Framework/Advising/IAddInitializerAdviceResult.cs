// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Framework.Advising;

/// <summary>
/// Represents the result of an advice that adds an initializer, returned by <see cref="AdviserExtensions.AddInitializer"/>.
/// </summary>
/// <seealso cref="IAdviceResult"/>
/// <seealso cref="AdviserExtensions.AddInitializer"/>
/// <seealso cref="InitializerKind"/>
/// <seealso href="@initializers"/>
/// <seealso href="@overriding-constructors"/>
public interface IAddInitializerAdviceResult : IAdviceResult;