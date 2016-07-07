using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telerik.OpenAccess;
using Telerik.OpenAccess.Metadata;
using Telerik.OpenAccess.Metadata.Fluent;

namespace TelerikTransactionsTest
{
    class Program
    {
        static void Main(string[] args)
        {
            Project project = null;
            using (var context = UpdateDatabase())
            {

                Database d = Database.Get("defaultConnection");
                using (IObjectScope scope = d.GetObjectScope())
                {
                    scope.Transaction.Begin();
                    context.Events.ObjectConstructed += Events_ObjectConstructed;
                    context.Events.Changed += Events_Changed;
                    context.Events.Added += Events_Added;

                    project = context.GetAll<Project>().FirstOrDefault();
                    project.Name = "Vision";
                    scope.Transaction.Rollback();
                }

                var name = project.Name; 

            }
        }

        private static void Events_Added(object sender, AddEventArgs e)
        {
        }

        private static void Events_Changed(object sender, ChangeEventArgs e)
        {
        }

        private static void Events_ObjectConstructed(object sender, ObjectConstructedEventArgs e)
        {
        }

        private static DbContext UpdateDatabase()
        {
            var context = new DbContext();
            var schemaHandler = context.GetSchemaHandler();
            EnsureDB(schemaHandler);
            return context;
        }

        private static void EnsureDB(ISchemaHandler schemaHandler)
        {
            string script = null;
            if (schemaHandler.DatabaseExists())
            {
                script = schemaHandler.CreateUpdateDDLScript(null);
            }
            else
            {
                schemaHandler.CreateDatabase();
                script = schemaHandler.CreateDDLScript();
            }

            if (!string.IsNullOrEmpty(script))
            {
                schemaHandler.ExecuteDDLScript(script);
            }
        }
    }


    public partial class DbContext : OpenAccessContext
    {
        private static string connectionStringName = @"defaultConnection";

        private static BackendConfiguration backend =
            GetBackendConfiguration();

        private static MetadataSource metadataSource =
            new FluentModelMetadataSource();

        public DbContext()
            : base(connectionStringName, backend, metadataSource)
        { }

        public IQueryable<Project> Projects
        {
            get
            {
                return this.GetAll<Project>();
            }
        }

        public IQueryable<Sprint> Sprints
        {
            get
            {
                return GetAll<Sprint>();
            }
        }

        public static BackendConfiguration GetBackendConfiguration()
        {
            BackendConfiguration backend = new BackendConfiguration();
            backend.Backend = "MsSql";
            backend.ProviderName = "System.Data.SqlClient";
            return backend;
        }
    }

    public partial class FluentModelMetadataSource : FluentMetadataSource
    {
        protected override IList<MappingConfiguration> PrepareMapping()
        {
            List<MappingConfiguration> configurations =
                new List<MappingConfiguration>();

            var projectMapping = new MappingConfiguration<Project>();
            projectMapping.MapType(p => new
            {
                Id = p.Id,
                Name = p.Name
            }).ToTable("Projects");
            projectMapping.HasProperty(c => c.Id).IsIdentity();

            var sprintMapping = new MappingConfiguration<Sprint>();
            sprintMapping.MapType(b => new
            {
                Id = b.Id,
                Name = b.Name,
                ProjecId = b.ProjectId
            }).ToTable("Sprint");
            sprintMapping.HasProperty(b => b.Id).IsIdentity();

            sprintMapping.HasAssociation(s => s.Project).WithOpposite(p => p.Sprints).HasConstraint((s, p) => s.ProjectId == p.Id);
            configurations.Add(sprintMapping);
            configurations.Add(projectMapping);

            return configurations;
        }
    }


    public class Project
    {
        public Project()
        {
            Sprints = new List<Sprint>();
        }
        public Guid Id { get; set; }
        public string Name { get; set; }

        public IList<Sprint> Sprints { get; set; } // new        

    }

    public class Sprint
    {

        public Guid Id { get; set; }
        public string Name { get; set; }

        public Guid ProjectId { get; set; }
        public Project Project { get; set; }

    }
}
