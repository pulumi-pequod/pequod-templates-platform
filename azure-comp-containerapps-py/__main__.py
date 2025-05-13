import pulumi
from pulumi_azure_native import resources

from pulumi_pequod_stackmgmt import StackSettings, StackSettingsArgs
from pulumi_pequod_containerapps import AppBuildDeploy, AppBuildDeploy

from config import insights_sku, app_ingress_port, platform

# Create a Resource Group
resource_group = resources.ResourceGroup("resourceGroup")

# Create a Container App and deploy a custom Docker image
app = AppBuildDeploy(
    "app",
    resource_group_name=resource_group.name,
    app_path="./app",
    platform=platform,
    insights_sku=insights_sku,
    app_ingress_port=app_ingress_port,
)

# Configure stack settings
stack_settings = StackSettings("stacksettings")

# Export the endpoint as an output
pulumi.export("endpoint", pulumi.Output.concat("https://", app.container_app_fqdn))

