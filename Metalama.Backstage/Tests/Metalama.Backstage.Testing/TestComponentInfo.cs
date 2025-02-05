// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Application;
using System;

namespace Metalama.Backstage.Testing
{
    public class TestComponentInfo : IComponentInfo
    {
        public TestComponentInfo( string name, string? version, bool? isPrerelease, DateTime? buildDate, string? company )
        {
            this.Company = company;
            this.Name = name;
            this.PackageVersion = version;
            this.IsPrerelease = isPrerelease;
            this.BuildDate = buildDate;
        }

        public string? Company { get; }

        public string Name { get; }

        public string? PackageVersion { get; }

        public Version? AssemblyVersion => TestVersionHelper.GetAssemblyVersionFromPackageVersion( this.PackageVersion );

        public bool? IsPrerelease { get; }

        public DateTime? BuildDate { get; }
    }
}