// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Metalama.Framework.DesignTime.Pipeline.Dependencies;

// ReSharper disable NotAccessedPositionalProperty.Global
/// <summary>
/// Represents a single dependency edge between a master syntax tree and a dependent syntax tree. This object is used for test only.
/// </summary>
internal record struct SyntaxTreeDependency( string MasterFilePath, string DependentFilePath );