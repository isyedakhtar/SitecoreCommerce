#addin "Cake.Http"
#addin "Cake.Json"
#addin "Cake.Powershell"
#addin "Newtonsoft.Json"
#addin nuget:?package=Cake.XdtTransform&version=0.17.0
#addin "Cake.FileHelpers"
//#tool nuget:http://CodeConcerns-nuget.stwgroup.net.au/nuget?package=CodeConcerns.SonarQube.Scanner


#load "local:?path=./build/CakeScripts/helper-methods.cake" 
#load "local:?path=./build/CakeScripts/xml-helpers.cake"

var target = Argument<string>("Target", "Default");
var configuration = new Configuration();
var cakeConsole = new CakeConsole();
var configJsonFile = Argument<string>("ConfigFile", "./build/cake-config.json");
var bootstrapScript = $"./scripts/BootstrapInitialize-CommerceEngine.ps1";
 
var deploymentTarget = "";
var releaseVersion = "";
bool deployLocal = false;
string topology = null;
var sonarQubeScannerPath = MakeAbsolute(File("./build/tools/custom/CodeConcerns.SonarQube.Scanner.1.0.0/msbuild-scanner/sonar-scanner-msbuild-4.1.1.1164-net46/SonarScanner.MSBuild.exe")).FullPath;

// SonarQube parameters
var sonarQubeProjectName = Argument("sonarQubeProjectName", EnvironmentVariable("SONARQUBE_PROJECT_NAME"));
var sonarQubeProjectKey = Argument("sonarQubeProjectKey", EnvironmentVariable("SONARQUBE_PROJECT_KEY"));
var sonarQubeTestProjectPattern = Argument("sonarQubeTestProjectPattern", EnvironmentVariable("SONARQUBE_TEST_PROJECT_PATTERN"));
var sonarQubeDynamicProperties = Argument("sonarQubeDynamicProperties", "sonar.host.url=http://sonarqube.my.net.au/|sonar.login=3db7c2e51c492bd51b96428472c5c89ae4eeb3e2");

/*===============================================
================ MAIN TASKS =====================
===============================================*/

Setup(context =>
{
    Information($"Vars{sonarQubeProjectName}, {sonarQubeProjectKey}, {sonarQubeTestProjectPattern} ");
	cakeConsole.ForegroundColor = ConsoleColor.Yellow;
	PrintHeader(ConsoleColor.DarkGreen);
	
    var configFile = new FilePath(configJsonFile);
    configuration = DeserializeJsonFromFile<Configuration>(configFile);
    
    releaseVersion = Argument<string>("ReleaseVersion", configuration.Version);

    configuration.DeployFolder = MakeAbsolute(File(configuration.DeployFolder)).FullPath;
    configuration.ProjectBuildFolder = MakeAbsolute(File(configuration.ProjectBuildFolder)).FullPath;
    deploymentTarget = Argument<string>("deploymentTarget",configuration.DeploymentTarget);
    
    if (target.Contains(DeploymentTargets.Hosted)){
        // DeploymentTarget is set to either Local or OnPrem but the user has selected Deploy to Azure.
        // Automatically switch the deploymentTarget based on the build target
        deploymentTarget = DeploymentTargets.Hosted;
    }

    deployLocal = configuration.DeploymentTarget.Equals(DeploymentTargets.Local, StringComparison.InvariantCultureIgnoreCase);

    if(string.IsNullOrWhiteSpace(sonarQubeProjectName))
    {
       sonarQubeProjectName = configuration.ProjectName;
    }
    if(string.IsNullOrWhiteSpace(sonarQubeProjectKey))
    {
      sonarQubeProjectKey = configuration.ProjectName;        
    }
});
   

/*===============================================
============ Local Build - Main Tasks ===========
===============================================*/
Task("Default")
.WithCriteria(()=> configuration != null)
.IsDependentOn("CleanBuildFolders")
.IsDependentOn("Publish-All-Projects-With-Commerce-Roles")
.IsDependentOn("Transform-All-Configs")
.IsDependentOn("PostDeploy"); 

Task("Quick-Deploy")
.WithCriteria(()=> configuration != null)
.IsDependentOn("CleanBuildFolders")
.IsDependentOn("Publish-All-Projects-With-Commerce-Roles")
.IsDependentOn("Transform-All-Configs"); 

