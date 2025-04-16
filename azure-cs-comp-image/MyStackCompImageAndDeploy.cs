using Pulumi;
using Pulumi.AzureNative;
using Pulumi.AzureNative.Resources;

using PulumiPequod.Stackmgmt;
using PulumiPequod.Containerapps;

class MyStackCompImageAndDeploy : Stack
{
    public MyStackCompImageAndDeploy()
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

        var appDeploy = new AppDeploy("appdeploy", new AppDeployArgs
        {
            ResourceGroupName = resourceGroup.Name,
            RegistryLoginServer = appImage.RegistryLoginServer,
            RegistryUsername = appImage.RegistryUsername,
            RegistryPassword = appImage.RegistryPassword,
            ImageRef = appImage.ImageRef,
            InsightsSku = insightsSku,
            AppIngressPort = appIngressPort,
        });

        var stacksettings = new StackSettings("stacksettings");

        this.Endpoint = appDeploy.ContainerAppFqdn.Apply(fqdn=> $"https://{fqdn}");
    }

    [Output("endpoint")]
    public Output<string> Endpoint { get; set; }
}
