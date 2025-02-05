// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.Diagnostics;

namespace Metalama.Framework.Engine.Advising;

internal static class TemplateNameValidator
{
    public static TemplateMemberRef? ValidateTemplateName(
        TemplateClass templateClass,
        string templateName,
        TemplateKind templateKind,
        bool required = false,
        bool ignoreMissing = false )
    {
        Invariant.Assert( !(required && ignoreMissing) );

        if ( templateClass.Members.TryGetValue( templateName, out var template ) )
        {
            if ( template.TemplateInfo.IsNone )
            {
                // It is possible that the aspect has a member of the required name, but the user did not use the custom attribute. In this case,
                // we want a proper error message.

                throw GeneralDiagnosticDescriptors.MemberDoesNotHaveTemplateAttribute.CreateException( (template.TemplateClass.FullName, templateName) );
            }

            if ( template.TemplateInfo.IsAbstract )
            {
                if ( !required )
                {
                    return null;
                }
                else
                {
                    throw new AssertionFailedException( "A non-abstract template was expected." );
                }
            }

            return new TemplateMemberRef( template, templateKind );
        }
        else
        {
            if ( ignoreMissing )
            {
                return null;
            }
            else
            {
                throw GeneralDiagnosticDescriptors.AspectMustHaveExactlyOneTemplateMember.CreateException( (templateClass.ShortName, templateName) );
            }
        }
    }
}