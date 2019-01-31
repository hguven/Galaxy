﻿using Autofac;
using Galaxy.Dapper.Interfaces;

namespace Galaxy.Dapper.Bootstrapper.AutoFacModules
{
    public class DapperModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<DapperRepository>()
                .As<IDapperRepository>()
                .InstancePerDependency();
        }
    }
}