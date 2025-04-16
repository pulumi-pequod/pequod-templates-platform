using Pulumi;
using Pulumi.AzureNative;
using Pulumi.AzureNative.ContainerRegistry;
using Pulumi.AzureNative.ContainerRegistry.Inputs;
using Pulumi.AzureNative.OperationalInsights;
using Pulumi.AzureNative.OperationalInsights.Inputs;
using Pulumi.AzureNative.Resources;
using Pulumi.AzureNative.App;
using Pulumi.AzureNative.App.Inputs;
using DockerBuild = Pulumi.DockerBuild;
using ContainerArgs = Pulumi.AzureNative.App.Inputs.ContainerArgs;
using SecretArgs = Pulumi.AzureNative.App.Inputs.SecretArgs;

using PulumiPequod.Stackmgmt;

class MyStack : Stack
{
    public MyStack()
    {
        var config = new Pulumi.Config();
        var insightsSku = config.Get("insightsSku") ?? "PerGB2018";
        var appIngressPort = config.GetInt32("appIngressPort") ?? 80;
        var platform = config.Get("platform") ?? "linux/amd64";

        var resourceGroup = new ResourceGroup("resourceGroup");

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

        var registry = new Registry("registry", new()
        {
            ResourceGroupName = resourceGroup.Name,
            Sku = new SkuArgs
            {
                Name = "Basic"
            },
            AdminUserEnabled = true,
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
                        Server = registry.LoginServer,
                        Username = registryUsername,
                        PasswordSecretRef = "pwd",
                    },
                },
                Secrets = new[]
                {
                    new SecretArgs
                    {
                        Name = "pwd",
                        Value = registryPassword
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
                        Image = myImage.Ref,
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
