// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Metalama.Framework.Aspects;

/// <summary>
/// An interface that specifies that the type contains templates. Templates must be annotated with <see cref="TemplateAttribute"/>.
/// </summary>
[RunTimeOrCompileTime]
public interface ITemplateProvider;