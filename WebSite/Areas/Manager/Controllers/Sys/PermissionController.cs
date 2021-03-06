﻿using Lumos.Common;
using Lumos.DAL;
using Lumos.DAL.AuthorizeRelay;
using Lumos.Entity;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;

namespace WebSite.Areas.Manager.Controllers
{

    public class PermissionController : ManagerController
    {
    
        public ViewResult Index()
        {
            return View();
        }

        /// <summary>
        /// 获取权限树形列表
        /// </summary>
        /// <returns></returns>
        public JsonResult GetPermissionTree()
        {
            var identity = new AspNetIdentiyAuthorizeRelay<SysUser>(CurrentDb);
            object json = ConvertToZTreeJson(identity.GetPermissionList(new PermissionCode()).ToArray(), "id", "pid", "name", "opfun");
            return Json(ResultType.Success, json);
        }

    }
}