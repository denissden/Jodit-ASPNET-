using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Jodit.Models;
using Jodit.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Jodit.Controllers
{
    public class GroupController : Controller
    {
        private ApplicationContext db;
        public GroupController(ApplicationContext context)
        {
            db = context;
        }
        
        public IActionResult ListGroups()
        {
            var userName = User.Identity.Name;
                User user = db.Users.FirstOrDefault(i => i.Email == userName);
                var userGroups = db.UserGroups
                    .Include(g => g.Group)
                    .Where(i => i.UserId == user.IdUser).ToList();
                
                
            GroupModel accountModel = new GroupModel
            {
                UserGroups = userGroups
            };
            return View(accountModel);
            
        }

        public IActionResult CreateGroup() => View();
        
        
         [HttpPost]
         public async Task<IActionResult> CreateGroup(Group group)
         {
             group.DateOfCreation = DateTime.Today;
             var userName = User.Identity.Name;
             User user = db.Users.FirstOrDefault(i => i.Email == userName);
             
             db.Groups.Add(group);
             if (user != null)
             {
                 user.UserGroups.Add(new UserGroup {Group = group, IsAdmin = true, User = user});
             }

             await db.SaveChangesAsync();
              return RedirectToAction("ListGroups", "Group");
         }


        public IActionResult CreateStatement(int id)
        {
            var userName = User.Identity.Name;
            User user = db.Users.FirstOrDefault(i => i.Email == userName);
            Group group = db.Groups.FirstOrDefault(gr => gr.IdGroup == id);
            db.Entry(group)
                .Collection(c => c.Users)
                .Load();

            //  Dictionary<DateTime, User> dictionary = group.CalculateToDate(DateTime.Now.Date.AddDays(30));
            ArrayList list = group.CalculateToDate(DateTime.Now.Date.AddDays(30));
            var listBuf = new ArrayList();
            foreach (UserDateTime item in list)
            {
                if (item.User.IdUser == user.IdUser)
                {
                    listBuf.Add(item.DateTime);
                }
            }

            ScheduleStatementModel model = new ScheduleStatementModel()
            {
                DateTimes = listBuf,
                ScheduleStatement = new ScheduleStatement()
                {
                    Group = group,
                    BeforeUser = user,
                }
            };

            return View(model);
        }



        [HttpPost]
         public async Task<IActionResult> CreateStatement(ScheduleStatementModel model)
         {
             if (ModelState.IsValid)
             {
                 var user = await db.Users.FirstOrDefaultAsync
                     (i => i.IdUser == model.ScheduleStatement.BeforeUser.IdUser);
                 var group = await db.Groups
                     .FirstOrDefaultAsync(gr => gr.IdGroup == 
                                                model.ScheduleStatement.Group.IdGroup);
                 var selectedValue = Request.Form["chooseDate"];
                 var parsedDate = DateTime.Parse(selectedValue);
                 model.ScheduleStatement.ReplacementDate = parsedDate;
                 model.ScheduleStatement.BeforeUser = user;
                 model.ScheduleStatement.Group = group;
                 group.ScheduleStatements.Add(model.ScheduleStatement);
                 await db.SaveChangesAsync();
             }
             return RedirectToAction("ListGroups", "Group");
         }

        public IActionResult CreateScheduleChange(int idGroup, int idUserBefore, int idScheduleStatement)
        {
            var userName = User.Identity.Name;
            User user = db.Users.FirstOrDefault(i => i.Email == userName);
            User userBefore = db.Users.FirstOrDefault(i => i.IdUser == idUserBefore);
            Group group = db.Groups.FirstOrDefault(gr => gr.IdGroup == idGroup);
            db.Entry(group)
                .Collection(c => c.Users)
                .Load();

            // Dictionary<DateTime, User> dictionary = group.CalculateToDate(DateTime.Now.Date.AddDays(30));
            ArrayList list = group.CalculateToDate(DateTime.Now.Date.AddDays(30));
            var listBuf = new ArrayList();
            foreach (UserDateTime item in list)
            {
                if (item.User.IdUser == user.IdUser)
                {
                    listBuf.Add(item.DateTime);
                }
            }

            ScheduleChangeModel model = new ScheduleChangeModel
            {
                DateTimes = listBuf,
                ScheduleChange = new ScheduleChange
                {
                    Group = group,
                    AfterUser = user,
                    BeforeUser = userBefore
                },
                IdScheduleStatement = idScheduleStatement
            };

            return View(model);
        }

        [HttpPost]
         public async Task<IActionResult> CreateScheduleChange(ScheduleChangeModel model)
         {
             if (ModelState.IsValid)
             {
                 var userAfter = db.Users
                     .FirstOrDefault(i => i.IdUser == model.ScheduleChange.AfterUser.IdUser);
                 var userBefore = db.Users
                     .FirstOrDefault(i => i.IdUser == model.ScheduleChange.BeforeUser.IdUser);

                 var statement = db.ScheduleStatements
                     .FirstOrDefault(i => i.IdScheduleStatement == model.IdScheduleStatement);
                 
                 var group = await db.Groups.FirstOrDefaultAsync(gr => gr.IdGroup == model.ScheduleChange.Group.IdGroup);
                 
                 var selectedValue = Request.Form["chooseDate"];
                 var parsedDate = DateTime.Parse(selectedValue);
                 
                 model.ScheduleChange.AfterUserDate = parsedDate;
                 model.ScheduleChange.BeforeUserDate = statement.ReplacementDate;
                 model.ScheduleChange.BeforeUser = userBefore;
                 model.ScheduleChange.AfterUser = userAfter;
                 model.ScheduleChange.Group = group;
                 
                 
                 group.ScheduleChanges.Add(model.ScheduleChange);
                 db.ScheduleStatements.Remove(statement);
                 await db.SaveChangesAsync();
             }
             return RedirectToAction("ListGroups", "Group");
         }
         
         public IActionResult ListScheduleStatement(int id)
         {
             var userName = User.Identity.Name;
             User user = db.Users.FirstOrDefault(i => i.Email == userName);
             
             var list = db.ScheduleStatements
                 .Include(u => u.BeforeUser)
                 .Include(u => u.Group)// подгружаем данные по группам
                 .Where(c => c.BeforeUser.IdUser != user.IdUser)
                 .Where(c => c.Group.IdGroup == id)
                 .ToList();
             
             return View(list);
         }
         

         public async Task<IActionResult> Details(int? id, DateTime date)
         {
             if (id != null)
             {
                 var userName = User.Identity.Name;
                 User user = db.Users.FirstOrDefault(i => i.Email == userName);
                 Group group = await db.Groups
                     .Include(x => x.Users)
                     .Include(x => x.ScheduleChanges)
                     .FirstOrDefaultAsync(gr => gr.IdGroup == id);

                 var userGroup = await db.UserGroups
                     .Where(i => i.GroupId == id)
                     .FirstOrDefaultAsync(i => i.UserId == user.IdUser);
                 
                 if (userGroup != null)
                 {
                     if (date.Date != DateTime.MinValue)
                     {
                        UserDateTime ud = group.CalculateByDate(date.Date); 
                        string str =  "Date: " + ud.DateTime.ToShortDateString() + " user: " + 
                                      ud.User.FirstName + " " + ud.User.SecondName; 
                        ViewData["ResultCalculateByDate"] = str;
                     }
                     
                     GroupDetailsModel model = new GroupDetailsModel()
                     {
                         UserGroup = userGroup,
                         ScheduleChanges = group.ScheduleChanges
                     };
                     
                     return View(model);
                 }
             }
             return NotFound();
         }
         
         
         public async Task<IActionResult> LeaveGroup(int? idUserGroup)
         {
             if (idUserGroup != null)
             {
                 var userGroup = await db.UserGroups
                     .Include(x => x.Group)
                     .Include(x => x.User)
                     .FirstOrDefaultAsync(i => i.IdUserGroup == idUserGroup);

                 if (userGroup != null)
                 {
                     db.UserGroups.Remove(userGroup);
                     await db.SaveChangesAsync();
                     return RedirectToAction("ListGroups", "Group");
                 }
             }
             return NotFound();
         }
         
         
         public async Task<IActionResult> Edit(int? id)
         {
             if (id != null)
             {
                 var group = await db.Groups.FirstOrDefaultAsync(gr => gr.IdGroup == id);
                 if (group != null)
                 {
                     return View(group);
                 }
             }
             return NotFound();
         }

         [HttpPost]
         public async Task<IActionResult> Edit(Group group)
         {
             db.Groups.Update(group);
             await db.SaveChangesAsync();
             return RedirectToAction("ListGroups");
         }
         
         [HttpGet]
         [ActionName("Delete")]
         public async Task<IActionResult> ConfirmDelete(int? id)
         {
             if (id != null)
             {
                 var group = await db.Groups.FirstOrDefaultAsync(gr => gr.IdGroup == id);
                 if (group != null)
                     return View(group);
             }
             return NotFound();
         }
         
         [HttpPost]
         public async Task<IActionResult> Delete(int? id)
         {
             if (id != null)
             {
                 var group = await db.Groups.FirstOrDefaultAsync(gr => gr.IdGroup == id);

                 if (group != null)
                 {
                     
                     db.Entry(group)
                         .Collection(c => c.Users)
                         .Load();
                     
                     
                     db.Groups.Remove(group);
                     await db.SaveChangesAsync();
                     return RedirectToAction("ListGroups");
                 }
             }
             return NotFound();
         }
    }
}