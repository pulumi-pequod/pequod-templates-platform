import pulumi
from typing import Optional, TypedDict
from appImage import AppImage
from appDeploy import AppDeploy

class AppBuildDeployArgs(TypedDict):

    resource_group_name: pulumi.Input[str] 
    """The resource group in which the image registry should be deployed."""
    app_path: pulumi.Input[str]
    """Path to the Dockerfile to build the app"""
    image_tag: Optional[pulumi.Input[str]] 
    """Optional: provided image tag to use. Default: latest"""
    platform: Optional[pulumi.Input[str]]
    """Optional: The platform for the image. Default: linux/amd64"""

class AppBuildDeploy(pulumi.ComponentResource):
    """
    Builds Docker image,  pushes it to Azure container registry, and deploys it to Azure container app
    """
    container_app_fqdn: pulumi.Output[str]
    """Fully qualified domain name of the container app."""

    def __init__(
            self,
            name: str,
            args: AppBuildDeployArgs,
            opts: Optional[pulumi.ResourceOptions] = None
    ):

        super().__init__('containerapps:index:AppBuildDeploy', name, {}, opts)

        resource_group_name = args.get("resource_group_name")
        app_path = args.get("app_path")
        image_tag = args.get("image_tag") or "latest"
        image_name = image_tag.split("/")[-1]
        platform = args.get("platform") or "linux/amd64"

        image = AppImage(name,
            resource_group_name=resource_group_name,
            app_path=app_path,
            image_tag=image_tag,
            platform=platform,
            opts=pulumi.ResourceOptions(parent=self)
        )

        deployment = AppDeploy(name, 
            resource_group_name=resource_group_name,
            registry_login_server=image.registry_login_server,
            registry_username=image.registry_username,
            registry_password=image.registry_password,
            image_ref=image.image_ref,
            opts=pulumi.ResourceOptions(parent=self)
        )

        self.container_app_fqdn = deployment.container_app_fqdn

        self.register_outputs({})
