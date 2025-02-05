using System;
using System.Runtime.Serialization;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.SyntaxBuilders;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Subtemplates.ArgsScope_Error;

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