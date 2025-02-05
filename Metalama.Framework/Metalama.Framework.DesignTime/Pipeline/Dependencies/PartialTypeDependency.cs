// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Metalama.Framework.DesignTime.Pipeline.Dependencies;

// ReSharper disable NotAccessedPositionalProperty.Global
/// <summary>
/// Represents a dependency between a master partial type and a dependent syntax tree. Used in tests only.
/// </summary>
internal record struct PartialTypeDependency( TypeDependencyKey MasterType, string DependentFilePath );