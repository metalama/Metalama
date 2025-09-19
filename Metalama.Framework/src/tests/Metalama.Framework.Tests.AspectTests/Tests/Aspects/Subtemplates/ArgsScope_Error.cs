// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Subtemplates.ArgsScope_Error;

internal class Aspect : TypeAspect
{
    [Introduce]
    private void Format(UnsafeStringBuilder stringBuilder, IFormatterRepository formatterRepository)
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
                                     stringBuilder = stringBuilder,
                                     formatterRepository = formatterRepository
                                 });
        }
    }

    [Template]
    private void FormatFieldOrProperty<[CompileTime] T>(
        IFieldOrProperty fieldOrProperty,
        UnsafeStringBuilder stringBuilder,
        IFormatterRepository formatterRepository)
    {
        formatterRepository.Get<T>().Format(stringBuilder, fieldOrProperty.Value);
    }
}

internal class UnsafeStringBuilder
{
    public void Append(string s) { }
}

internal interface IFormatterRepository
{
    IFormatter<T> Get<T>();
}

internal interface IFormatter<in T>
{
    void Format(UnsafeStringBuilder stringBuilder, T? value);
}

// <target>
[Aspect]
internal class TargetCode
{
    private int P { get; set; }
}