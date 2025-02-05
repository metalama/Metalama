// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Backstage.Licensing.Licenses;
using System;

namespace Metalama.Backstage.Licensing.Consumption
{
    /// <summary>
    /// Information about a license relevant to licensed features consumption.
    /// </summary>
    [PublicAPI]
    public class LicenseConsumptionData
    {
        /// <summary>
        /// Gets the namespace constraint of the license.
        /// Gets <c>null</c> if there is no namespace constraint.
        /// </summary>
        public string? LicensedNamespace { get; }

        /// <summary>
        /// Gets the product licensed by the license.
        /// </summary>
        public LicensedProduct LicensedProduct { get; }

        /// <summary>
        /// Gets the type of the license.
        /// </summary>
        public LicenseType LicenseType { get; }

        /// <summary>
        /// Gets the displayable name of the license shown in diagnostics and trace messages.
        /// </summary>
        public string DisplayName { get; }

        /// <summary>
        /// Gets the minimal PostSharp version the license can be used with.
        /// </summary>
        /// <remarks>
        /// Doesn't apply to products not based on PostSharp. (E.g. Metalama.)
        /// </remarks>
        public Version MinPostSharpVersion { get; }

        /// <summary>
        /// Gets a value indicating whether the license is redistributable.
        /// </summary>
        public bool IsRedistributable { get; }

        /// <summary>
        /// Gets the string representation of the license if the license has it.
        /// </summary>
        public string? LicenseString { get; }

        /// <summary>
        /// Gets a value indicating whether the license usage can be audited.
        /// </summary>
        public bool IsAuditable { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="LicenseConsumptionData"/> class.
        /// </summary>
        /// <param name="licensedProduct">The product licensed by the license.</param>
        /// <param name="licenseType">The type of the license.</param>
        /// <param name="licensedRequirement">Requirement available by the license.</param>
        /// <param name="licensedNamespace">Namespace constraint of the license. <c>null</c> if there is no namespace constraint.</param>
        /// <param name="displayName">The displayable name of the license shown in diagnostics and trace messages.</param>
        /// <param name="minPostSharpVersion">Minimal PostSharp version the license can be used with. Doesn't apply to products not based on PostSharp. (E.g. Metalama.)</param>
        /// <param name="licenseString">The string representation of the license, if exists. <c>null</c> if the license doesn't have a string representation.</param>
        /// <param name="isRedistributable">Indicates whether the license is redistributable.</param>
        /// <param name="maxAspectsCount">The number of aspects allowed to be used.</param>
        public LicenseConsumptionData(
            LicensedProduct licensedProduct,
            LicenseType licenseType,
            string? licensedNamespace,
            string displayName,
            Version minPostSharpVersion,
            string? licenseString,
            bool isRedistributable,
            bool isAuditable )
        {
            this.LicensedProduct = licensedProduct;
            this.LicenseType = licenseType;
            this.LicensedNamespace = licensedNamespace;
            this.DisplayName = displayName;
            this.MinPostSharpVersion = minPostSharpVersion;
            this.LicenseString = licenseString;
            this.IsRedistributable = isRedistributable;
            this.IsAuditable = isAuditable;
        }

        /// <summary>
        /// Gets the displayable name of the license.
        /// </summary>
        /// <returns>The displayable name of the license.</returns>
        public override string ToString()
        {
            return this.DisplayName;
        }
    }
}