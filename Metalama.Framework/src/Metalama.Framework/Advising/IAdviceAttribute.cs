// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;

namespace Metalama.Framework.Advising;

/// <summary>
/// A marker interface for attributes that provide advice-related metadata. This interface serves as a common base
/// for attributes that configure or describe members used in aspect-oriented programming scenarios, such as templates
/// and declarative advice.
/// </summary>
/// <seealso cref="ITemplateAttribute"/>
/// <seealso cref="TemplateAttribute"/>
/// <seealso cref="DeclarativeAdviceAttribute"/>
[RunTimeOrCompileTime]
public interface IAdviceAttribute;