// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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
#pragma warning disable CS0618 // Type or member is obsolete
            LicenseProduct.MetalamaFree => "Metalama Free (legacy)",
            LicenseProduct.MetalamaStarter => "Metalama Starter (legacy)",
            LicenseProduct.MetalamaUltimate => "Metalama Ultimate (legacy)",
#pragma warning restore CS0618 // Type or member is obsolete

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