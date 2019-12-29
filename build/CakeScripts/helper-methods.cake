using System.Text.RegularExpressions;

/*===============================================
================= HELPER METHODS ================
===============================================*/

public class Configuration
{
    private MSBuildToolVersion _msBuildToolVersion;    

    public string WebsiteRoot {get;set;}
    public string AuthoringRoot {get;set;}
    public string ShopsRoot {get;set;}
    public string OpsRoot {get;set;}
    public string MinionsRoot {get;set;}
    public string ProjectName {get;set;}
    public string SolutionName {get;set;}
    public string ProjectFolder {get;set;}
    public string CommerceEngineProjectPath {get;set;}
    public string ProjectBuildFolder {get;set;}
    public string BuildConfiguration {get;set;}
    public string Thumbprint {get;set;}
    public string SolrUrl {get;set;}
    public string MinionsEnvironmentName {get;set;}
    public string ShopsEnvironmentName {get;set;}
    public string AuthoringEnvironmentName {get;set;}
    public string MessageStatisticsApiKey {get;set;}
    public bool RunCleanBuilds {get;set;}
    public string DeployFolder {get;set;}      
    public string Version {get;set;}
    public string DeploymentTarget{get;set;}
    public bool RunSonarqAnalysis {get;set;}
    public string IdentityServerUrl {get;set;} 
    public string ContentManagementUrl {get;set;} 
    public string ContentDeliveryUrl {get;set;}
    public string BizfxUrl {get;set;}
    public string SitecoreUsername {get;set;}
    public string SitecorePassword {get;set;}
    public string Bootstrap {get;set;}
    public string AuthoringUrl {get;set;}

    public string CommerceEngineProjectFilePath{
        get
        {
            return $"{CommerceEngineProjectPath}\\Sitecore.Commerce.Engine.csproj";
        }     
    } 

    public string CommerceEngineProjectPublishPath
    {
        get
        {
            return $"{CommerceEngineProjectPath}\\bin\\publish";
        }     
    }

    public string BuildToolVersions 
    {
        set 
        {
            if(!Enum.TryParse(value, out this._msBuildToolVersion))
            {
                this._msBuildToolVersion = MSBuildToolVersion.Default;
            }
        }
    }

    public string SourceFolder => $"{ProjectFolder}\\src";
    public string FoundationSrcFolder => $"{SourceFolder}\\Foundation";
    public string FeatureSrcFolder => $"{SourceFolder}\\Feature";
    public string ProjectSrcFolder => $"{SourceFolder}\\Project";

    public string SolutionFile => $"{ProjectFolder}{SolutionName}";
    public MSBuildToolVersion MSBuildToolVersion => this._msBuildToolVersion;
    public string BuildTargets => this.RunCleanBuilds ? "Clean;Build" : "Build";
}

public void PrintHeader(ConsoleColor foregroundColor)
{
    cakeConsole.ForegroundColor = foregroundColor;
    cakeConsole.WriteLine("     "); 
    cakeConsole.WriteLine("     "); 
    cakeConsole.WriteLine(@" --------------------  ------------------");
    cakeConsole.WriteLine("   " + "Building the project");
    
    cakeConsole.WriteLine("     "); 
    cakeConsole.WriteLine("     ");
    cakeConsole.ResetColor();
}

public void PublishProjects(string rootFolder, string publishRoot)
{
    var projects = GetFiles($"{rootFolder}\\**\\code\\*.csproj"); //TODO:
    Information("Publishing " + rootFolder + " to " + publishRoot);
    var excludeConfigTransform = true;

    foreach (var project in projects)
    {
        MSBuild(project, cfg => InitializeMSBuildSettings(cfg)
                                   .WithTarget(configuration.BuildTargets)
                                   .WithProperty("Configuration", configuration.BuildConfiguration)
                                   .WithProperty("MarkWebConfigAssistFilesAsExclude", $"{excludeConfigTransform}")
                                   .WithProperty("DeployOnBuild", "true")
                                   .WithProperty("DeployDefaultTarget", "WebPublish")
                                   .WithProperty("WebPublishMethod", "FileSystem")
                                   .WithProperty("DeleteExistingFiles", "false")
                                   .WithProperty("publishUrl", publishRoot)
                                   .WithProperty("BuildProjectReferences", "false")
                                   );
    }
}

