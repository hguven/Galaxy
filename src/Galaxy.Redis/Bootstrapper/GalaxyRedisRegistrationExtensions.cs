﻿using Autofac;
using Galaxy.Cache;
using Galaxy.Redis.Bootstrapper.AutoFacModules;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Text;

namespace Galaxy.Redis.Bootstrapper
{
    public static class GalaxyRedisRegistrationExtensions
    {
        public static ContainerBuilder UseGalaxyRedisCache(this ContainerBuilder builder, Action<ConfigurationOptions> configurationOpts)
        {
            RegisterRedisCache(builder, configurationOpts);

            builder.RegisterAssemblyModules(typeof(RedisCacheModule).Assembly);
            
            return builder;
        }

        public static ContainerBuilder UseGalaxyRedisCache(this ContainerBuilder builder, Action<ConfigurationOptions> configurationOpts, Action<ICacheDefaultSettings> configureCacheSettings)
        {
            UseGalaxyRedisCache(builder, configurationOpts);

            var settings = new RedisGlobalDefaultCacheSettings();

            configureCacheSettings(settings);

            builder.RegisterInstance(settings)
                .As<ICacheDefaultSettings>()
                .SingleInstance();

            return builder;
        }

        private static ContainerBuilder RegisterRedisCache(this ContainerBuilder builder,Action<ConfigurationOptions> configurationOpts)
        {
            var conf = new ConfigurationOptions();
            
            configurationOpts(conf);
            
            ConnectionMultiplexer redis = ConnectionMultiplexer.Connect(conf);

            builder.Register(c =>
            {
                return redis.GetDatabase();
            })
           .As<IDatabase>()
           .InstancePerDependency();
            
            return builder;
        }
    }
}
