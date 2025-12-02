using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Refit;
using RefitDynamicApi.Helpers;
using RefitDynamicApi.Attributes;
using RefitDynamicApi.Interfaces;

namespace RefitDynamicApi.Extensions
{
    public static class ApiProxyExtensions
    {
        public static void MapAllRefitClientToDynamicApi(this IEndpointRouteBuilder app, string baseRoute, Assembly assembly)
        {
            var baseInterfaceType = typeof(IRefitClient);

            var clientInterfaces = assembly.GetTypes().Where(t =>
                t.IsInterface &&
                t != baseInterfaceType &&
                baseInterfaceType.IsAssignableFrom(t)
            ).ToList();

            if (!clientInterfaces.Any())
                throw new Exception($"Make sure your Refit interfaces is Assignable from {baseInterfaceType.Name}");

            foreach (var item in clientInterfaces)
            {
                app.MapDynamicApi(item, baseRoute);
            }
        }

        public static void MapDynamicApi<T>(this IEndpointRouteBuilder app, string baseRoute)
        {
            var type = typeof(T);
            var controllerName = ReflectionHelper.GetControllerName(type);
            var methods = type.GetMethods();

            foreach (var method in methods)
            {
                // Eğer method DisableMethod attribute içeriyorsa atla
                if (method.GetCustomAttribute<DisableMethodAttribute>() != null)
                    continue;

                var httpAttr = method.GetCustomAttributes()
                    .FirstOrDefault(a => a is GetAttribute || a is PostAttribute);

                if (httpAttr is null)
                    continue; // attribute yoksa atla

                var route = $"{baseRoute}/{controllerName}/{method.Name}";

                // GET
                if (httpAttr is GetAttribute)
                {
                    app.MapGet(route, async (T client, HttpContext ctx) =>
                    {
                        var result = await ReflectionHelper.InvokeMethodAsync(client, method, ctx);
                        return Results.Ok(result);
                    });
                }

                // POST
                else if (httpAttr is PostAttribute)
                {
                    app.MapPost(route, async (T client, HttpContext ctx) =>
                    {
                        var result = await ReflectionHelper.InvokeMethodAsync(client, method, ctx);
                        return Results.Ok(result);
                    });
                }
            }
        }

        public static void MapDynamicApi(this IEndpointRouteBuilder app, Type type, string baseRoute)
        {
            var methods = type.GetMethods();
            var controllerName = ReflectionHelper.GetControllerName(type);

            foreach (var method in methods)
            {
                // Eğer method DisableMethod attribute içeriyorsa atla
                if (method.GetCustomAttribute<DisableMethodAttribute>() != null)
                    continue;

                var httpAttr = method.GetCustomAttributes()
                    .FirstOrDefault(a => a is GetAttribute || a is PostAttribute);

                if (httpAttr is null)
                    continue;

                                //var parameters = method.GetParameters();
                //bool hasClassOrArrayParam = parameters.Any(p =>
                //    (p.ParameterType.IsClass && p.ParameterType != typeof(string))
                //    || p.ParameterType.IsArray
                //);

                var route = $"{baseRoute}/{controllerName}/{method.Name}";

                if (httpAttr is GetAttribute)
                {
                    app.MapGet(route, async (HttpContext ctx) =>
                    {
                        var client = ctx.RequestServices.GetRequiredService(type);
                        var result = await ReflectionHelper.InvokeMethodAsync(client, method, ctx);
                        return Results.Ok(result);
                    });
                }
                else if (httpAttr is PostAttribute)
                {
                    app.MapPost(route, async (HttpContext ctx) =>
                    {
                        var client = ctx.RequestServices.GetRequiredService(type);
                        var result = await ReflectionHelper.InvokeMethodAsync(client, method, ctx);
                        return Results.Ok(result);
                    });
                }
            }
        }

    }

}
