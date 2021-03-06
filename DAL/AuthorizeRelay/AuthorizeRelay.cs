﻿using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.Owin.Security;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Runtime.Remoting.Messaging;
using Lumos.Entity;
namespace Lumos.DAL.AuthorizeRelay
{
    public class AspNetIdentiyAuthorizeRelay<TUser> where TUser : SysUser
    {

        #region 属性和变量
        private static SysRoleManager _roleManager;
        private static SysUserManager<TUser> _userManager;


        public SysRoleManager RoleManager
        {
            get
            {
                return _roleManager;
            }
        }

        public SysUserManager<TUser> UserManager
        {
            get
            {
                return _userManager;
            }
        }


        private static AuthorizeRelayDbContext _db;
        private static readonly object syncObject = new object();
        private static AuthorizeRelayDbContext SetDb(AuthorizeRelayDbContext contextDb)
        {
            //这里可以保证只实例化一次
            //即在第一次调用时实例化
            //以后调用便不会再实例化

            //第一重  == null
            if (_db == null)
            {
                lock (syncObject)
                {
                    //第二重  == null
                    if (_db == null)
                    {
                        _db = contextDb;
                        _roleManager = new SysRoleManager(new RoleStore<SysRole,int,SysUserRole>(_db));
                        _userManager = new SysUserManager<TUser>(new UserStore<TUser,SysRole,int,SysUserLogin, SysUserRole, SysUserClaim>(_db));
                    }
                }
            }
            return _db;
        }


        public  AuthorizeRelayDbContext Db
        {
            get
            {
                return _db;
            }
        }
        #endregion

        public AspNetIdentiyAuthorizeRelay()
        {
            if (_db == null)
            {
                throw new ArgumentOutOfRangeException("_db值为null,没有初始化DbContext。注意：调用前先调用AspNetIdentiyAuthorizeRelay(AuthorizeRelayDbContext context)");
            }

        }
        private static string efKey = "ef_key";
        public AspNetIdentiyAuthorizeRelay(AuthorizeRelayDbContext context)
        {

           // SetDb(context);

            AuthorizeRelayDbContext db = CallContext.GetData(efKey) as AuthorizeRelayDbContext;
            if (db == null)
            {
                _db = context;
                CallContext.SetData(efKey, _db);
            }
            else
            {
                _db = db;
            }
            if (_db == null)
            {
                throw new ArgumentOutOfRangeException("context值为null,没有初始化context。注意：调用前先调用AspNetIdentiyAuthorizeRelay(AuthorizeRelayDbContext context)");
            }
            _roleManager = new SysRoleManager(new RoleStore<SysRole, int, SysUserRole>(_db));
            _userManager = new SysUserManager<TUser>(new UserStore<TUser, SysRole, int, SysUserLogin, SysUserRole, SysUserClaim>(_db));

        }

        #region 登录和注销
        private IAuthenticationManager AuthenticationManager
        {
            get
            {
                return HttpContext.Current.GetOwinContext().Authentication;
            }
        }

        private string GetIPAddress()
        {

            string user_IP = string.Empty;
            if (System.Web.HttpContext.Current.Request.ServerVariables["HTTP_VIA"] != null)
            {
                if (System.Web.HttpContext.Current.Request.ServerVariables["HTTP_X_FORWARDED_FOR"] != null)
                {
                    user_IP = System.Web.HttpContext.Current.Request.ServerVariables["HTTP_X_FORWARDED_FOR"].ToString();
                }
                else
                {
                    user_IP = System.Web.HttpContext.Current.Request.UserHostAddress;
                }
            }
            else
            {
                user_IP = System.Web.HttpContext.Current.Request.ServerVariables["REMOTE_ADDR"].ToString();
            }
            return user_IP;
        }


        public TUser SignIn(string userName, string password, bool isPersistent)
        {
            var user = _userManager.Find<TUser,int>(userName, password);
            if (user != null)
            {
                if (!user.IsDelete && !user.IsDisable)
                {
                    user.LastLoginTime = DateTime.Now;
                    user.LastLoginIp = GetIPAddress();
                    _db.SaveChanges();
                    AuthenticationManager.SignOut(DefaultAuthenticationTypes.ExternalCookie);
                    var identity = _userManager.CreateIdentity(user, DefaultAuthenticationTypes.ApplicationCookie);
                    AuthenticationManager.SignIn(new AuthenticationProperties() { IsPersistent = isPersistent }, identity);
                }
            }

            return user;
        }

        public void SignOut()
        {
            AuthenticationManager.SignOut();
        }
        #endregion

        #region 用户相关
        /// <summary>
        /// 通过用户ID获取用户信息
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public TUser GetUser(int id)
        {
     
            return _userManager.FindById<TUser,int>(id);
        }

