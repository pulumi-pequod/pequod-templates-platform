using Pulumi;
using Pulumi.AzureNative;
using Pulumi.AzureNative.Resources;

using PulumiPequod.Stackmgmt;
using PulumiPequod.Containerapps;

class MyStackCompBuildDeploy: Stack
{
    public MyStackCompBuildDeploy()
    {
        var config = new Pulumi.Config();
        var insightsSku = config.Get("insightsSku") ?? "PerGB2018";
        var appIngressPort = config.GetInt32("appIngressPort") ?? 80;
        var platform = config.Get("platform") ?? "linux/amd64";

        var resourceGroup = new ResourceGroup("resourceGroup");

        // Build the docker image, push to registry and deploy to container app in one fell swoop.
        var app = new AppBuildDeploy("app", new AppBuildDeployArgs 
        {
            ResourceGroupName = resourceGroup.Name,
            AppPath = "./app",
            Platform = platform,
        });

        var stacksettings = new StackSettings("stacksettings");

        this.Endpoint = app.ContainerAppFqdn.Apply(fqdn=> $"https://{fqdn}");    }

    [Output("endpoint")]
    public Output<string> Endpoint { get; set; }
}
