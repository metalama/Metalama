// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Metalama.Framework.Tests.UnitTestHelpers.Helpers;

public static class OptionsTestHelper
{
    public const string OptionsCode =
        """
        using System;
        using Metalama.Framework.Advising; 
        using Metalama.Framework.Advising;
        using Metalama.Framework.Advising;
        using Metalama.Framework.Aspects; 
        using Metalama.Framework.Code;
        using Metalama.Framework.Options;
        using Metalama.Framework.Eligibility;
        using Metalama.Framework.Project;

        public record MyOptions : IHierarchicalOptions<IDeclaration>
        {
            public string? Value { get; set; }
        
            public IHierarchicalOptions GetDefaultOptions( OptionsInitializationContext context ) => this;
        
            public object ApplyChanges( object overridingObject, in ApplyChangesContext context )
            {
                var other = (MyOptions)overridingObject;
        
                return new MyOptions { Value = other.Value ?? Value };
            }
        
            public void BuildEligibility( IEligibilityBuilder<IDeclaration> declaration ) { }
        }
                               
        """;

    public const string ReportWarningFromOptionAspectCode =
        """
        using Metalama.Framework.Advising; 
        using Metalama.Framework.Advising;
        using Metalama.Framework.Aspects; 
        using Metalama.Framework.Code;
        using Metalama.Framework.Diagnostics;
        using Metalama.Framework.Eligibility;
        using System.Linq;
        using System;

        class ReportWarningFromOptionsAspect : MethodAspect
        {
           private static readonly DiagnosticDefinition<string> _description = new("MY001", Severity.Warning, "Option='{0}'" );
           
           public override void BuildAspect( IAspectBuilder<IMethod> aspectBuilder )
           {
                aspectBuilder.Diagnostics.Report( _description.WithArguments( aspectBuilder.Target.Enhancements().GetOptions<MyOptions>().Value ?? "<undefined>" ) );
           }
        }
        """;
}