namespace SDLearnerSVCs.Data;

using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using SDLearnerSVCs.Models;

public class AppDbContext : DbContext
{


    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {

    }
    public DbSet<VideoMetadata> Videos { get; set; }


}