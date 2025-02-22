using MHRUnpack.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace MHRUnpack.Utils
{
    public class ServiceManager
    {
        private static readonly Lazy<IServiceProvider> _lazy = new Lazy<IServiceProvider>(() => ConfigureServices());
        public static IServiceProvider Services => _lazy.Value;

        private static IServiceProvider ConfigureServices()
        {
            var services = new ServiceCollection();
            //Type type = typeof(ServiceManager);
            //var array = GetExtensionMethods(type);
            //foreach (var method in array)
            //{
            //    method.Invoke(null, [null, services]);
            //}

            services.AddSingleton<MainViewModel>();
            services.AddSingleton(s => new MainWindow { DataContext = s.GetService<MainViewModel>() });
            return services.BuildServiceProvider();
        }
        public static MethodInfo[] GetExtensionMethods(Type targetType)
        {
            // 获取所有程序集中的扩展方法
            var extensionMethods = AppDomain.CurrentDomain
                                              .GetAssemblies()
                                              .SelectMany(assembly => assembly.GetTypes())
                                              .Where(type => type.IsDefined(typeof(ExtensionAttribute), false))
                                              .SelectMany(type => type.GetMethods(BindingFlags.Static | BindingFlags.Public))
                                              .Where(method => method.IsExtensionMethod() && method.GetParameters()[0].ParameterType == targetType)
                                              .ToArray();
            return extensionMethods;
        }
    }
    public static class Extension
    {
        public static bool IsExtensionMethod(this MethodInfo method)
        {
            return method.IsDefined(typeof(ExtensionAttribute), false);
        }
    }
}
