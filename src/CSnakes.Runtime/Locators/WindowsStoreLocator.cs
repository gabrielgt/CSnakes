﻿using System.Runtime.InteropServices;

namespace CSnakes.Runtime.Locators;
internal class WindowsStoreLocator(Version version) : PythonLocator(version)
{
    public override PythonLocationMetadata LocatePython()
    {
        var windowsStorePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "Python", $"Python{Version.Major}.{Version.Minor}");
        return LocatePythonInternal(windowsStorePath);
    }

    internal override bool IsSupported() => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
}
