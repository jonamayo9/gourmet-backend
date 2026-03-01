namespace GourmetApi.Security
{
    using System.Security.Claims;

    public static class ClaimsExtensions
    {
        public static int GetCompanyId(this ClaimsPrincipal user)
        {
            var value = user.FindFirst("companyId")?.Value;
            if (string.IsNullOrWhiteSpace(value))
                throw new Exception("companyId missing");

            return int.Parse(value);
        }
    }
}
