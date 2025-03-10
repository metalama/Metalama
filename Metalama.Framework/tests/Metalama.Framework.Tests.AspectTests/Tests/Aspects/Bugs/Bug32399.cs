// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Linq;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug32399;

[Inheritable]
[EditorExperience( SuggestAsLiveTemplate = true )]
internal class DeepCloneAttribute : TypeAspect
{
    [Template( IsVirtual = true )]
    public T CloneImpl<[CompileTime] T>()
    {
        // The typeof in the expression below could not compile.
        var clonableFields =
            meta.Target.Type.FieldsAndProperties.Where(
                f => f.IsAutoPropertyOrField.GetValueOrDefault() &&
                     !f.IsImplicitlyDeclared &&
                     ( ( f.Type.IsConvertibleTo( typeof(ICloneable) ) && f.Type.SpecialType != SpecialType.String ) ||
                       ( f.Type is INamedType fieldNamedType && fieldNamedType.Enhancements().HasAspect( typeof(DeepCloneAttribute) ) ) ) );

        return default!;
    }
}

// <target>
[DeepClone]
internal partial class C { }