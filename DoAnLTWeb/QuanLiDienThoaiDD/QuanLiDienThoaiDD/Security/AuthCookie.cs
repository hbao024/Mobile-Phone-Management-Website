using System;
using System.Security.Principal;
using System.Web;
using System.Web.Security;

namespace QuanLiDienThoaiDD.Security
{
    public static class AuthCookie
    {
        public static void SignIn(string userName, string role, bool isPersistent)
        {
            var now = DateTime.Now;
            var ticket = new FormsAuthenticationTicket(
                1,
                userName,
                now,
                now.AddMinutes(isPersistent ? 43200 : 60),
                isPersistent,
                role ?? string.Empty,
                FormsAuthentication.FormsCookiePath);

            var encryptedTicket = FormsAuthentication.Encrypt(ticket);
            var cookie = new HttpCookie(FormsAuthentication.FormsCookieName, encryptedTicket)
            {
                HttpOnly = true,
                Secure = HttpContext.Current.Request.IsSecureConnection
            };

            HttpContext.Current.Response.Cookies.Add(cookie);
        }

        public static void SignOut()
        {
            FormsAuthentication.SignOut();
        }

        public static void RestorePrincipal(HttpContext context)
        {
            var cookie = context.Request.Cookies[FormsAuthentication.FormsCookieName];
            if (cookie == null || string.IsNullOrWhiteSpace(cookie.Value))
            {
                return;
            }

            try
            {
                var ticket = FormsAuthentication.Decrypt(cookie.Value);
                if (ticket == null || ticket.Expired)
                {
                    return;
                }

                var identity = new FormsIdentity(ticket);
                var roles = string.IsNullOrWhiteSpace(ticket.UserData)
                    ? new string[0]
                    : new[] { ticket.UserData };

                context.User = new GenericPrincipal(identity, roles);
            }
            catch (Exception)
            {
                FormsAuthentication.SignOut();
            }
        }
    }
}
