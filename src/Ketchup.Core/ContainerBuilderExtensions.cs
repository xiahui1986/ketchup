﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Autofac;
using Ketchup.Core.Modules;
using Ketchup.Core.Utilities;
using Microsoft.Extensions.DependencyModel;
using Microsoft.Extensions.Logging;

namespace Ketchup.Core
{
    public static class ContainerBuilderExtensions
    {
        private static readonly List<Assembly> _referenceAssembly = new List<Assembly>();
        private static List<KernelModule> _modules = new List<KernelModule>();

        public static ContainerBuilder AddCoreService(this ContainerBuilder builder)
        {
            builder.Register(p => new KetchupPlatformContainer(p));
            builder.Register(p => new KetchupPlatformContainer(ServiceLocator.Current));
            return builder;
        }

        public static void RegisterModules(this ContainerBuilder builder)
        {
            var referenceAssemblies = GetAssemblies();
            foreach (var moduleAssembly in referenceAssemblies)
            {
                GetKernelModules(moduleAssembly).ForEach(module =>
                {
                    builder.RegisterModule(module);
                    _modules.Add(module);
                });
            }

            builder.Register(provider => new KernelModuleProvider(_modules,
                    provider.Resolve<KetchupPlatformContainer>(),
                    provider.Resolve<ILogger<KernelModuleProvider>>()))
                .As<IKernelModuleProvider>();
        }

        private static List<Assembly> GetAssemblies()
        {
            var referenceAssemblies = new List<Assembly>();

            var assemblyNames = DependencyContext
                .Default.GetDefaultAssemblyNames().Select(p => p.Name).ToArray();
            assemblyNames = GetFilterAssemblies(assemblyNames);
            foreach (var name in assemblyNames)
                referenceAssemblies.Add(Assembly.Load(name));
            _referenceAssembly.AddRange(referenceAssemblies.Except(_referenceAssembly));

            return referenceAssemblies;
        }

        private static string[] GetFilterAssemblies(string[] assemblyNames)
        {
            var pattern =
                "^Microsoft.\\w*|^System.\\w*|^DotNetty.\\w*|^runtime.\\w*|^ZooKeeperNetEx\\w*|^StackExchange.Redis\\w*|^Consul\\w*|^Newtonsoft.Json.\\w*|^Autofac.\\w*";
            var notRelatedRegex = new Regex(pattern,
                RegexOptions.Singleline | RegexOptions.Compiled | RegexOptions.IgnoreCase);
            return
                assemblyNames.Where(
                    name => !notRelatedRegex.IsMatch(name)).ToArray();
        }

        private static List<KernelModule> GetKernelModules(Assembly assembly)
        {
            var modules = new List<KernelModule>();
            Type[] arrayModule =
                assembly
                    .GetTypes()
                    .Where(t => t.IsSubclassOf(typeof(KernelModule)))
                    .ToArray();

            foreach (var moduleType in arrayModule)
            {
                var abstractModule = (KernelModule)Activator.CreateInstance(moduleType);
                modules.Add(abstractModule);
            }

            return modules;
        }
    }

    ///// <summary>
    /////     服务构建者。
    ///// </summary>
    //public interface IServiceBuilder
    //{
    //    /// <summary>
    //    ///     服务集合。
    //    /// </summary>
    //    ContainerBuilder Services { get; set; }
    //}
}