        /// <summary>
        /// 获取用户的所有菜单
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public List<SysMenu> GetUserMenus(int userId)
        {
            List<SysMenu> listMenu = new List<SysMenu>();
            var model =
                from menu in _db.SysMenu
                where
                (from rolemenu in _db.SysRoleMenu
                 where
                 (from userrole in _db.SysUserRole where rolemenu.RoleId == userrole.RoleId && userrole.UserId == userId select userrole.RoleId)
                 .Contains(rolemenu.RoleId)
                 select rolemenu.MenuId).Contains(menu.Id)
                select menu;


            if (model != null)
            {
                listMenu = model.OrderByDescending(m=>m.Priority).ToList();
            }
            return listMenu;
        }

        /// <summary>
        /// 获取用户的所有权限
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public List<string> GetUserPermissions(int userId)
        {
            List<string> list = new List<string>();

            var model =
                (from rolepermission in _db.SysRolePermission
                 where
                 (from userrole in _db.SysUserRole where rolepermission.RoleId == userrole.RoleId && userrole.UserId == userId select userrole.RoleId)
                 .Contains(rolepermission.RoleId)
                 select rolepermission.PermissionId).Distinct();
            if (model != null)
            {
                list = model.ToList();
            }
            return list;
        }


        public bool UserExists(string username)
        {
            var idResult = _db.Users.Where(m=>m.UserName==username).FirstOrDefault();
            if (idResult != null)
                return true;

            return false;
        }



        /// <summary>
        /// 创建用户
        /// </summary>
        /// <param name="user"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public bool CreateUser(TUser user, string password)
        {
            var idResult = _userManager.Create<TUser,int>(user, password);
            return idResult.Succeeded;
        }

        /// <summary>
        /// 修改用户
        /// </summary>
        /// <param name="user"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public bool UpdateUser(TUser user, string password)
        {
            if (!string.IsNullOrWhiteSpace(password))
            {
                user.PasswordHash = _userManager.PasswordHasher.HashPassword(password);
            }
  
            var idResult = _userManager.Update<TUser,int>(user);
            return idResult.Succeeded;
        }