Task("PostDeploy")
.WithCriteria(()=> configuration != null)
.IsDependentOn("BootstrpInitialize-Commerce-Engine");

/*===============================================
=========== Packaging - Main Tasks ==============
===============================================*/
Task("Build-Nuget")
.WithCriteria(()=> configuration != null)
.IsDependentOn("CleanAll")
.IsDependentOn("Publish-Commerce-Engine-Project")
.IsDependentOn("Create-NuGet-Packages");

/*===============================================
================= SUB TASKS =====================
===============================================*/

Task("CleanAll")
.IsDependentOn("CleanBuildFolders")
.IsDependentOn("CleanDeployFolder");

Task("CleanBuildFolders").Does(() => {
    // TODO: Needs more attention
    CleanDirectories($"{configuration.SourceFolder}/**/obj");
    CleanDirectories($"{configuration.SourceFolder}/**/bin");
});


Task("CleanDeployFolder").Does(() => {
    // Clean deployment folders
     string[] folders = { $"\\NugetPackages",$"\\assets\\{configuration.ProjectName}", $"\\{configuration.ProjectName}"};

    foreach (string folder in folders)
    {
        Information($"Cleaning: {folder}");
        if (DirectoryExists($"{configuration.DeployFolder}{folder}"))
        {
            try
            {
                CleanDirectories($"{configuration.DeployFolder}{folder}");
            } catch
            {
                Console.BackgroundColor = ConsoleColor.Red;
                Console.WriteLine($"The folder under path \'{configuration.DeployFolder}{folder}\' is still in use by a process. Exiting...");
                Console.ResetColor();
                Environment.Exit(0);
            }
        }
    }
});

Task("SonarBegin")
    .WithCriteria(()=> configuration.RunSonarqAnalysis)
    .Does(()=>{
        // examples values of parameters to be passed through dynamic properties argument
        //
        // Required
        // sonar.host.url=http://localhost:9000
        // sonar.login=xxx
        //
        // Optional
        // sonar.cs.dotcover.reportsPaths="%CD%\dotCover.html
        //
        // PR Analysis parameters
        // sonar.bitbucket.repoSlug= {ProjectName}
        // sonar.bitbucket.accountName= {AccountName}
        // sonar.bitbucket.oauthClientKey=xxx
        // sonar.bitbucket.oauthClientSecret=xxx
        // sonar.bitbucket.branchName=%teamcity.build.branch%|
        // sonar.analysis.mode=issues

        var consoleArgs = "begin /k:\""+sonarQubeProjectKey+"\" /n:\""+sonarQubeProjectName+"\" /v:\""+ releaseVersion +"\"";
        if(!string.IsNullOrWhiteSpace(sonarQubeTestProjectPattern))
        {
            consoleArgs += " /d:sonar.msbuild.testProjectPattern:\""+sonarQubeTestProjectPattern+"\"";
            consoleArgs += " ";
        }
        var dynamicProperties = new List<string>();
        if(!string.IsNullOrWhiteSpace(sonarQubeDynamicProperties))
        {
            dynamicProperties = sonarQubeDynamicProperties.Split(new[]{'|'},StringSplitOptions.RemoveEmptyEntries).ToList();
            if(dynamicProperties.Any())
            {
                consoleArgs += " ";
                consoleArgs += string.Join(" ",dynamicProperties.Select(x=> "/d:"+x));
            }
        }

        var exitCode = StartProcess(sonarQubeScannerPath, new ProcessSettings {
            Arguments = new ProcessArgumentBuilder()
                .Append(consoleArgs)
        });

        if(exitCode != 0)
            Information("Encountered some problem when running: SonarBegin");
    });

Task("SonarEnd")
    .WithCriteria(()=> configuration.RunSonarqAnalysis)
    .Does(()=>{
        var consoleArgs = "end";
        if(!string.IsNullOrWhiteSpace(sonarQubeDynamicProperties))
        {
            var dynamicProperties = sonarQubeDynamicProperties.Split(new[]{'|'},StringSplitOptions.RemoveEmptyEntries).ToList();
            if(dynamicProperties.Any())
            {
                consoleArgs += " ";
                consoleArgs += string.Join(" ",dynamicProperties.Where(p=>p.Contains("login")).Select(x=> "/d:"+x));
            }
        }

        var exitCode = StartProcess(sonarQubeScannerPath, new ProcessSettings {
            Arguments = new ProcessArgumentBuilder()
                .Append(consoleArgs)
        });

        if(exitCode != 0)
            Information("Encountered some problem when running: SonarEnd");
    });
