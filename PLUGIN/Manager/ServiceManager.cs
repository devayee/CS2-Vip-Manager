namespace Mesharsky_Vip;

public partial class MesharskyVip
{
    public class ServiceManager
    {
        private static readonly Dictionary<string, Service?> Services = [];

        public static void RegisterService(Service? service)
        {
            if (service != null && !string.IsNullOrEmpty(service.Name))
            {
                Services[service.Name] = service;
            }
        }

        public static Service? GetService(string name)
        {
            Services.TryGetValue(name, out var service);

            return service;
        }
    }
}

