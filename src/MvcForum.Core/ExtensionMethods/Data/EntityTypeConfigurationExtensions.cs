﻿namespace MvcForum.Core.ExtensionMethods.Data
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Microsoft.EntityFrameworkCore;

    public static class EntityTypeConfigurationExtensions
    {
        private static readonly MethodInfo EntityMethod = typeof(ModelBuilder).GetTypeInfo().GetMethods().Single(x => x.Name == "Entity" && x.IsGenericMethod && x.GetParameters().Length == 0);
        private static readonly Dictionary<Assembly, IEnumerable<Type>> TypesPerAssembly = new Dictionary<Assembly, IEnumerable<Type>>();

        private static Type FindEntityType(Type type)
        {
            var interfaceType = type.GetInterfaces().First(x =>
                x.GetTypeInfo().IsGenericType && x.GetGenericTypeDefinition() == typeof(IEntityTypeConfiguration<>));
            return interfaceType.GetGenericArguments().First();
        }

        public static ModelBuilder ApplyConfiguration<T>(this ModelBuilder modelBuilder,
            IEntityTypeConfiguration<T> configuration) where T : class
        {
            var entityType = FindEntityType(configuration.GetType());

            dynamic entityTypeBuilder = EntityMethod
                .MakeGenericMethod(entityType)
                .Invoke(modelBuilder, new object[0]);

            configuration.Configure(entityTypeBuilder);

            return modelBuilder;
        }

        public static ModelBuilder UseEntityTypeConfiguration(this ModelBuilder modelBuilder)
        {
            var asm = Assembly.GetEntryAssembly();

            if (TypesPerAssembly.TryGetValue(asm, out var configurationTypes) == false)
                TypesPerAssembly[asm] = configurationTypes = asm.GetExportedTypes()
                                                                .Where(x => x.GetTypeInfo().IsClass && x.GetTypeInfo().IsAbstract == false && x.GetInterfaces()
                                                                .Any(y => y.GetTypeInfo().IsGenericType && y.GetGenericTypeDefinition() == typeof(IEntityTypeConfiguration<>)));

            var configurations = configurationTypes.Select(Activator.CreateInstance);

            foreach (dynamic configuration in configurations)
                ApplyConfiguration(modelBuilder, configuration);

            return modelBuilder;
        }
    }
}