using System.Threading.Tasks;
using Pulumi;

class Program
{
    // static Task<int> Main() => Deployment.RunAsync<MyStackCompImage>();
    // static Task<int> Main() => Deployment.RunAsync<MyStackCompBuildDeploy>();
    static Task<int> Main() => Deployment.RunAsync<MyStackCompImageAndDeploy>();
}
