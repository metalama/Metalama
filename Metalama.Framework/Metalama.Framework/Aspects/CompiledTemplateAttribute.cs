// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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