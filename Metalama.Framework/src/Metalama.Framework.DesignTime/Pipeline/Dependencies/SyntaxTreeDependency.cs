// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Framework.DesignTime.Pipeline.Dependencies;

// ReSharper disable NotAccessedPositionalProperty.Global
/// <summary>
/// Represents a single dependency edge between a master syntax tree and a dependent syntax tree. This object is used for test only.
/// </summary>
internal record struct SyntaxTreeDependency( string MasterFilePath, string DependentFilePath );