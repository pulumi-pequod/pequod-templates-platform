import pulumi
from pulumi_azure_native import containerregistry
import pulumi_docker_build as docker_build
from typing import Optional, TypedDict

class AppImageArgs(TypedDict):

    resource_group_name: pulumi.Input[str] 
    """The resource group in which the image registry should be deployed."""
    app_path: pulumi.Input[str]
    """Path to the Dockerfile to build the app"""
    image_tag: Optional[pulumi.Input[str]] = "latest" 
    """Optional: provided image tag to use. Default: latest"""
    platform: Optional[pulumi.Input[str]] = "linux/amd64"
    """Optional: The platform for the image. Default: linux/amd64"""

class AppImage(pulumi.ComponentResource):
    """
    Builds Docker image and pushes it to Azure container registry
    """
    registry_login_server: pulumi.Output[str]
    """Container registry login server."""
    registry_username: pulumi.Output[str] 
    """Login for container registry."""
    registry_password: pulumi.Output[str]
    """Password for container registry login."""
    image_ref: pulumi.Output[str]
    """Reference for the image in the container registry."""

    def __init__(
            self,
            name: str,
            args: AppImageArgs,
            opts: Optional[pulumi.ResourceOptions] = None
    ):

        super().__init__('containerapps:index:AppImage', name, {}, opts)

        resource_group_name = args.get("resource_group_name")
        app_path = args.get("app_path")
        image_tag = args.get("image_tag") 
        image_name = image_tag.split("/")[-1]
        platform = args.get("platform") 

        registry = containerregistry.Registry(
            f"{name}registry",
            resource_group_name=resource_group_name,
            sku=containerregistry.SkuArgs(name="Basic"),
            admin_user_enabled=True,
            opts=pulumi.ResourceOptions(parent=self)
        )

        credentials = pulumi.Output.all(resource_group_name, registry.name).apply(
            lambda args: containerregistry.list_registry_credentials(
                resource_group_name=args[0], registry_name=args[1]
            )
        )
        self.registry_login_server = registry.login_server
        self.registry_username = credentials.username
        self.registry_password = credentials.passwords[0]["value"]


        image = docker_build.Image(f"{name}-myImage",
            context={
                "location": app_path,
            },
            push=True,
            exec_=True,
            tags=[registry.login_server.apply(lambda login_server: f"{login_server}/{image_name}:{image_tag}")],
            platforms=[docker_build.Platform(platform)],
            registries=[{
                "address": self.registry_login_server,
                "username": self.registry_username,
                "password": self.registry_password,
            }],
            opts=pulumi.ResourceOptions(parent=self)
        )
        self.image_ref = image.ref

        self.register_outputs({})
