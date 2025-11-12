using Microsoft.Build.Locator;

namespace Ciel;

public static class Runtime {
    public static async Task Main() {
        if(!MSBuildLocator.IsRegistered)
            MSBuildLocator.RegisterDefaults();
        var res = await Modules.Initialize([
            "/home/mathilde/projects/CielTemplate/CielTemplate.sln",
        ]);
        Console.WriteLine(res);
    }
}