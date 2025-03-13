// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Runtime.Serialization;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.SyntaxBuilders;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Subtemplates.ArgsScope;

class Aspect : TypeAspect
{
    [Introduce]
    void Format(UnsafeStringBuilder stringBuilder, IFormatterRepository formatterRepository)
    {
        var fieldOrProperties = meta.Target.Type.FieldsAndProperties;

        foreach (var fieldOrProperty in fieldOrProperties)
        {
            if (fieldOrProperty.IsImplicitlyDeclared)
            {
                continue;
            }

            meta.InvokeTemplate(nameof(this.FormatFieldOrProperty),
                                 args: new {
                                     T = fieldOrProperty.Type,
                                     fieldOrProperty,
                                     sb = ExpressionFactory.Capture(stringBuilder),
                                     fr = ExpressionFactory.Capture(formatterRepository)
                                 });
        }
    }

    [Template]
    private void FormatFieldOrProperty<[CompileTime] T>(
        IFieldOrProperty fieldOrProperty,
        UnsafeStringBuilder sb,
        IFormatterRepository fr)
    {
        fr.Get<T>().Format(sb, fieldOrProperty.Value);
    }
}

class UnsafeStringBuilder
{
    public void Append(string s) { }
}

interface IFormatterRepository
{
    IFormatter<T> Get<T>();
}

interface IFormatter<in T>
{
    void Format(UnsafeStringBuilder stringBuilder, T? value);
}

// <target>
[Aspect]
class TargetCode
{
    int P { get; set; }
}