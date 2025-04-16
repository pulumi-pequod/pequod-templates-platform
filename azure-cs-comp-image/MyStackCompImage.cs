using Pulumi;
using Pulumi.AzureNative;
using Pulumi.AzureNative.OperationalInsights;
using Pulumi.AzureNative.OperationalInsights.Inputs;
using Pulumi.AzureNative.Resources;
using Pulumi.AzureNative.App;
using Pulumi.AzureNative.App.Inputs;
using ContainerArgs = Pulumi.AzureNative.App.Inputs.ContainerArgs;
using SecretArgs = Pulumi.AzureNative.App.Inputs.SecretArgs;

using PulumiPequod.Stackmgmt;
using PulumiPequod.Containerapps;

class MyStackCompImage : Stack
{
    public MyStackCompImage()
    {
        var config = new Pulumi.Config();
        var insightsSku = config.Get("insightsSku") ?? "PerGB2018";
        var appIngressPort = config.GetInt32("appIngressPort") ?? 80;
        var platform = config.Get("platform") ?? "linux/amd64";

        var resourceGroup = new ResourceGroup("resourceGroup");

        var appImage = new AppImage("appimage", new AppImageArgs 
        {
            ResourceGroupName = resourceGroup.Name,
            AppPath = "./app",
            Platform = platform,
        });

        var workspace = new Workspace("workspace", new()
        {
            ResourceGroupName = resourceGroup.Name,
            Sku = new WorkspaceSkuArgs
            {
                Name = insightsSku,
            },
            RetentionInDays = 30,
        });

        var sharedKey = GetSharedKeys.Invoke(new()
        {
            ResourceGroupName = resourceGroup.Name,
            WorkspaceName = workspace.Name,
        }).Apply(invoke => invoke.PrimarySharedKey);


        var managedEnv = new ManagedEnvironment("managedEnv", new()
        {
            ResourceGroupName = resourceGroup.Name,
            AppLogsConfiguration = new AppLogsConfigurationArgs
            {
                Destination = "log-analytics",
                LogAnalyticsConfiguration = new LogAnalyticsConfigurationArgs
                {
                    CustomerId = workspace.CustomerId,
                    SharedKey = sharedKey,
                },
            },
        });

        var containerapp = new ContainerApp("containerapp", new()
        {
            ResourceGroupName = resourceGroup.Name,
            ManagedEnvironmentId = managedEnv.Id,
            Configuration = new ConfigurationArgs
            {
                Ingress = new IngressArgs
                {
                    External = true,
                    TargetPort = appIngressPort,
                },
                Registries = new[]
                {
                    new RegistryCredentialsArgs
                    {
                        Server = appImage.RegistryLoginServer,
                        Username = appImage.RegistryUsername,
                        PasswordSecretRef = "pwd",
                    },
                },
                Secrets = new[]
                {
                    new SecretArgs
                    {
                        Name = "pwd",
                        Value = appImage.RegistryPassword
                    },
                },
            },
            Template = new TemplateArgs
            {
                Containers = new[]
                {
                    new ContainerArgs
                    {
                        Name = "myapp",
                        Image = appImage.ImageRef,
                    },
                },
            },
        });

        var stacksettings = new StackSettings("stacksettings");

        this.Endpoint = containerapp.Configuration.Apply(configuration => $"https://{configuration?.Ingress?.Fqdn}");
    }

    [Output("endpoint")]
    public Output<string> Endpoint { get; set; }
}
