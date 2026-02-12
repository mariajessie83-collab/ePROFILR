namespace Server.Data
{
    public class Dbconnections
    {
        public IConfiguration Configuration { get; }
        public String GetConnection() => Configuration.GetSection("ConnectionStrings").GetSection("dbconstring").Value;
        public Dbconnections(IConfiguration configuration)
        {
            Configuration = configuration;
        }
    }
}
