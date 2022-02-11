﻿using Microsoft.EntityFrameworkCore.Infrastructure;
using System.Collections.Generic;

namespace Detached.Mappers.EntityFramework.Extensions
{
    public class DetachedDbContextOptionsExtensionInfo : DbContextOptionsExtensionInfo
    {
        public DetachedDbContextOptionsExtensionInfo(IDbContextOptionsExtension extension)
            : base(extension)
        {
        }

        public override bool IsDatabaseProvider => false;

        public override string LogFragment => "Detached";

        public override long GetServiceProviderHashCode() => 0;

        public override void PopulateDebugInfo(IDictionary<string, string> debugInfo)
        {

        }
    }
}