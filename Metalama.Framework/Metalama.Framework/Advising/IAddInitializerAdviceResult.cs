// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Framework.Advising;

/// <summary>
/// Represents the result of the <see cref="IAdviceFactory.AddInitializer(Metalama.Framework.Code.INamedType,string,InitializerKind,object?,object?)"/>
/// method.
/// </summary>
public interface IAddInitializerAdviceResult : IAdviceResult;