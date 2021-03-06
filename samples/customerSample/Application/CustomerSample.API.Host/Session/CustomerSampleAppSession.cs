﻿using Galaxy.EFCore;
using Galaxy.Session;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CustomerSample.API.Host.Session
{
    public class CustomerSampleAppSession : IAppSessionContext
    {
        public  int? TenantId { get => null ; set => TenantId =  value; }
        public  int? UserId { get => 1; set => UserId = value; }
        
        public  int? GetCurrenTenantId()
        {
            return this.TenantId;
        }

        public  int? GetCurrentUserId()
        {
            return this.UserId;
        }
    }
}