/*===============================================
=============== Generic Tasks ===================
===============================================*/
Task("Publish-All-Projects-With-Commerce-Roles")
.IsDependentOn("Publish-Commerce-Engine-Project")
.IsDependentOn("Publish-Commerce-Engine-Roles");

Task("Publish-Commerce-Engine-Roles")
.IsDependentOn("Publish-Commerce-Engine-Authoring")
.IsDependentOn("Publish-Commerce-Engine-Shops")
.IsDependentOn("Publish-Commerce-Engine-Ops")
.IsDependentOn("Publish-Commerce-Engine-Minions");

Task("Publish-Commerce-Engine-Authoring")
    .WithCriteria(()=> deployLocal)
    .Does(() =>
    {   
        PublishCommerceRole(configuration.CommerceEngineProjectPublishPath, $"{configuration.WebsiteRoot}\\{configuration.AuthoringRoot}");
    });

Task("Transform-All-Configs")
.IsDependentOn("Transform-Authoring-Configs")
.IsDependentOn("Transform-Shops-Configs")
.IsDependentOn("Transform-Ops-Configs")
.IsDependentOn("Transform-Minions-Configs");

Task("Transform-Authoring-Configs")
    .WithCriteria(()=> deployLocal)
    .Does(() =>
    {  
        var configs =new string[] {
            $"{configuration.WebsiteRoot}\\{configuration.AuthoringRoot}\\wwwroot\\config.json",
            $"{configuration.WebsiteRoot}\\{configuration.AuthoringRoot}\\wwwroot\\data\\Environments\\PlugIn.Search.Solr.PolicySet-1.0.0.json"            
        };

        TransformCommerceRolesConfigs(configs, configuration.AuthoringEnvironmentName);
    });

Task("Transform-Ops-Configs")
    .WithCriteria(()=> deployLocal)
    .Does(() =>
    {  
        var configs =new string[] {
            $"{configuration.WebsiteRoot}\\{configuration.OpsRoot}\\wwwroot\\config.json",
            $"{configuration.WebsiteRoot}\\{configuration.OpsRoot}\\wwwroot\\data\\Environments\\PlugIn.Search.Solr.PolicySet-1.0.0.json"            
        };

        TransformCommerceRolesConfigs(configs, configuration.AuthoringEnvironmentName);
    });

Task("Transform-Minions-Configs")
    .WithCriteria(()=> deployLocal)
    .Does(() =>
    {  
        var configs =new string[] {
            $"{configuration.WebsiteRoot}\\{configuration.MinionsRoot}\\wwwroot\\config.json",
            $"{configuration.WebsiteRoot}\\{configuration.MinionsRoot}\\wwwroot\\data\\Environments\\PlugIn.Search.Solr.PolicySet-1.0.0.json"            
        };

        TransformCommerceRolesConfigs(configs, configuration.MinionsEnvironmentName);
    });    

Task("Transform-Shops-Configs")
    .WithCriteria(()=> deployLocal)
    .Does(() =>
    {  
        var configs =new string[] {
            $"{configuration.WebsiteRoot}\\{configuration.ShopsRoot}\\wwwroot\\config.json",
            $"{configuration.WebsiteRoot}\\{configuration.ShopsRoot}\\wwwroot\\data\\Environments\\PlugIn.Search.Solr.PolicySet-1.0.0.json"            
        };
        
        TransformCommerceRolesConfigs(configs, configuration.ShopsEnvironmentName);
});
Task("Publish-Commerce-Engine-Shops")
    .WithCriteria(()=> deployLocal)
    .Does(() =>
    {   
        PublishCommerceRole(configuration.CommerceEngineProjectPublishPath, $"{configuration.WebsiteRoot}//{configuration.ShopsRoot}");
    });

Task("Publish-Commerce-Engine-Ops")
    .WithCriteria(()=> deployLocal)
    .Does(() =>
    {   
        PublishCommerceRole(configuration.CommerceEngineProjectPublishPath, $"{configuration.WebsiteRoot}//{configuration.OpsRoot}");
    });

Task("Publish-Commerce-Engine-Minions")
    .WithCriteria(()=> deployLocal)
    .Does(() =>
    {   
        PublishCommerceRole(configuration.CommerceEngineProjectPublishPath, $"{configuration.WebsiteRoot}//{configuration.MinionsRoot}");
    });
	
