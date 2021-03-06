﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NetCoreCMS.Framework.Core;
using NetCoreCMS.Framework.Core.Models;
using NetCoreCMS.Framework.Core.Models.ViewModels;
using NetCoreCMS.Framework.Core.Mvc.Controllers;
using NetCoreCMS.Framework.Core.Mvc.Models;
using NetCoreCMS.Framework.Core.Network;
using NetCoreCMS.Framework.Core.Services;
using NetCoreCMS.Framework.i18n;
using NetCoreCMS.Framework.Modules;
using NetCoreCMS.Framework.Setup;
using NetCoreCMS.Framework.Themes;
using NetCoreCMS.Framework.Utility;
using NetCoreCMS.Modules.Admin.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace NetCoreCMS.Core.Modules.Admin.Controllers
{
    [Authorize(Roles = "SuperAdmin,Administrator")]
    [AdminMenu(Name = "Settings", IconCls = "fa-cogs", Order = 99)]
    public class AdminController : NccController
    {
        NccWebSiteService _webSiteService;
        NccPageService _pageService;
        NccPostService _postService;
        NccCategoryService _categoryService;
        NccSettingsService _settingsService;
        RoleManager<NccRole> _roleManager;
        UserManager<NccUser> _userManager;
        NccStartupService _startupService;
        IHostingEnvironment _hostingEnvironment;
        IConfiguration _configuration;
        NccModuleService _moduleService;

        public AdminController(NccWebSiteService nccWebSiteService, NccPageService pageService, NccPostService postService, NccCategoryService categoryService, NccSettingsService settingsService, RoleManager<NccRole> roleManager, UserManager<NccUser> userManager, NccStartupService startupService, IConfiguration configuration, IHostingEnvironment hostingEnv,
        NccModuleService moduleService, ILoggerFactory loggarFactory)
        {
            _webSiteService = nccWebSiteService;
            _pageService = pageService;
            _postService = postService;
            _categoryService = categoryService;
            _settingsService = settingsService;
            _roleManager = roleManager;
            _userManager = userManager;
            _startupService = startupService;
            _configuration = configuration;
            _hostingEnvironment = hostingEnv;
            _moduleService = moduleService;
            _logger = loggarFactory.CreateLogger<AdminController>();
        }

        [Authorize]
        //[AdminMenuItem(Name = "Dashboard", Url = "/Admin", IconCls = "fa-dashboard", Order = 1)]
        public ActionResult Index()
        {
            var webSite = new NccWebSite();
            var webSites = _webSiteService.LoadAll();

            if (webSites != null && webSites.Count > 0)
            {
                webSite = webSites.FirstOrDefault();
            }
            ViewBag.TotalPublishedPage = _pageService.LoadAllByPageStatus(NccPage.NccPageStatus.Published).Count();
            ViewBag.TotalPage = _pageService.LoadAll(true).Count();
            ViewBag.TotalPublishedPost = _postService.TotalPublishedPostCount();
            ViewBag.TotalPost = _postService.LoadAll(true).Count();
            ViewBag.TotalUser = _userManager.Users.Count();
            ViewBag.TotalModule = _moduleService.LoadAll().Count();
            ViewBag.TotalTheme = GlobalConfig.Themes.Count();
            return View(webSite);
        }

        #region Settings

        [AdminMenuItem(Name = "General", Url = "/Admin/Settings", IconCls = "fa-gear", Order = 2)]
        public ActionResult Settings()
        {
            //var webSites = _webSiteService.LoadAll();
            //var cultures = SupportedCultures.Cultures;
            //ViewBag.Languages = new SelectList(cultures.Select(x => new { Value = x.TwoLetterISOLanguageName.ToLower(), Text = x.NativeName.ToString() }).ToList(), "Value", "Text", SetupHelper.Language);
            //ViewBag.CurrentLanguage = CurrentLanguage;            

            NccWebSite webSite = _webSiteService.LoadAll().FirstOrDefault();
            if (webSite.WebSiteInfos.Count <= 0)
            {
                NccWebSiteInfo _item = new NccWebSiteInfo();
                _item.Language = GlobalConfig.WebSite.Language;
                webSite.WebSiteInfos.Add(_item);
            }

            if (GlobalConfig.WebSite.IsMultiLangual)
            {
                foreach (var item in SupportedCultures.Cultures)
                {
                    var count = webSite.WebSiteInfos.Where(x => x.Language == item.TwoLetterISOLanguageName).Count();
                    if (count <= 0)
                    {
                        NccWebSiteInfo _item = new NccWebSiteInfo();
                        _item.Language = item.TwoLetterISOLanguageName;
                        webSite.WebSiteInfos.Add(_item);
                    }
                }
            }

            return View(webSite);
        }

        [HttpPost]
        public ActionResult Settings(NccWebSite website)
        {
            ViewBag.MessageType = "ErrorMessage";
            ViewBag.Message = "Error occoured. Please fill up all field correctly.";

            bool isMultiLanguageChange = GlobalConfig.WebSite.IsMultiLangual == website.IsMultiLangual ? false : true;

            if (ModelState.IsValid)
            {
                NccWebSite prevWebSite = _webSiteService.Get(website.Id, true);
                bool isSuccess = true;

                #region For default language
                var defaultLangDetails = website.WebSiteInfos.Where(x => x.Language == GlobalConfig.WebSite.Language).FirstOrDefault();
                if (defaultLangDetails == null)
                {
                    isSuccess = false;
                    ViewBag.Message = "Default language data can't be null";
                }
                else
                {
                    //title empty validation
                    if (string.IsNullOrEmpty(defaultLangDetails.SiteTitle))
                    {
                        isSuccess = false;
                        ViewBag.Message = "Default language Title can't be null";
                    }
                }
                #endregion

                #region Check validation for other languages 
                List<NccWebSiteInfo> deletedList = new List<NccWebSiteInfo>();
                foreach (var item in website.WebSiteInfos.Where(x => x.Language != GlobalConfig.WebSite.Language).ToList())
                {
                    if (item.Id == 0 && string.IsNullOrEmpty(item.SiteTitle) && string.IsNullOrEmpty(item.Name) && string.IsNullOrEmpty(item.Tagline) && string.IsNullOrEmpty(item.Copyrights) && string.IsNullOrEmpty(item.PrivacyPolicyUrl))
                    {
                        deletedList.Add(item);
                    }
                    else
                    {
                        if (string.IsNullOrEmpty(item.SiteTitle))
                        {
                            isSuccess = false;
                            ViewBag.Message = "Title can't be null for language " + item.Language;
                        }
                    }
                }

                //Remove empty
                if (isSuccess)
                {
                    foreach (var item in deletedList)
                    {
                        website.WebSiteInfos.Remove(item);
                    }
                }
                #endregion

                #region Operation
                if (isSuccess)
                {
                    _webSiteService.Update(website);
                    GlobalConfig.WebSite = website;
                    ThemeHelper.WebSite = website;
                    SetupHelper.LoadSetup();
                    SetupHelper.Language = website.Language;
                    SetupHelper.SaveSetup();

                    ViewBag.MessageType = "SuccessMessage";
                    ViewBag.Message = "Page updated successful";
                    //var successMessage = "Settings updated successfully";
                    if (isMultiLanguageChange)
                    {
                        ViewBag.Message += ". You must <a href=\"/Home/RestartHost\">restart</a> the site.";
                        //TempData["SuccessMessage"] = "You must <a href=\"/Home/RestartHost\">restart</a> the site.";
                        //successMessage += ". You must <a href=\"/Home/RestartHost\">restart</a> the site.";
                        //GlobalConfig.WebSite.IsMultiLangual = false;
                        //ThemeHelper.WebSite.IsMultiLangual = false;
                    }
                }
                #endregion
            }
            else
            {
                //ModelState.AddModelError("Name", "Please check all values and submit again.");
                ViewBag.Message = string.Join("; ", ModelState.Values.SelectMany(x => x.Errors).Select(x => x.ErrorMessage));
            }

            //var cultures = SupportedCultures.Cultures;
            //ViewBag.Languages = new SelectList(cultures.Select(x => new { Value = x.TwoLetterISOLanguageName.ToLower(), Text = x.NativeName.ToString() }).ToList(), "Value", "Text", SetupHelper.Language);

            if (GlobalConfig.WebSite.IsMultiLangual)
            {
                foreach (var item in SupportedCultures.Cultures)
                {
                    var count = website.WebSiteInfos.Where(x => x.Language == item.TwoLetterISOLanguageName).Count();
                    if (count <= 0)
                    {
                        NccWebSiteInfo _item = new NccWebSiteInfo();
                        _item.Language = item.TwoLetterISOLanguageName;
                        website.WebSiteInfos.Add(_item);
                    }
                }
            }
            return View(website);
        }

        [AdminMenuItem(Name = "Startup", Url = "/Admin/Startup", IconCls = "fa-random", Order = 3)]
        public ActionResult Startup()
        {
            var model = PrepareStartupViewData();
            return View(model);
        }

        [AdminMenuItem(Name = "Email", Url = "/Admin/EmailSettings", IconCls = "fa-envelope", Order = 4)]
        public ActionResult EmailSettings()
        {
            var model = _settingsService.GetByKey<SmtpSettings>(Constants.SMTPSettingsKey);
            if (model == null)
                model = new SmtpSettings();
            return View(model);
        }

        [HttpPost]
        public ActionResult EmailSettings(SmtpSettings model, bool UseSSL)
        {
            if (ModelState.IsValid)
            {
                model.UseSSL = UseSSL;
                var settings = _settingsService.SetByKey<SmtpSettings>(Constants.SMTPSettingsKey, model);
                TempData["SuccessMessage"] = "Settings save successful.";
            }
            return View(model);
        }

        [AdminMenuItem(Name = "Logging", Url = "/Admin/Logging", IconCls = "fa-file-text-o", Order = 5)]
        public ActionResult Logging()
        {
            PrepareLogViewData();
            return View();
        }

        private void PrepareLogViewData()
        {
            var config = SetupHelper.LoadSetup();
            var levels = new Dictionary<string, int>();

            levels.Add("Trace", (int)LogLevel.Trace);
            levels.Add("Debug", (int)LogLevel.Debug);
            levels.Add("Information", (int)LogLevel.Information);
            levels.Add("Warning", (int)LogLevel.Warning);
            levels.Add("Error", (int)LogLevel.Error);
            levels.Add("Critical", (int)LogLevel.Critical);
            levels.Add("None", (int)LogLevel.None);

            ViewBag.LogLevels = levels;
            ViewBag.LogLevel = config.LoggingLevel;

            ViewBag.LogFiles = ListLogFiles();

        }

        [HttpPost]
        public ActionResult Logging(int logLevelValue, string logFileName, string operation)
        {
            if (operation == "SetLog")
            {
                SetupHelper.LoadSetup();
                SetupHelper.LoggingLevel = logLevelValue;
                SetupHelper.SaveSetup();
                TempData["SuccessMessage"] = "Log Levels save successful. <a href='/Home/RestartHost'> Restart Site</a> for change effect.";
            }
            else
            {
                if (!string.IsNullOrEmpty(logFileName))
                {
                    try
                    {
                        var logFilePath = GlobalConfig.ContentRootPath + "\\" + NccInfo.LogFolder + "\\" + logFileName;
                        var originalFileStream = System.IO.File.Open(logFilePath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
                        MemoryStream zipStream = new MemoryStream();
                        using (ZipArchive zip = new ZipArchive(zipStream, ZipArchiveMode.Create, true))
                        {
                            var zipEntry = zip.CreateEntry(logFileName);
                            using (var writer = new StreamWriter(zipEntry.Open()))
                            {
                                originalFileStream.Seek(0, SeekOrigin.Begin);
                                originalFileStream.CopyTo(writer.BaseStream);
                            }
                        }
                        zipStream.Seek(0, SeekOrigin.Begin);
                        return File(zipStream, "application/zip", logFileName + ".zip");

                    }
                    catch (Exception ex)
                    {
                        TempData["ErrorMessage"] = ex.Message;
                    }
                    finally
                    {

                    }
                }
            }
            PrepareLogViewData();
            return View();
        }

        private Dictionary<string, string> ListLogFiles()
        {
            var dict = new Dictionary<string, string>();
            var logFolderPath = GlobalConfig.ContentRootPath + "\\" + NccInfo.LogFolder;
            var files = Directory.GetFiles(logFolderPath);
            foreach (var item in files)
            {
                var file = new FileInfo(item);
                dict.Add(file.Name, file.Name);
            }
            return dict;
        }

        public FileResult DownloadAllLogs()
        {
            var dict = new Dictionary<string, string>();
            var logFolderPath = GlobalConfig.ContentRootPath + "\\" + NccInfo.LogFolder;

            MemoryStream zipStream = new MemoryStream();
            using (ZipArchive zip = new ZipArchive(zipStream, ZipArchiveMode.Create, true))
            {
                var files = Directory.GetFiles(logFolderPath);
                foreach (var item in files)
                {
                    try
                    {
                        var fi = new FileInfo(item);
                        var originalFileStream = System.IO.File.Open(item, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
                        var zipEntry = zip.CreateEntry(fi.Name);
                        using (var writer = new StreamWriter(zipEntry.Open()))
                        {
                            originalFileStream.Seek(0, SeekOrigin.Begin);
                            originalFileStream.CopyTo(writer.BaseStream);
                        }
                    }
                    catch (Exception ex)
                    {
                        
                    }
                }
            }

            zipStream.Seek(0, SeekOrigin.Begin);
            return File(zipStream, "application/zip", "AllLogFiles.zip");

        }

        [HttpPost]
        public ActionResult Startup(StartupViewModel vmodel)
        {
            try
            {
                var setupConfig = SetupHelper.LoadSetup();
                setupConfig.StartupType = vmodel.StartupType;

                if (vmodel.StartupType == StartupTypeText.Url)
                {
                    setupConfig.StartupData = vmodel.Url;
                    setupConfig.StartupUrl = vmodel.Url;
                }
                else if (vmodel.StartupType == StartupTypeText.Page)
                {
                    setupConfig.StartupData =  vmodel.PageId;
                    var page = _pageService.Get(long.Parse(vmodel.PageId));
                    if (page == null)
                    {
                        TempData["ErrorMessage"] = "Page not found.";
                    }
                    var pageDetails = page.PageDetails.Where(x => x.Language == GlobalConfig.WebSite.Language).FirstOrDefault();
                    if (pageDetails == null)
                    {
                        TempData["ErrorMessage"] = "Page for default language not found.";
                    }
                    else
                    {
                        setupConfig.StartupUrl = "/" + pageDetails.Slug;
                    }
                }
                else if (vmodel.StartupType == StartupTypeText.Post)
                {
                    setupConfig.StartupData =  vmodel.PostId;
                    var post = _postService.Get(long.Parse(vmodel.PostId));
                    if (post == null)
                    {
                        TempData["ErrorMessage"] = "Post not found.";
                    }
                    var postDetails = post.PostDetails.Where(x => x.Language == GlobalConfig.WebSite.Language).FirstOrDefault();
                    if (postDetails == null)
                    {
                        TempData["ErrorMessage"] = "Post for default language not found.";
                    }
                    else
                    {
                        setupConfig.StartupUrl = "/Post/" + postDetails.Slug;
                    }
                }
                else if (vmodel.StartupType == StartupTypeText.Category)
                {
                    setupConfig.StartupData = vmodel.CategoryId;
                    var category = _categoryService.Get(long.Parse(vmodel.CategoryId));
                    if (category == null)
                    {
                        TempData["ErrorMessage"] = "Category not found.";
                    }
                    var categoryDetails = category.CategoryDetails.Where(x => x.Language == GlobalConfig.WebSite.Language).FirstOrDefault();
                    if (categoryDetails == null)
                    {
                        TempData["ErrorMessage"] = "Category for default language not found.";
                    }
                    else
                    {
                        setupConfig.StartupUrl = "/Category/" + categoryDetails.Slug;
                    }
                }
                else if (vmodel.StartupType == StartupTypeText.Module)
                {
                    setupConfig.StartupData = vmodel.ModuleSiteMenuUrl;
                    setupConfig.StartupUrl = vmodel.ModuleSiteMenuUrl;
                }
                else
                {
                    setupConfig.StartupType = StartupTypeText.Url;
                    setupConfig.StartupData = "/CmsHome";
                    setupConfig.StartupUrl = "/CmsHome";
                }

                if (setupConfig.StartupData.Trim('/') == "" || setupConfig.StartupData.Trim().Trim('/').ToLower() == "home")
                {
                    TempData["ErrorMessage"] = "Incorrect value";
                    return View(vmodel);
                }

                SetupHelper.UpdateSetup(setupConfig);
                GlobalConfig.SetupConfig = setupConfig;

            }
            catch (Exception ex)
            {
                return View(vmodel);
            }

            TempData["SuccessMessage"] = "Config save successful.";
            var model = PrepareStartupViewData();
            return View(model);
        }
        
        public StartupViewModel PrepareStartupViewData()
        {
            var setupConfig = SetupHelper.LoadSetup();
            var model = new StartupViewModel();
            var moduleSiteMenuList = new List<SiteMenuItem>();
            var roleList = _roleManager.Roles.Select(x => new { Name = x.Name, Value = x.Id }).ToList();

            model.Url = setupConfig.StartupUrl;
            model.StartupType = setupConfig.StartupType;

            //original was Slug , Title
            model.Pages = new SelectList(_pageService.LoadAll(true), "Id", "Name", setupConfig.StartupData);
            model.Posts = new SelectList(_postService.LoadPublished(0,100), "Id", "Name", setupConfig.StartupData);
            model.Categories = new SelectList(_categoryService.LoadAll(true), "Id", "Name", setupConfig.StartupData);
            NccMenuHelper.GetModulesSiteMenus().Select(x => x.Value).ToList().ForEach(x => moduleSiteMenuList.AddRange(x));
            model.ModuleSiteMenus = new SelectList(moduleSiteMenuList, "Url", "Url", setupConfig.StartupData);
            model.Roles = new SelectList(roleList, "Value", "Name");

            ViewBag.RoleStartups = _startupService.LoadAll(true,0,"",false);

            ViewBag.DefaultChecked = "";
            ViewBag.PageChecked = "";
            ViewBag.CategoryChecked = "";
            ViewBag.PostChecked = "";
            ViewBag.ModuleChecked = "";

            if (setupConfig.StartupType == StartupTypeText.Page)
            {
                ViewBag.PageChecked = "checked";
            }
            else if (setupConfig.StartupType == StartupTypeText.Post)
            {
                ViewBag.PostChecked = "checked";
            }
            else if (setupConfig.StartupType == StartupTypeText.Category)
            {
                ViewBag.CategoryChecked = "checked";
            }
            else if (setupConfig.StartupType == StartupTypeText.Module)
            {
                ViewBag.ModuleChecked = "checked";
            }
            else
            {
                ViewBag.DefaultChecked = "checked";
            }

            return model;
        }

        public ActionResult RoleStartup()
        {
            var model = PrepareStartupViewData();
            return View(model);
        }

        [HttpPost]
        public ActionResult RoleStartup(StartupViewModel vmodel, long[] Roles)
        {
            var finalUrl = GetFinalStartupUrl(vmodel);
            _startupService.SaveOrUpdate(finalUrl, vmodel.RoleStartupType, Roles);
            TempData["SuccessMessage"] = "Update Successful";
            return RedirectToAction("Startup");
        }

        public ActionResult DeleteRoleStartup(long id)
        {
            _startupService.DeletePermanently(id);
            TempData["SuccessMessage"] = "Delete Successful";
            return RedirectToAction("Startup");
        }


        public ActionResult ManageUsers()
        {
            return View();
        }

        [AdminMenuItem(Name = "Maintenance Mode", Url = "/Admin/MaintenanceMode", IconCls = "fa-gavel", Order = 10)]
        public IActionResult MaintenanceMode()
        {
            ViewBag.SetupConfig = SetupHelper.LoadSetup();
            return View();
        }

        [HttpPost]
        public IActionResult MaintenanceMode(string isMaintenanceMode, int maintenanceDuration, string maintenanceMessage)
        {
            var setupConfig = SetupHelper.LoadSetup();
            setupConfig.IsMaintenanceMode = !string.IsNullOrEmpty(isMaintenanceMode);
            setupConfig.MaintenanceDownTime = maintenanceDuration;
            setupConfig.MaintenanceMessage = maintenanceMessage;

            SetupHelper.UpdateSetup(setupConfig);
            ViewBag.Message = "Save successful";

            ViewBag.SetupConfig = setupConfig;
            return View();
        }

        #endregion

        #region Privet Methods

        public string GetFinalStartupUrl(StartupViewModel vmodel)
        {
            var finalUrl = "";

            if (vmodel.RoleStartupType == StartupTypeText.Url)
            {
                finalUrl = vmodel.Url;
            }
            else if (vmodel.RoleStartupType == StartupTypeText.Page)
            {
                var page = _pageService.Get(long.Parse(vmodel.PageId));
                if (page == null)
                {
                    TempData["ErrorMessage"] = "Page not found.";
                }
                var pageDetails = page.PageDetails.Where(x => x.Language == GlobalConfig.WebSite.Language).FirstOrDefault();
                if (pageDetails == null)
                {
                    TempData["ErrorMessage"] = "Page for default language not found.";
                }
                else
                {
                    finalUrl = "/" + pageDetails.Slug;
                }
            }
            else if (vmodel.RoleStartupType == StartupTypeText.Post)
            {
                var post = _postService.Get(long.Parse(vmodel.PostId));
                if (post == null)
                {
                    TempData["ErrorMessage"] = "Post not found.";
                }
                var postDetails = post.PostDetails.Where(x => x.Language == GlobalConfig.WebSite.Language).FirstOrDefault();
                if (postDetails == null)
                {
                    TempData["ErrorMessage"] = "Post for default language not found.";
                }
                else
                {
                    finalUrl = "/Post/" + postDetails.Slug;
                }
            }
            else if (vmodel.RoleStartupType == StartupTypeText.Category)
            {
                var category = _categoryService.Get(long.Parse(vmodel.CategoryId));
                if (category == null)
                {
                    TempData["ErrorMessage"] = "Category not found.";
                }
                var categoryDetails = category.CategoryDetails.Where(x => x.Language == GlobalConfig.WebSite.Language).FirstOrDefault();
                if (categoryDetails == null)
                {
                    TempData["ErrorMessage"] = "Category for default language not found.";
                }
                else
                {
                    finalUrl = "/Category/" + categoryDetails.Slug;
                }
            }
            else if (vmodel.RoleStartupType == StartupTypeText.Module)
            {
                finalUrl = vmodel.ModuleSiteMenuUrl;
            }
            else
            {
                finalUrl = "";
            }
            return finalUrl;
        }

        public string GetSlug(string url)
        {
            var slug = "";
            if (!string.IsNullOrEmpty(url))
            {
                var parts = url.Split("/".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length > 1)
                {
                    slug = parts[parts.Length - 1];
                }
            }
            return slug;
        }

        #endregion
    }
}