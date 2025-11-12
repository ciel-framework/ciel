using Microsoft.Build.Evaluation;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;

namespace Ciel;

public class ModuleDescriptor
{
    public required string Project { get; set; }
    public string? Version { get; set; }
    public string? Title { get; set; }
    public string? Authors { get; set; }
    public string? Description { get; set; }
    public string? PackageId { get; set; }
    public string? Copyright { get; set; }
    public string? PackageProjectUrl { get; set; }
    public string? PackageLicenseUrl { get; set; }
    public string? PackageIcon { get; set; }
    public string? RepositoryUrl { get; set; }
    public string? RepositoryType { get; set; }
    public string? PackageTags { get; set; }
    public string? PackageReleaseNotes { get; set; }
}

public class Modules {

    // <--- Method now returns a list of descriptors ---
    public static async Task<List<ModuleDescriptor>> Initialize(List<string> solutionPaths) {
        
        var globalProps = new Dictionary<string,string> 
        {
            //{ "Configuration", "Release" },
            //{ "Platform", "AnyCPU" },
            //{ "TargetFramework", "net9.0" }
        };
        
        using var workspace = MSBuildWorkspace.Create(globalProps);
        var registration = workspace.RegisterWorkspaceFailedHandler(OnWorkspaceFailed);

        var descriptors = new List<ModuleDescriptor>();

        using var projectCollection = new ProjectCollection();
        foreach (var solutionPath in solutionPaths) {
            var solution = await workspace.Project;

            foreach (var roslynProject in solution.Projects) {
                
                    
                
            }
        }

        // <--- Return the final list ---
        return descriptors;
    }

    private static void OnWorkspaceFailed(WorkspaceDiagnosticEventArgs obj) {
        Console.WriteLine($"WorkspaceFailed: {obj.Diagnostic.Message}");
    }
}