Task("BootstrpInitialize-Commerce-Engine")
    .WithCriteria(()=> deployLocal)
    .Does(() =>
    {   
        StartPowershellFile(bootstrapScript, new PowershellSettings()
                            .SetFormatOutput()
                            .SetLogOutput()
                            .WithArguments(args => {
                                args.Append("engineHostName", configuration.AuthoringUrl)
                                    .Append("identityServerHost", configuration.IdentityServerUrl)
                                    .Append("adminUser", configuration.SitecoreUsername)
                                    .Append("adminPassword", configuration.SitecorePassword);
                                                        }));
    });	

Task("Publish-Commerce-Engine-Project")
    .IsDependentOn("Build")
    .Does(() =>
    {
        var publishPath = configuration.CommerceEngineProjectPublishPath;
        if(!deployLocal)
        {
            publishPath = $"{configuration.DeployFolder}\\{configuration.ProjectName}\\{configuration.ProjectName}";
        }

        var settings = new DotNetCorePublishSettings
        {
            Configuration = configuration.BuildConfiguration,
            NoBuild = false,
            OutputDirectory = publishPath
        };

        DotNetCorePublish(configuration.CommerceEngineProjectFilePath, settings);
    });


Task("Build")
.IsDependentOn("SonarBegin")
.IsDependentOn("Build-Solution")
.IsDependentOn("SonarEnd");

Task("Build-Solution")
.IsDependentOn("Restore-NuGet-Packages")
.Does(() => {
    MSBuild(MakeAbsolute(File(configuration.SolutionFile)).FullPath, cfg => InitializeMSBuildSettings(cfg));
});

Task("Restore-NuGet-Packages")
    .Does(() =>
    {
        var settings = new NuGetRestoreSettings
        {
            Verbosity = NuGetVerbosity.Detailed
        };
        
        NuGetRestore(configuration.SolutionName, settings);   
    });

/*===============================================
=============== Utility Tasks ===================
===============================================*/

Task("Create-NuGet-Packages")
    .Does(()=>{
        var websiteRelativeRoot = $".\\{configuration.ProjectName}\\{configuration.ProjectName}";
        var nugetRootPath = $"{configuration.DeployFolder}\\NugetPackages";
        EnsureDirectoryExists(nugetRootPath);
        
        var nuGetPackSettings   = new NuGetPackSettings {
                                     
                                     Version                 = releaseVersion,
                                     Authors                 = new[] {"CodeConcerns"},
                                     Owners                  = new[] {"CodeConcerns"},
                                     Copyright               = "CodeConcerns 2020",
                                     RequireLicenseAcceptance= false,
                                     NoPackageAnalysis       = true,
                                     BasePath                = configuration.DeployFolder,
                                     OutputDirectory         = nugetRootPath
                                 };
    
    //Pack the Website
    Information("Nuspec file : Commerce");
    nuGetPackSettings.Id = $"{configuration.ProjectName}";
    nuGetPackSettings.Description = $"{configuration.ProjectName}";
    nuGetPackSettings.Files = new List<NuSpecContent>(){
                new NuSpecContent(){
                    Source = $"{websiteRelativeRoot}\\**",
                    Exclude = $"{websiteRelativeRoot}\\Sitecore.*.dll;{websiteRelativeRoot}\\System.*.dll;{websiteRelativeRoot}\\Microsoft.*.dll;{websiteRelativeRoot}\\App_Config\\Include\\**\\*.exclude;"
                }
            };
    NuGetPack(nuGetPackSettings);

    if(DirectoryExists($"{configuration.ProjectBuildFolder}\\scripts"))
    {
        //Pack the DeploymentTools
        Information("Nuspec file : DeploymentTools");
        nuGetPackSettings.BasePath = $"{configuration.ProjectBuildFolder}\\scripts";                             
        nuGetPackSettings.Id = $"{configuration.ProjectName}.DeploymentTools";
        nuGetPackSettings.Description = $"{configuration.ProjectName} DeploymentTools";
        nuGetPackSettings.Files = new List<NuSpecContent>(){
                    new NuSpecContent(){
                        Source = ".\\**",
                        Target = ".\\DeploymentTools"
                    }
                };
        NuGetPack(nuGetPackSettings);
    }
});

RunTarget(target);
