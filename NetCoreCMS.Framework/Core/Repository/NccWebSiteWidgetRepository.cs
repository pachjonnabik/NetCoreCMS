﻿using NetCoreCMS.Framework.Core.Data;
using NetCoreCMS.Framework.Core.IoC;
using NetCoreCMS.Framework.Core.Models;
using NetCoreCMS.Framework.Core.Mvc.Repository;

namespace NetCoreCMS.Framework.Core.Repository
{
    public class NccWebSiteWidgetRepository : BaseRepository<NccWebSiteWidget, long>, ITransient
    {
        public NccWebSiteWidgetRepository(NccDbContext context) : base(context)
        {
        }
    }
}
