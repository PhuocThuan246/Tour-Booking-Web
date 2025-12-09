using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.WebUtilities;

namespace TourBookingWeb.Helpers
{
    public static class UrlHelperExtensions
    {
        public static string AppendQueryString(this IUrlHelper urlHelper, string basePath, IQueryCollection query)
        {
            if (query == null || !query.Any()) return basePath;

            var queryDict = query.ToDictionary(q => q.Key, q => q.Value.ToString());
            return QueryHelpers.AddQueryString(basePath, queryDict);
        }
    }
}
