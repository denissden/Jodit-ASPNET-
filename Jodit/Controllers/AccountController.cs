using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Jodit.Models;
using Jodit.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Jodit.Controllers
{
    public class AccountController : Controller
    {
        private ApplicationContext db;
        public AccountController(ApplicationContext context)
        {
            db = context;
        }
        
        
        public IActionResult Login()
        {
            return View();
        }
        
        public IActionResult Register()
        {
            return View();
        }
        
        [HttpPost]
        public async Task<IActionResult> Register(RegisterModel model)
       {
           if (ModelState.IsValid)
           {
               User findUser =  db.Users.FirstOrDefault(u => u.Email == model.Email);
                
               if (findUser == null)
               {
                   User user = new User
                   {
                       FirstName = model.FirstName,
                       SecondName = model.SecondName,
                       LastName = model.LastName,
                       Login = model.Login,
                       Phone = model.Phone,
                       Email = model.Email,
                       UserPassword = model.Password
                   };
                   await Authenticate(user.Email); 
                   db.Users.Add(user);
                   db.SaveChanges();
                   return RedirectToAction("Account", "Account");
               }
           }
           return View(model);
       }
       
        [HttpPost]
       public async Task<IActionResult> Login(LoginModel model)
       {
           if (ModelState.IsValid)
           {
               User user =  db.Users.FirstOrDefault
                   (u => u.Email == model.Email && u.UserPassword == model.Password);
               if (user != null)
               {
                   await Authenticate(user.Email); 
                   return RedirectToAction("Account", "Account");
               }
               ModelState.AddModelError("", "Некорректные логин и(или) пароль");
           }
           return View(model);
       }
       
      private async Task Authenticate(string userName)
       {
           // создаем один claim
           var claims = new List<Claim>
           {
               new Claim(ClaimsIdentity.DefaultNameClaimType, userName)
           };
           // создаем объект ClaimsIdentity
           ClaimsIdentity id = new ClaimsIdentity(claims, "ApplicationCookie",
               ClaimsIdentity.DefaultNameClaimType, ClaimsIdentity.DefaultRoleClaimType);
           // установка аутентификационных куки
           await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(id));
       }
        
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account");
        }
        
        [Authorize]
        public IActionResult Account()
        {
            var userName = User.Identity.Name;
            User user = db.Users.FirstOrDefault(u => u.Email == userName);
            
            db.Entry(user)
                .Collection(c => c.UserGroups)
                .Load();
            
            db.Entry(user)
                .Collection(c => c.Groups)
                .Load();
            
            AccountModel model = new AccountModel()
            {
                user = user
            };
            return View(model);
        }
        
        [Authorize]
        public IActionResult GroupInvitations()
        { 
            var userName = User.Identity.Name;
            User user = db.Users.FirstOrDefault(u => u.Email == userName);
            
           
           var groupInvites = db.GroupInvites
               .Include(u => u.Group)  // подгружаем данные по группам
               .Include(c => c.InvitingUser)
               .Where(c => c.InvitedUserId == user.IdUser) 
               .ToList();

           return View(groupInvites);
        }

        
        [Authorize]
        public async Task<IActionResult> AcceptGroupInvitations(int idGroupInvitations)
        {
            GroupInvite groupInvite = db.GroupInvites.FirstOrDefault(gi => gi.IdGroupInvite == idGroupInvitations);
            db.Entry(groupInvite)
                .Reference(c => c.Group)
                .Load();
            
            db.Entry(groupInvite)
                .Reference(c => c.InvitingUser)
                .Load();
            
            db.Entry(groupInvite)
                .Reference(c => c.InvitedUser)
                .Load();
            if (groupInvite != null)
            {
                groupInvite.InvitedUser.UserGroups.Add(new UserGroup {Group = groupInvite.Group, 
                    IsAdmin = false, User = groupInvite.InvitedUser});
                db.GroupInvites.Remove(groupInvite);
            }
            db.SaveChanges();
            return RedirectToAction("GroupInvitations", "Account");
        }
        
        [Authorize]
        public async Task<IActionResult> RefuseGroupInvitations(int idGroupInvitations)
        {
            GroupInvite groupInvite = db.GroupInvites.FirstOrDefault(gi => gi.IdGroupInvite == idGroupInvitations);
            db.Entry(groupInvite)
                .Reference(c => c.Group)
                .Load();
            
            db.Entry(groupInvite)
                .Reference(c => c.InvitingUser)
                .Load();
            
            db.Entry(groupInvite)
                .Reference(c => c.InvitedUser)
                .Load();

            if (groupInvite != null)
            {
                db.GroupInvites.Remove(groupInvite);
            }
            db.SaveChangesAsync();
            return RedirectToAction("GroupInvitations", "Account");
        }
    }
}