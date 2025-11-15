using Microsoft.Build.Locator;

namespace Ciel;

public static class Runtime {
    public static async Task Main() {
        if(!MSBuildLocator.IsRegistered)
            MSBuildLocator.RegisterDefaults();
        var res = ModuleRegistry.Initialize([
            "/home/mathilde/projects/ciel-framework/CielTemplate/CielTemplate.sln",
        ]);
        foreach (var v in res)
        {
            Console.WriteLine($"{v.Key}: {v.Value}");
        }
    }
}