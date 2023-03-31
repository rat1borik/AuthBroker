using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net;

public class HostModel : PageModel {
    private readonly IHttpContextAccessor _httpContextAccssor;

    public HostModel(IHttpContextAccessor httpContextAccssor) {
        _httpContextAccssor = httpContextAccssor;
    }

    public string UserAgent { get; set; }
    public IPAddress IPAddress { get; set; }

    // The following links may be useful for getting the IP Adress:
    // https://stackoverflow.com/questions/35441521/remoteipaddress-is-always-null
    // https://stackoverflow.com/questions/28664686/how-do-i-get-client-ip-address-in-asp-net-core

    public void OnGet() {
        UserAgent = _httpContextAccssor.HttpContext.Request.Headers["User-Agent"];
        // Note that the RemoteIpAddress property returns an IPAdrress object 
        // which you can query to get required information. Here, however, we pass 
        // its string representation
        //IPAddress = _httpContextAccssor.HttpContext.Connection.RemoteIpAddress;
    }
}