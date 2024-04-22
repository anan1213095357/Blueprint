using Microsoft.AspNetCore.Mvc;
using System.Collections.Frozen;
using System.Linq;
using System.Xml.Linq;
using static FreeSql.Internal.GlobalFilter;

namespace Blueprint.Net.Server.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class BlueprintController(ILogger<BlueprintController> logger, IFreeSql fsql) : ControllerBase
    {
        private readonly ILogger<BlueprintController> _logger = logger;
        private readonly IFreeSql _fsql = fsql;

        [HttpGet("GetProjects")]
        public async Task<ActionResult<IEnumerable<Project>>> GetProjects()
        {
            var projects = await _fsql.Select<Project>()
                .IncludeMany(p => p.Workspaces, a => a.IncludeMany(b => b.Links))
                .IncludeMany(p => p.Workspaces, a => a.IncludeMany(b => b.Cards))
                .IncludeMany(p => p.Workspaces, a => a.IncludeMany(b => b.Cards, c => c.IncludeMany(d => d.Nodes)))
                .ToListAsync();

            List<long> linkIds = [];
            List<long> workspaceIds = [];

            foreach (var project in projects)
            {
                foreach (var workspace in project.Workspaces)
                {
                    workspaceIds.Add(workspace.Id);

                }
            }

            var links = await _fsql.Select<Link>().Where(p => workspaceIds.Contains(p.WorkspaceId)).ToListAsync();

            foreach (var link in links)
            {
                linkIds.Add(link.SourceId);
                linkIds.Add(link.TargetId);
            }

            var nodeConnectInfos = await _fsql.Select<NodeConnectInfo>().Where(p => linkIds.Contains(p.Id)).ToListAsync();

            foreach (var project in projects)
            {
                foreach (var workspace in project.Workspaces)
                {

                    foreach (var link in links.Where(p => p.WorkspaceId == workspace.Id))
                    {
                        link.Source = nodeConnectInfos.FirstOrDefault(p => p.Id == link.SourceId) ?? new();
                        link.Target = nodeConnectInfos.FirstOrDefault(p => p.Id == link.TargetId) ?? new();

                        workspace.Links.Add(link);
                    }
                }
            }


            return Ok(projects);
        }

        [HttpPost("Project")]
        public async Task<ActionResult<Project>> Project([FromBody] Project project)
        {

            var projectRepo = _fsql.GetRepository<Project>();

            projectRepo.DbContextOptions.EnableCascadeSave = true;

            project = await projectRepo.InsertOrUpdateAsync(project);
            projectRepo.Attach(project);

            foreach (var workspaces in project.Workspaces)
            {
                foreach (var node in workspaces.Links)
                {

                    if (node.SourceId == 0)
                    {
                        node.SourceId = await _fsql.Insert(node.Source).ExecuteIdentityAsync();
                        node.Source.Id = node.SourceId;
                    }
                    else
                    {
                        await _fsql.Update<NodeConnectInfo>().SetSource(node.Source).Where(p => p.Id == node.SourceId).ExecuteAffrowsAsync();
                    }

                    if (node.TargetId == 0)
                    {
                        node.TargetId = await _fsql.Insert(node.Target).ExecuteIdentityAsync();
                        node.Target.Id = node.TargetId;
                    }
                    else
                    {
                        await _fsql.Update<NodeConnectInfo>().SetSource(node.Target).Where(p => p.Id == node.TargetId).ExecuteAffrowsAsync();
                    }
                }
            }
            await projectRepo.UpdateAsync(project);

            return Ok(project);

        }


        [HttpDelete("DeleteProject/{id}")]
        public async Task<IActionResult> DeleteProject(long id)
        {

            var repo = _fsql.GetRepository<Project>();

            repo.DbContextOptions.EnableCascadeSave = true;

            var project = await repo.Select
                .IncludeMany(p => p.Workspaces, a => a.IncludeMany(b => b.Cards, c => c.IncludeMany(d => d.Nodes)))
                .IncludeMany(p => p.Workspaces, a => a.IncludeMany(b => b.Links))
                .Where(p => p.Id == id)
                .FirstAsync();

            var deleteIds = new List<long>();

            foreach (var workspace in project.Workspaces)
            {
                foreach (var node in workspace.Links)
                {
                    deleteIds.Add(node.SourceId);
                    deleteIds.Add(node.TargetId);
                }
            }
            await _fsql.Delete<NodeConnectInfo>().Where(p => deleteIds.Contains(p.Id)).ExecuteAffrowsAsync();

            var ret = await repo.DeleteAsync(project);
            if (ret > 0)
                return Ok();
            return NotFound();
        }
        [HttpDelete("DeleteWorkspace/{id}")]
        public async Task<IActionResult> DeleteWorkspace(long id)
        {
            var repo = _fsql.GetRepository<Workspace>();

            repo.DbContextOptions.EnableCascadeSave = true;

            var workspace = await repo.Select
                .IncludeMany(p => p.Cards, a => a.IncludeMany(b => b.Nodes))
                .IncludeMany(p => p.Links)
                .Where(p => p.Id == id)
                .FirstAsync();


            var deleteIds = new List<long>();

            foreach (var node in workspace.Links)
            {
                deleteIds.Add(node.SourceId);
                deleteIds.Add(node.TargetId);
            }
            await _fsql.Delete<NodeConnectInfo>().Where(p => deleteIds.Contains(p.Id)).ExecuteAffrowsAsync();

            var ret = await repo.DeleteAsync(workspace);
            if (ret > 0)
                return Ok();
            return NotFound();
        }

        [HttpDelete("DeleteLink/{id}")]
        public async Task<IActionResult> DeleteLink(long id)
        {
            var repo = _fsql.GetRepository<Link>();

            repo.DbContextOptions.EnableCascadeSave = true;

            var link = await repo.Select.Where(p => p.Id == id).FirstAsync();


            var deleteIds = new List<long>
            {
                link.SourceId,
                link.TargetId
            };

            await _fsql.Delete<NodeConnectInfo>().Where(p => deleteIds.Contains(p.Id)).ExecuteAffrowsAsync();

            var ret = await repo.DeleteAsync(link);
            if (ret > 0)
                return Ok();
            return NotFound();
        }

        [HttpDelete("DeleteCard/{id}")]
        public async Task<IActionResult> DeleteCard(long id)
        {
            var repo = _fsql.GetRepository<Card>();

            repo.DbContextOptions.EnableCascadeSave = true;

            var card = await repo.Select.IncludeMany(p => p.Nodes).Where(p => p.Id == id).FirstAsync();

            var ret = await repo.DeleteAsync(card);
            if (ret > 0)
                return Ok();
            return NotFound();
        }

    }
}
