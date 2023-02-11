using AuthBroker.Pages.Admin;
using Bogus.DataSets;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32.SafeHandles;
using System.ComponentModel.DataAnnotations;
using System.Data.Common;
using System.Reflection.Metadata;
using System.Text.Json.Nodes;

namespace AuthBroker.Model;

public class AppDbContext : DbContext {
	public DbSet<User> Users { get; set; }

	public DbSet<AppGrants> AppGrants { get; set; }

	public DbSet<AppClient> AppClients { get; set; }

	public DbSet<Grant> Grants { get; set; }

	public AppDbContext(DbContextOptions<AppDbContext> options)
		: base(options) {
	}

	protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
	=> optionsBuilder
		.UseSnakeCaseNamingConvention();

	protected override void OnModelCreating(ModelBuilder modelBuilder) {
		modelBuilder.HasPostgresExtension("uuid-ossp");

        modelBuilder.Entity<User>().HasIndex(i => i.Login).IsUnique();
        modelBuilder.Entity<User>().HasCheckConstraint("name_cntsr", "length(login) > 5");

		modelBuilder.Entity<Grant>().HasIndex(i => i.Name).IsUnique();
        modelBuilder.Entity<Grant>().HasCheckConstraint("name_cntsr","length(name) > 5");

        modelBuilder.Entity<AppClient>().HasIndex(i => i.Name).IsUnique();
        modelBuilder.Entity<AppClient>().HasCheckConstraint("name_cntsr", "length(name) > 5");
        modelBuilder.Entity<AppClient>().HasIndex(i => i.AppId).IsUnique();
    }
}
//public class GrantStore {
//	private AppDbContext cx;

//	public GrantStore(AppDbContext _cx) {
//		cx = _cx;
//	}

//	public async Task<List<Grant>> GetListAsync() {
//		return await cx.Grants.ToListAsync();
//	}

//    public async Task AddAsync(string name) {
//        await cx.Grants.AddAsync(new Grant() { Name = name });

//		await cx.SaveChangesAsync();
//    }

//    public async Task RemoveAsync(Grant gr) {
//        cx.Grants.Remove(gr);
//        await cx.SaveChangesAsync();
//    }
//}

//public class UserAccStore {
//	private AppDbContext cx;

//	public UserAccStore(AppDbContext _cx) {
//		cx = _cx;

//	}

//	public async Task<List<User>> GetListAsync() {
//		return await cx.Users.ToListAsync();
//	}

//	public async Task<User?> GetByLogin(string login) {
//		return await cx.Users.Where(usr=>usr.Login == login).FirstOrDefaultAsync();
//	}

//	public async Task AddAsync(string login) {
//		await cx.Users.AddAsync(new User() { Login = login });

//		await cx.SaveChangesAsync();
//	}

//	public async Task RemoveAsync(User gr) {
//		cx.Users.Remove(gr);
//		await cx.SaveChangesAsync();
//	}
//}

//public class AppClientStore {
//    private AppDbContext cx;

//    public AppClientStore(AppDbContext _cx) {
//        cx = _cx;
//    }

//    public async Task<List<AppClient>> GetListAsync() {
//        return await cx.AppClients.ToListAsync();
//    }

//    public async Task AddAsync(string name) {
//        await cx.AppClients.AddAsync(new AppClient() { Name = name });

//        await cx.SaveChangesAsync();
//    }

//    public async Task RemoveAsync(AppClient ac) {
//        cx.AppClients.Remove(ac);
//        await cx.SaveChangesAsync();
//    }
//}

public abstract class Store<T> where T : class {
    protected DbSet<T> _store;
    protected AppDbContext _cx;

    public Store(AppDbContext cx) { _cx = cx; }

    public async Task<List<T>> GetListAsync() {
        return await _store.ToListAsync();
    }

    public async Task AddAsync(T item) {
        await _store.AddAsync(item);
        await _cx.SaveChangesAsync();
    }

    public async Task RemoveAsync(T ac) {
        _store.Remove(ac);
        await _cx.SaveChangesAsync();
    }
}

public class UserAccStore : Store<User> {
    
    public UserAccStore(AppDbContext cx) : base(cx) {
        _store = cx.Users;
    }

    public async Task<User?> GetByLogin(string login) {
        return await _cx.Users.Where(usr => usr.Login == login).FirstOrDefaultAsync();
    }
}

    public class GrantStore : Store<Grant> {

    public GrantStore(AppDbContext cx) : base(cx) {
        _store = cx.Grants;
    }
}
public class AppClientStore : Store<AppClient> {

    public AppClientStore(AppDbContext cx) : base(cx) {
        _store = cx.AppClients;
    }
}