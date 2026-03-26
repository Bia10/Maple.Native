using System.Globalization;

namespace Maple.Native.DocTest;

public static class AssemblyInitializeCultureTest
{
    [Before(Assembly)]
    public static void SetInvariantCulture()
    {
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;
    }
}