        /// <summary>
        /// 修改用户
        /// </summary>
        /// <param name="user"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public bool DeleteUser(int userId)
        {
            SysUser user = _db.Users.Find(userId);
            user.IsDelete = true;
            user.Mender = AuthenticationManager.User.Identity.GetUserId<int>();
            user.LastUpdateTime = DateTime.Now;
            int r =_db.SaveChanges();
            if(r>0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        //重置密码
        public bool ResetPassword(int userId,string password)
        {

            string resetPwd = password;
            SysUser user = GetUser(userId);
            user.PasswordHash = null;
            user.Mender = AuthenticationManager.User.Identity.GetUserId<int>();
            user.LastUpdateTime = DateTime.Now;
            _db.SaveChanges();
            IdentityResult result = _userManager.AddPassword(userId, resetPwd);
            return result.Succeeded;

        }

        //修改密码
        public bool ChangePassword(int userId, string oldPassword, string newPassword)
        {

            IdentityResult result = _userManager.ChangePassword(userId, oldPassword, newPassword);
            if(result.Succeeded)
            {
                return true;
            }
            else
            {
                return false;
            }

        }

        #endregion 用户相关

        #region 角色相关

        /// <summary>
        /// 判断角色名称是否存在
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public bool RoleExists(string name)
        {
            return _roleManager.RoleExists(name);
        }

        /// <summary>
        /// 创建角色
        /// </summary>
        /// <param name="role"></param>
        /// <returns></returns>
        public bool CreateRole(SysRole role)
        {
            var idResult = _roleManager.Create(role);
            return idResult.Succeeded;
        }

        /// <summary>
        /// 删除角色 删除角色菜单 删除角色用户 删除角色权限
        /// </summary>
        /// <param name="roleId"></param>
        public void DeleteRole(int roleId)
        {
            var roleUsers = _db.SysUserRole.Where(u => u.RoleId == roleId).Distinct();

            var roleMenus = _db.SysRoleMenu.Where(u => u.RoleId == roleId).Distinct();

            var rolePermission = _db.SysRolePermission.Where(u => u.RoleId == roleId).Distinct();

            var role = _db.Roles.Find(roleId);

            foreach (var user in roleUsers)
            {
                _db.SysUserRole.Remove(user);
            }

            foreach (var menu in roleMenus)
            {
                _db.SysRoleMenu.Remove(menu);
            }

            foreach (var permission in rolePermission)
            {
                _db.SysRolePermission.Remove(permission);
            }

            _db.Roles.Remove(role);
            _db.SaveChanges();
        }

        /// <summary>
        /// 更新角色
        /// </summary>
        /// <param name="roleId"></param>
        public bool UpdateRole(SysRole role)
        {
            var idResult = _roleManager.Update(role);
            return idResult.Succeeded;
        }

        /// <summary>
        /// 添加用户到角色
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="roleName"></param>
        /// <returns></returns>
        public bool AddUserToRole(int userId, int roleId)
        {
            _db.SysUserRole.Add(new SysUserRole { UserId = userId, RoleId = roleId });
            _db.SaveChanges();
            return true;
        }

        /// <summary>
        /// 清楚用户所在的角色
        /// </summary>
        /// <param name="userId"></param>
        public void ClearUserRoles(int userId)
        {
            var user = _userManager.FindById(userId);
            var currentRoles = new List<SysUserRole>();
            currentRoles.AddRange(user.Roles);
            foreach (var role in currentRoles)
            {
                //_userManager.RemoveFromRole(userId, role.);
            }
        }

        /// <summary>
        /// 清楚用户所在的角色
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="roleName"></param>
        public void RemoveUserFromRole(int userId, int roleId)
        {

            SysUserRole userRole = _db.SysUserRole.Find(roleId, userId);
            _db.SysUserRole.Remove(userRole);
            _db.SaveChanges();
        }


        /// <summary>
        /// 获取角色的菜单
        /// </summary>
        /// <param name="roleId"></param>
        /// <returns></returns>
        public List<SysMenu> GetRoleMenus(int roleId)
        {
            var model = from c in _db.SysMenu
                        where
                            (from o in _db.SysRoleMenu where o.RoleId == roleId select o.MenuId).Contains(c.Id)
                        select c;

            return model.ToList();
        }

        /// <summary>
        /// 添加菜单到角色
        /// </summary>
        /// <param name="menuId"></param>
        /// <param name="roleId"></param>
        /// <returns></returns>
        public bool SaveRoleMenu(int roleId, int[] menuIds)
        {

            List<SysRoleMenu> roleMenuList = _db.SysRoleMenu.Where(r => r.RoleId == roleId).ToList();

            foreach (var roleMenu in roleMenuList)
            {
                _db.SysRoleMenu.Remove(roleMenu);
            }

            foreach (var menuId in menuIds)
            {
                _db.SysRoleMenu.Add(new SysRoleMenu { RoleId = roleId, MenuId = menuId });
            }

            _db.SaveChanges();

            return true;
        }

        /// <summary>
        /// 添加权限到角色
        /// </summary>
        /// <param name="permissionId"></param>
        /// <param name="menuName"></param>
        /// <returns></returns>
        public bool SaveRolePermission(int roleId, string[] permissionIds)
        {

            List<SysRolePermission> rolePermissionList = _db.SysRolePermission.Where(r => r.RoleId == roleId).ToList();

            foreach (var m in rolePermissionList)
            {
                _db.SysRolePermission.Remove(m);
            }

            foreach (var m in permissionIds)
            {
                _db.SysRolePermission.Add(new SysRolePermission { RoleId = roleId, PermissionId = m });
            }

            _db.SaveChanges();

            return true;
        }

        #endregion 角色相关

        #region 菜单相关
        public int CreateMenu(SysMenu enity)
        {
            _db.SysMenu.Add(enity);
            _db.SaveChanges();
            return enity.Id;
        }

        /// <summary>
        /// 更新菜单
        /// </summary>
        /// <param name="id"></param>
        public void UpdateMenu(SysMenu enity)
        {
            _db.Entry(enity).State = EntityState.Modified;
            _db.SaveChanges();
        }

        /// <summary>
        /// 删除菜单
        /// </summary>
        /// <param name="id"></param>
        public void DeleteMenu(int menuid)
        {
            SysMenu model = _db.SysMenu.Where(m => m.Id == menuid).FirstOrDefault();

            _db.SysMenu.Remove(model);

            var menu = _db.SysRoleMenu.Where(r => r.MenuId == menuid).ToList();
            foreach (var m in menu)
            {
                _db.SysRoleMenu.Remove(m);
            }

            _db.SaveChanges();
        }

        #endregion 菜单相关

        #region 权限相关

        public List<SysPermission> GetPermissionList(PermissionCode permission)
        {
            Type t = permission.GetType();
            List<SysPermission> list = new List<SysPermission>();
            SysPermission p = new SysPermission();
            p.Id = "0";
            p.Name = "权限集合";
            p.PId = "";
            list.Add(p);
            list = GetBasePermissionList(t, list);
            return list;
        }


        private List<SysPermission> GetBasePermissionList(Type t, List<SysPermission> list)
        {
            if (t.Name != "Object")
            {
                System.Reflection.FieldInfo[] properties = t.GetFields();
                foreach (System.Reflection.FieldInfo property in properties)
                {
                    string pId = "0";
                    object[] typeAttributes = property.GetCustomAttributes(false);
                    foreach (PermissionCodeAttribute attribute in typeAttributes)
                    {
                        pId = attribute.PId;
                    }
                    object id = property.GetValue(null);
                    string name = property.Name;
                    SysPermission model = new SysPermission();
                    model.Id = id.ToString();
                    model.Name = name;
                    model.PId = pId;
                    list.Add(model);
                }
                list = GetBasePermissionList(t.BaseType, list);
            }
            return list;
        }

        #endregion
    }
}
