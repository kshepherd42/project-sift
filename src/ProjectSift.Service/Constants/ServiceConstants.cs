namespace ProjectSift.Service.Constants;

public static class ServiceConstants
{
    public static class Cors
    {
        public const string ViteDevServerPolicyName = "ViteDevServer";
        public const string ViteDevServerOrigin = "http://localhost:5173";
    }

    public static class Routes
    {
        public const string Health = "/api/health";
    }

    public static class ResponseFields
    {
        public const string Status = "status";
    }
}
