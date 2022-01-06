using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Jodit.Controllers;
using Jodit.Models;
using Jodit.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Jodit.api
{
    [ApiController]
    [Route("[controller]")]
    public class ApiController : Controller
    {
        private ApplicationContext db;
        public ApiController(ApplicationContext context)
        {
            db = context;
        }
        
        [HttpGet]
        [Route("GetUserAPI")]
        public User GetUserAPI(string idSession)
        {
            UserSession session = db.UserSessions.FirstOrDefault(x => x.IdSession == idSession);
            if (session != null)
            {
                var user = db.Users.FirstOrDefault(u => u.IdUser == session.UserId);
                
                db.Entry(user)
                    .Collection(c => c.Groups)
                    .Load();

                var executers = db.UserMissions
                    .Include(u => u.Group)  // подгружаем данные по группам
                    .Include(c => c.Author)
                    .Include(a => a.Mission)
                    .Where(c => c.ExecutorId == user.IdUser)
                    .ToList();

                user.Executors = executers;

                return user; 
            }
            return new User();
        }
        
        
            
        [HttpGet]
        [Route("GetListMissionsExecutors")]
        public List<UserMission> GetListMissionsExecutors(string idSession)
        {
            UserSession session = db.UserSessions.FirstOrDefault(x => x.IdSession == idSession);
            if (session != null)
            {
                var user = db.Users.FirstOrDefault(u => u.IdUser == session.UserId);
                var executers = db.UserMissions
                    .Include(u => u.Group)  // подгружаем данные по группам
                    .Include(c => c.Author)
                    .Include(a => a.Mission)
                    .Where(c => c.ExecutorId == user.IdUser)
                    .ToList();
                return executers; 
            }
            return new List<UserMission>();
        }
        
        [HttpGet]
        [Route("GetMissionsExecutors")]
        public List<Mission> GetMissionsExecutors(string idSession)
        {
            UserSession session = db.UserSessions.FirstOrDefault(x => x.IdSession == idSession);
            if (session != null)
            {
                var user = db.Users.FirstOrDefault(u => u.IdUser == session.UserId);
                
                db.Entry(user)
                    .Collection(c => c.Executors)
                    .Load();
                List<Mission> missions = new List<Mission>();
                foreach (var userMission in user.Executors)
                {
                    missions.Add(userMission.Mission);
                }
                
                return missions; 
            }
            return new List<Mission>();
        }
        
        [HttpDelete]
        public void DeleteAPI(int id)
        {
            var user = db.Users.FirstOrDefault(i => i.IdUser == id);
            db.Users.Remove(user);
            db.SaveChanges();
        }

        [HttpDelete]
        public void GroupInvitationsRefuseAPI(int id)
        {
           
        }
        
        [HttpPut]
        public void GroupInvitationsAcceptAPI(int id)
        {
           
        }
        
        [HttpPost]
        [Route("GetGroupInvitesAPI")]
        public IEnumerable<GroupInvite> GetGroupInvitesAPI(string userEmail)
        {
            User user = db.Users.FirstOrDefault(i => i.Email == userEmail);
            var groupInvites = db.GroupInvites
                .Include(u => u.Group)  // подгружаем данные по группам
                .Include(c => c.InvitingUser)
                .Where(c => c.InvitedUserId == user.IdUser);
            return groupInvites;
        }

        [HttpPost]
        [Route("RegisterAPI")]
        public string RegisterAPI([FromBody] RegisterModel model)
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
                    
                    string sessionId = GetRandomString();
                    db.UserSessions.Add(new UserSession() {User = user, IdSession = sessionId});
                    db.Users.Add(user);
                    db.SaveChanges();
                    return sessionId;
                }
            }
            return "";
        }
        
        
        [HttpPost]
        [Route("LoginAPI")]
        public string LoginAPI([FromBody] LoginModel model)
        {
            if (ModelState.IsValid)
            {
                User user =  db.Users.FirstOrDefault
                    (u => u.Email == model.Email && u.UserPassword == model.Password);
                if (user != null)
                {
                    UserSession session = db.UserSessions.FirstOrDefault(x => x.UserId == user.IdUser);
                    if (session == null)
                    {
                        string sessionId = GetRandomString();
                        db.UserSessions.Add(new UserSession() {User = user, IdSession = sessionId});
                        db.SaveChanges();
                        return sessionId;
                    }
                }
            }
            return "";
        }
        
        [HttpDelete]
        [Route("LogoutAPI")]
        public bool LogoutAPI(string idSession)
        {
            UserSession session = db.UserSessions.FirstOrDefault(x => x.IdSession == idSession);
            if (session != null)
            {
                db.Entry(session)
                    .Reference(c => c.User)
                    .Load();
                db.UserSessions.Remove(session); 
                db.SaveChanges();
                return true;
            }
            return false;
        }
        
        
        
        [HttpPost]
        [Route("AddGroup")]
        public int AddGroup(string idSession, [FromBody] Group group)
        {
            UserSession session = db.UserSessions.FirstOrDefault(x => x.IdSession == idSession);
            if (group != null && session != null)
            {
                group.DateOfCreation = DateTime.Today;
                var user = db.Users.FirstOrDefault(u => u.IdUser == session.UserId);
               
                db.Groups.Add(group);
                if (user != null)
                {
                    user.UserGroups.Add(new UserGroup {Group = group, IsAdmin = true, User = user});
                }
                db.SaveChanges();
                return group.IdGroup;
            }
            return -1;
        }
        
    
        
        
        [HttpGet]
        [Route("TakeMission")]
        public bool TakeMission(int idMission, string idSession)
        {
            UserSession session = db.UserSessions.FirstOrDefault(x => x.IdSession == idSession);
            if (session != null)
            {
                try
                {
                    var user = db.Users.FirstOrDefault(u => u.IdUser == session.UserId);
                
                    var userMission = db.UserMissions
                        .Where(a => a.MissionId == idMission)
                        .FirstOrDefault(a => a.ExecutorId == user.IdUser);
              
                    var a = db.UserMissions
                        .Include(c => c.Executor)
                        .Where(c => c.MissionId == userMission.MissionId).ToList();
              
              
                    var newList = a.Where(x=>x.Executor.IdUser != user.IdUser).ToList();
                    foreach (var um in newList)
                    {
                        db.UserMissions.Remove(um);
                    }
                    userMission.Status = MissionController.STATUS.TAKE.ToString(); 
                    db.SaveChanges();
                }
                catch (Exception ee)
                {
                    return false;
                }
            }
            return true;
        }
        
        
        [HttpGet]
        [Route("RefuseMission")]
        public bool RefuseMission(int idMission, string idSession)
        {
            UserSession session = db.UserSessions.FirstOrDefault(x => x.IdSession == idSession);
            if (session != null)
            {
                try
                {
                    var user = db.Users.FirstOrDefault(u => u.IdUser == session.UserId);
                    var userMission = db.UserMissions
                        .Where(a => a.MissionId == idMission)
                        .FirstOrDefault(a => a.ExecutorId == user.IdUser);
                    userMission.Status = MissionController.STATUS.REFUSE.ToString(); 
                    db.SaveChanges();
                    return true;
                }
                catch (Exception ee)
                {
                    
                }
            }
            return false;
        }
        public string GetRandomString()
        {
            int [] arr = new int [25]; 
            Random rnd = new Random();
            string str = "";
 
            for (int i=0; i<arr.Length; i++)
            {
                arr[i] = rnd.Next(65,90);
                str += (char) arr[i];
            }
            return str;
        }
    }
    
}