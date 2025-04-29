import pulumi

config = pulumi.Config()

insights_sku = config.get("insightsSku", "PerGB2018")
app_ingress_port = config.get_int("appIngressPort", 80)
platform = config.get("platform", "linux/amd64")