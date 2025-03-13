// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using System;

namespace Metalama.Framework.Aspects;

/// <summary>
/// This custom attribute is internal to the Metalama infrastructure and should not be used in user code.
/// It is added by Metalama when an aspect is compile to store the original characteristics of the template because some are
/// changed during compilation.
/// </summary>
[CompileTime]
[AttributeUsage( AttributeTargets.Event | AttributeTargets.Field | AttributeTargets.Method | AttributeTargets.Property )]
public sealed class CompiledTemplateAttribute : Attribute
{
    public Accessibility Accessibility { get; set; }

    public bool IsIteratorMethod { get; set; }

    public bool IsAsync { get; set; }
}