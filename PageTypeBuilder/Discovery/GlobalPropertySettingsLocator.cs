﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using PageTypeBuilder.Reflection;

namespace PageTypeBuilder.Discovery
{
    public class GlobalPropertySettingsLocator : IGlobalPropertySettingsLocator
    {
        private IAssemblyLocator assemblyLocator;

        public GlobalPropertySettingsLocator(IAssemblyLocator assemblyLocator)
        {
            this.assemblyLocator = assemblyLocator;
        }

        public virtual IEnumerable<GlobalPropertySettingsUpdater> GetGlobalPropertySettingsUpdaters()
        {
            var updaters = new List<GlobalPropertySettingsUpdater>();
            IEnumerable<Type> types = GetTypesInApplicationDomain();

            foreach (Type type in types)
            {
                foreach (var interfaceType in type.GetInterfaces())
                {
                    if (!interfaceType.IsGenericType)
                        continue;

                    if (!typeof(IUpdateGlobalPropertySettings<>).IsAssignableFrom(interfaceType.GetGenericTypeDefinition()))
                        continue;
                    var settingsType = interfaceType.GetGenericArguments().First();
                    var instance = Activator.CreateInstance(type);
                    var updater = new GlobalPropertySettingsUpdater(settingsType, instance);
                    updaters.Add(updater);
                }
            }

            return updaters;
        }

        private IEnumerable<Type> GetTypesInApplicationDomain()
        {
            string assemblyName = typeof(IUpdateGlobalPropertySettings<>).Assembly.GetName().Name;
            List<Type> updaterTypes = new List<Type>();
            var assemblies = assemblyLocator.GetAssemblies();
            foreach (Assembly assembly in assemblies)
            {
                if(assembly.GetReferencedAssemblies().Count(a => a.Name == assemblyName) == 0)
                    continue;

                IEnumerable<Type> typesInAssembly = assembly.GetTypes();
                updaterTypes.AddRange(typesInAssembly);
            }
            return updaterTypes;
        }
    }
}
