// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System;

namespace Metalama.Framework.Engine.Validation;

[Flags]
public enum ChildKinds
{
    // ReSharper disable once UnusedMember.Global
    None,
    DerivedType = 1,
    ContainingDeclaration = 2,
    All = DerivedType | ContainingDeclaration
}