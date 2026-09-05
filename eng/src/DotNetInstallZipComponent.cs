// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using PostSharp.Engineering.BuildTools.Docker;
using System.IO;

namespace BuildMetalama;

/// <summary>
/// Makes <c>dotnet-install.ps1</c> download the zip form of the .NET SDK instead of the tar.gz form, by setting
/// the <c>DOTNET_INSTALL_SKIP_TAR</c> environment variable of the image.
/// </summary>
/// <remarks>
/// <para>
/// From .NET 11 onwards, <c>dotnet-install.ps1</c> prefers a tar.gz archive on Windows and extracts it by
/// invoking <c>tar</c>. The image installs Git for Windows and places its <c>usr\bin</c> directory ahead of
/// <c>System32</c> in the path, so <c>tar</c> resolves to GNU tar instead of the <c>bsdtar</c> of Windows. GNU
/// tar reads the leading <c>C:</c> of the archive path as the name of a remote host, and the installation fails
/// with "tar (child): Cannot connect to C: resolve failed".
/// </para>
/// <para>
/// <c>DOTNET_INSTALL_SKIP_TAR</c> is the switch that the script itself provides. It makes
/// <c>Test-TarAvailable</c> return false, and <c>Get-FileExtension-For-Version</c> then returns the zip
/// extension, which the script extracts with <c>System.IO.Compression</c> and no external process. Both archives
/// are published for every .NET 11 build, so no version becomes unavailable.
/// </para>
/// <para>
/// This component belongs in PostSharp.Engineering, beside <c>DotNetInstallerComponent</c>, because every image
/// that it generates installs Git for Windows. Remove it from this repository once that release is available.
/// </para>
/// </remarks>
internal sealed class DotNetInstallZipComponent : ContainerComponent
{
    public override string Name => "Install the .NET SDK from the zip archive";

    public override ContainerComponentKind Kind => ContainerComponentKind.DotNetInstaller;

    /// <summary>
    /// Gets the position of this component, which is between the download of <c>dotnet-install.ps1</c> and the
    /// first installation of an SDK by it.
    /// </summary>
    public override int SortOrder => ((int) ContainerComponentKind.DotNetInstaller * 100) + 50;

    public override void WriteDockerfile( TextWriter writer, ContainerOperatingSystem operatingSystem )
    {
        if ( operatingSystem == ContainerOperatingSystem.Linux )
        {
            // dotnet-install.sh extracts the archive itself and does not consult this variable.
            return;
        }

        writer.WriteLine( "ENV DOTNET_INSTALL_SKIP_TAR=1" );
    }
}
