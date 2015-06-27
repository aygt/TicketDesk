﻿using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using TicketDesk.Domain;
using TicketDesk.Domain.Model;

namespace TicketDesk.Web.Client.Controllers
{
    [RoutePrefix("navigation")]
    public class NavigationController : Controller
    {
        public TdDomainContext Context { get; set; }

        
        public NavigationController(TdDomainContext context)
        {
            Context = context;
        }

        [ChildActionOnly]
        [Route("project-menu")]
        public ActionResult ProjectMenu()
        {
            var projects = Context.Projects.OrderBy(p => p.ProjectName);
            if (projects.Count() < 2)
            {
                //only one project, don't render the menu at all
                return new EmptyResult();
            }

            var modelProjects = projects.ToList();
            
            //get user's selected project
            var projectId = AsyncHelper.RunSync(() =>  GetUserSelectedProjectId(modelProjects));

            //add the "all projects item" then get a select list to render
            modelProjects.Insert(0, new Project {ProjectId = 0, ProjectName = "All Projects", ProjectDescription = string.Empty});
            var model = modelProjects.ToSelectList(p => p.ProjectId.ToString(), p => p.ProjectName, projectId, false);
            
            return PartialView("_ProjectMenu", model);
        }

        [Route("change-project/{projectId:int}")]
        public async Task<ActionResult> ChangeProject(int projectId)
        {
            //only switch projects if project exists, or selection is the "all projects" option
            if (projectId == 0 || Context.Projects.Any(p => p.ProjectId == projectId))
            {
                await UpdateUserSelectedProject(projectId);
                await Context.SaveChangesAsync();
            }
            if (Request.UrlReferrer == null)
            {
                return RedirectToAction("Index", "Home");
            }
            return Redirect(Request.UrlReferrer.ToString());

        }

        private async Task<int> GetUserSelectedProjectId(IEnumerable<Project> projects)
        {
            var settings = AsyncHelper.RunSync(() => Context.UserSettingsManager.GetSettingsForUserAsync(Context.SecurityProvider.CurrentUserId));
            var projectId = settings.SelectedProjectId ?? 0;

            //if user's selected project points to a project that no longer exists, reset
            //  normally this wouldn't happen since the dbcontext will update user settings when projects are deleted 
            if (projectId != 0 && projects.All(p => p.ProjectId != projectId))
            {
                projectId = 0;
                await UpdateUserSelectedProject(projectId);
                Context.SaveChanges();
            }

            return projectId;

        }

        private async Task UpdateUserSelectedProject(int projectId)
        {
            var settings = await Context.UserSettingsManager.GetSettingsForUserAsync(Context.SecurityProvider.CurrentUserId);
            settings.SelectedProjectId = projectId;
        }
    }
}