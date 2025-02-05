using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.IntegrationTests.Aspects.Overrides.Constructors.PrimaryRecordStruct_CopyCtor;

/*
 * Tests single OverrideConstructor advice on a copy-ctor constructor of a record class (should get an eligibility error).
 */

public class OverrideAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        foreach (var constructor in builder.Target.Constructors)
        {
            if (!constructor.IsImplicitlyDeclared)
            {
                continue;
            }

            builder.With( constructor ).Override( nameof(Template) );
        }
    }

    [Template]
    public void Template()
    {
        meta.Proceed();
    }
}

// <target>
[Override]
public record class TargetStruct( int X, int Y ) { }