public FilePathCollection GetTransformFiles(string rootFolder)
{
    Func<IFileSystemInfo, bool> exclude_obj_bin_folder =fileSystemInfo => !fileSystemInfo.Path.FullPath.Contains("/obj/") || !fileSystemInfo.Path.FullPath.Contains("/bin/");

    var xdtFiles = GetFiles($"{rootFolder}\\**\\*.xdt", exclude_obj_bin_folder);

    return xdtFiles;
}

public void PublishCommerceRole(string source, string destination)
{
    var files = GetFiles($"{source}/**/*");
    var enumerator = files.GetEnumerator();
    var transformConfigs = new List<Cake.Core.IO.FilePath>(); 
    while (enumerator.MoveNext())
    {
        var filePath = enumerator.Current.FullPath;   
        if(filePath.EndsWith("Release.json"))
        {     
            transformConfigs.Add(enumerator.Current);
        }
    }
    foreach(var f in transformConfigs)
    {
        files.Remove(f);
    }
    CopyFiles(files, $"{destination}", true);
}

public void RebuildIndex(string indexName)
{
    var url = $"utilities/indexrebuild.aspx?index={indexName}";
    string responseBody = HttpGet(url);
}

public MSBuildSettings InitializeMSBuildSettings(MSBuildSettings settings)
{
    return new MSBuildSettings(){
        ArgumentCustomization = args=>args.Append("/consoleloggerparameters:ErrorsOnly")  //Suppresses the warnings to be reated as errors
    }
    .WithTarget(configuration.BuildTargets)
    .SetConfiguration(configuration.BuildConfiguration)
    .SetMSBuildPlatform(MSBuildPlatform.Automatic)
    .SetVerbosity(Verbosity.Minimal)
    .SetPlatformTarget(PlatformTarget.MSIL)
    .UseToolVersion(configuration.MSBuildToolVersion)
    .WithRestore();
}

public void CreateFolder(string folderPath)
{
    if (!DirectoryExists(folderPath))
    {
        CreateDirectory(folderPath);
    }
}

public void Spam(Action action, int? timeoutMinutes = null)
{
	Exception lastException = null;
	var startTime = DateTime.Now;
	while (timeoutMinutes == null || (DateTime.Now - startTime).TotalMinutes < timeoutMinutes)
	{
		try {
			action();

			Information($"Completed in {(DateTime.Now - startTime).Minutes} min {(DateTime.Now - startTime).Seconds} sec.");
			return;
		} catch (AggregateException aex) {
		    foreach (var x in aex.InnerExceptions)
				Information($"{x.GetType().FullName}: {x.Message}");
			lastException = aex;
		} catch (Exception ex) {
		    Information($"{ex.GetType().FullName}: {ex.Message}");
			lastException = ex;
		}
	}

    throw new TimeoutException($"Unable to complete within {timeoutMinutes} minutes.", lastException);
}

public void TransformCommerceRolesConfigs(string[] configs, string environmentName)
{
    foreach(var config in configs)
    {        
        var text = System.IO.File.ReadAllText(config);
        var transformedText = text
            .Replace("#{thumbprint}", configuration.Thumbprint)
            .Replace("#{solr-url}", configuration.SolrUrl)
            .Replace("#{environment-name}", environmentName)
            .Replace("#{dbmigration-enabled}", "false")
            .Replace("#{cm-url}", configuration.ContentManagementUrl)
            .Replace("#{cd-url}", configuration.ContentDeliveryUrl)
            .Replace("#{identity-server-url}", configuration.IdentityServerUrl)
            .Replace("#{bizfx-url}", configuration.BizfxUrl);
            
        System.IO.File.WriteAllText(config, transformedText);
    }
}

public void WriteError(string errorMessage)
{
    cakeConsole.ForegroundColor = ConsoleColor.Red;
    cakeConsole.WriteError(errorMessage);
    cakeConsole.ResetColor();
}

public static class DeploymentTargets
{
    public const string Local = "Local";
    public const string Hosted = "Azure";
}