// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Metalama.Backstage.Licensing;

internal static class LicenseProductExtensions
{
    public static bool IsSupportedBeforeMetalama20251( this LicenseProduct product ) => product is not LicenseProduct.MetalamaCommunity;

    public static string GetDisplayName( this LicenseProduct product )
        => product switch
        {
            LicenseProduct.MetalamaCommunity => "Metalama Community",
            LicenseProduct.MetalamaEnterprise => "Metalama Enterprise",
            LicenseProduct.MetalamaProfessional => "Metalama Professional",
            LicenseProduct.PostSharpEssentials => "PostSharp Essentials",
            LicenseProduct.PostSharpFramework => "PostSharp Framework",
            LicenseProduct.PostSharpUltimate => "PostSharp Ultimate",
            LicenseProduct.PostSharpCachingLibrary => "PostSharp Caching",
            LicenseProduct.PostSharpDiagnosticsLibrary => "PostSharp Logging",
            LicenseProduct.PostSharpModelLibrary => "PostSharp MVVM",
            LicenseProduct.PostSharpThreadingLibrary => "PostSharp Threading",
            LicenseProduct.MetalamaFree => "Metalama Free (legacy)",
            LicenseProduct.MetalamaStarter => "Metalama Starter (legacy)",
            LicenseProduct.MetalamaUltimate => "Metalama Ultimate (legacy)",

            _ => product.ToString()
        };

    public static string GetDisplayName( this LicenseProduct product, ServicingPhase servicingPhase )
    {
        if ( servicingPhase is ServicingPhase.LongTerm && product.CanHaveLongTermSupportOption() )
        {
            return product.GetDisplayName() + " with long-term support";
        }
        else
        {
            return product.GetDisplayName();
        }
    }

    public static ServicingPhase GetDefaultServicingPhase( this LicenseProduct product )
        => product switch
        {
            // Note that Metalama Enterprise is Metalama Professional with a ServicingPhase field set to LongTerm.
            LicenseProduct.MetalamaProfessional => ServicingPhase.Extended,
            LicenseProduct.MetalamaEnterprise => ServicingPhase.LongTerm,
            LicenseProduct.PostSharpFramework => ServicingPhase.Extended,
            LicenseProduct.PostSharpUltimate => ServicingPhase.Extended,
            _ => ServicingPhase.Default
        };

    public static bool CanHaveLongTermSupportOption( this LicenseProduct product )
        => product switch
        {
            LicenseProduct.PostSharpUltimate => true,
            LicenseProduct.PostSharpFramework => true,
            _ => false
        };
}