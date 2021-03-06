﻿using Microsoft.Extensions.DependencyInjection;
using NetCoreCMS.Framework.Modules;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.ComponentModel.DataAnnotations.Schema;
using NetCoreCMS.Framework.Modules.Widgets;
using Microsoft.AspNetCore.Routing;
using NetCoreCMS.Framework.Core.Services;
using NetCoreCMS.Framework.Core.Data;
using NetCoreCMS.Framework.Core.Models;

namespace NetCoreCMS.Modules.Subscription
{
    public class SubscriptionModule : IModule
    {
        List<Widget> _widgets;
        public SubscriptionModule()
        {
             
        }

        public string ModuleId { get; set; }
        public string ModuleTitle { get; set; }
        public string Author { get; set; }
        public string Email { get; set; }
        public string Website { get; set; }
        public string DemoUrl { get; set; }
        public string ManualUrl { get; set; }
        public bool AntiForgery { get; set; }
        public string Version { get; set; }
        public string MinNccVersion { get; set; }
        public string MaxNccVersion { get; set; }
        public string Description { get; set; }
        public string Category { get; set; }
        public List<NccModuleDependency> Dependencies { get; set; }

        [NotMapped]
        public Assembly Assembly { get; set; }
        public string Path { get ; set ; }
        public string Folder { get; set; }
        public int ModuleStatus { get; set; }
        public string SortName { get ; set ; }
        [NotMapped]
        public List<Widget> Widgets { get { return _widgets; } set { _widgets = value; } }

        public bool Activate()
        {
            return true;
        }

        public bool Inactivate()
        {
            return true;
        }

        public void Init(IServiceCollection services)
        {            
            //services.AddTransient<SubscriptionUserRepository>();
            //services.AddTransient<SubscriptionUserService>();
        }

        public void RegisterRoute(IRouteBuilder routes)
        {
            
        }

        public bool Install(NccSettingsService settingsService, Func<NccDbQueryText, string> executeQuery)
        {
            var createQuery = @"
                CREATE TABLE IF NOT EXISTS `Ncc_Subscription_User` ( 
                    `Id` BIGINT NOT NULL AUTO_INCREMENT , 
                    `VersionNumber` INT NOT NULL , 
                    `Metadata` varchar(250) COLLATE utf8_unicode_ci NULL,
                    `Name` VARCHAR(250) NULL , 
                    `CreationDate` DATETIME NOT NULL , 
                    `ModificationDate` DATETIME NOT NULL , 
                    `CreateBy` BIGINT NOT NULL , 
                    `ModifyBy` BIGINT NOT NULL , 
                    `Status` INT NOT NULL , 

                    `Email` VARCHAR(255) NOT NULL, 
                    `Mobile` VARCHAR(255) NULL, 
                    `Remarks` VARCHAR(1000) NULL, 
                PRIMARY KEY (`Id`)) ENGINE = MyISAM;
            ";

            var nccDbQueryText = new NccDbQueryText() { MySql_QueryText = createQuery };
            var retVal = executeQuery(nccDbQueryText);
            if (!string.IsNullOrEmpty(retVal))
            {
                return true;
            }
            return false;
        }
        public bool Update(NccSettingsService settingsService, Func<NccDbQueryText, string> executeQuery)
        {
            return true;
        }
        public bool Uninstall(NccSettingsService settingsService, Func<NccDbQueryText, string> executeQuery)
        {
            var deleteQuery = @"
                DROP TABLE IF EXISTS `Ncc_Subscription_User`;
            ";

            var nccDbQueryText = new NccDbQueryText() { MySql_QueryText = deleteQuery };
            var retVal = executeQuery(nccDbQueryText);
            if (!string.IsNullOrEmpty(retVal))
            {
                return true;
            }
            return false;
        }
    }
}
