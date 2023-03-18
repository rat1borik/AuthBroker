using AuthBroker.Pages.Admin;
using Bogus.DataSets;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32.SafeHandles;
using System.ComponentModel.DataAnnotations;
using System.Data.Common;
using System.Reflection.Metadata;
using System.Text.Json.Nodes;
using System.Linq;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore.Update.Internal;
using Microsoft.EntityFrameworkCore.Update;

namespace AuthBroker.Models;

public class AppDbContext : DbContext {
	public DbSet<User> Users { get; set; }

	public DbSet<Session> Sessions { get; set; }

	public DbSet<AccessToken> AccessTokens { get; set; }

	public DbSet<AppClient> AppClients { get; set; }

	public DbSet<Scope> Scopes { get; set; }

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

		modelBuilder.Entity<Scope>().HasIndex(i => i.Name).IsUnique();
        modelBuilder.Entity<Scope>().HasCheckConstraint("name_cntsr","length(name) > 5");

        modelBuilder.Entity<AppClient>().HasIndex(i => i.Name).IsUnique();
        modelBuilder.Entity<AppClient>().HasCheckConstraint("name_cntsr", "length(name) > 5");
        modelBuilder.Entity<AppClient>().HasIndex(i => i.Id).IsUnique();

	}
}

public abstract class Store<T> where T : class {
    protected DbSet<T> _store;
    protected AppDbContext _cx;

    public Store(AppDbContext cx) { _cx = cx; }

    public virtual async Task<List<T>?> GetListAsync() {
        return await _store.ToListAsync();
    }

    public virtual async Task AddAsync(T item) {
        await _store.AddAsync(item);
        await _cx.SaveChangesAsync();
    }

    public virtual async Task RemoveAsync(T ac) {
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


public class ScopeStore : Store<Scope> {

    public ScopeStore(AppDbContext cx) : base(cx) {
        _store = cx.Scopes;
    }

    public async Task<Scope?> GetScopeAsync(Guid scId) {
        return await _store.Where(scp => scp.Id == scId).FirstOrDefaultAsync();
    }
}
public class AppClientStore : Store<AppClient> {

    public AppClientStore(AppDbContext cx) : base(cx) {
        _store = cx.AppClients;
    }

    public async Task<AppClient?> GetAppClientAsync(string appId) {
        return await _store.Where(app => app.Id == appId).FirstOrDefaultAsync();
    }

	public override async Task RemoveAsync(AppClient ac) {
		_store.Remove(ac);
        _cx.Sessions.RemoveRange(_cx.Sessions.Where(sess => sess.App == ac));
		await _cx.SaveChangesAsync();
	}
}

public class SessionStore : Store<Session> {

	public SessionStore(AppDbContext cx) : base(cx) {
		_store = cx.Sessions;
	}

    public async Task<Session?> GetSessionAsync(string appId, string usr) {
        //var q = from session in _store.Include(app => app.Scopes)
        //        where session.User.Login == usr && session.App.Id == appId
        //        select session;
        //return await q.FirstAsync();

        return await _store.Include(c => c.Scopes).Include(c => c.App).Include(c => c.User).Where(row => row.User.Login == usr && row.App.Id == appId).FirstOrDefaultAsync();

	}

    public async Task<List<Session>?> GetSessions(string usr) {
        //var q = from session in _store.Include(app => app.Scopes)
        //        where session.User.Login == usr && session.App.Id == appId
        //        select session;
        //return await q.FirstAsync();

        return await _store.Include(c => c.Scopes).Include(c => c.App).Include(c => c.User).Where(row => row.User.Login == usr).ToListAsync();

    }

    public override async Task<List<Session>?> GetListAsync() {
        //var q = from session in _store.Include(app => app.Scopes)
        //        where session.User.Login == usr && session.App.Id == appId
        //        select session;
        //return await q.FirstAsync();

        return await _store.Include(c => c.Scopes).Include(c => c.App).Include(c => c.User).ToListAsync();

    }

    public async Task<Session?> GetSession(Guid sessId) {
		//var q = from session in _store.Include(app => app.Scopes)
		//        where session.User.Login == usr && session.App.Id == appId
		//        select session;
		//return await q.FirstAsync();

		return await _store.Include(c => c.Scopes).Include(c => c.App).Include(c => c.User).Where(row => row.Id == sessId).FirstOrDefaultAsync();

	}

	public async Task<Session?> GetSessionByCode(string code) {
		//var q = from session in _store.Include(app => app.Scopes)
		//        where session.User.Login == usr && session.App.Id == appId
		//        select session;
		//return await q.FirstAsync();

		return await _store.Include(c => c.Scopes).Include(c => c.App).Include(c => c.User).Where(row => row.Code == code).FirstOrDefaultAsync();

	}

	public async Task<Session?> CreateSession(string usr, string appId, Guid[] scopes) {
        var _app = (from app in _cx.AppClients
                   where app.Id == appId
                   select app).FirstOrDefault();
        var _usr = (from user in _cx.Users
                    where user.Login == usr
                    select user).FirstOrDefault();

		if (_app != null && _usr != null) {
            var sess = new Session() {
                App = _app,
                User = _usr,
                Code = RandomTokenGenerator.CreateKey(),
                Scopes = (from scope in _cx.Scopes
                          where scopes.Contains(scope.Id)
                          select scope).ToList()
			};
            _store.Add(sess);
            await _cx.SaveChangesAsync();
            return sess;
        }
        return null;
	}

    public async Task UpdateKey(Session sess) {
        if (sess != null) {
            sess.Code = RandomTokenGenerator.CreateKey();
            await _cx.SaveChangesAsync();
        }
    }

   
}

public class AccessTokenStore : Store<AccessToken> {

	public AccessTokenStore(AppDbContext cx) : base(cx) {
		_store = cx.AccessTokens;
	}
}

public class RandomTokenGenerator {
	public static string CreateKey() {
		var key = new byte[32];
		var rng = RandomNumberGenerator.Create();
		rng.GetBytes(key);
		return Convert.ToBase64String(key);
	}
}

public static class Base64Ex {
	public static string Base64UrlEncode(this string value) {
		return value.Replace('+', '.').Replace('/', '_').Replace('=', '-');
	}
	public static string Base64UrlDecode(this string value) {
		return value.Replace('.', '+').Replace('_', '/').Replace('-', '=');
	}
}