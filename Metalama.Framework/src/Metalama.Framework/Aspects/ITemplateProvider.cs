// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Framework.Aspects;

/// <summary>
/// An interface that specifies that the type contains templates. Templates must be annotated with <see cref="TemplateAttribute"/>.
/// To call auxiliary templates from a class implementing this interface, use the <see cref="meta.InvokeTemplate(string, ITemplateProvider?, object?)"/> method.
/// </summary>
/// <seealso cref="TemplateAttribute"/>
/// <seealso cref="IAspect"/>
/// <seealso cref="meta.InvokeTemplate(string, ITemplateProvider?, object?)"/>
/// <seealso href="@templates"/>
/// <seealso href="@auxiliary-templates"/>
[RunTimeOrCompileTime]
public interface ITemplateProvider;