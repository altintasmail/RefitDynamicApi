using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Refit;

namespace RefitDynamicApi.Helpers
{
    internal class ReflectionHelper
    {
            public static async Task<object> InvokeMethodAsync(object client, MethodInfo method, HttpContext ctx)
            {
                var args = await GetMethodParamsAsync(method, ctx);

                var returnValue = method.Invoke(client, args);

                if (returnValue is null)
                    return null;

                // ----------------------
                // METHOD RETURN TYPE:
                // - Task
                // - Task<T>
                // - sync T
                // ----------------------

                // Task (void)
                if (returnValue is Task taskNonGeneric && method.ReturnType == typeof(Task))
                {
                    await taskNonGeneric;
                    return null;
                }

                // Task<T>
                if (returnValue is Task task)
                {
                    await task;

                    var resultProp = task.GetType().GetProperty("Result");
                    return resultProp?.GetValue(task);
                }

                // Synchronous method (rare case)
                return returnValue;
            }

            private static async Task<object[]> GetMethodParamsAsync(MethodInfo method, HttpContext ctx)
            {
                var parameters = method.GetParameters();
                var args = new object[parameters.Length];

                // Body parametresi varsa
                var bodyParam = parameters.FirstOrDefault(p => p.GetCustomAttribute<BodyAttribute>() != null);

                foreach (var (p, index) in parameters.Select((p, i) => (p, i)))
                {
                    var type = p.ParameterType;

                    // -------------------
                    // Body param
                    // -------------------
                    if (bodyParam != null && p == bodyParam)
                    {
                        //[Body] attribütüne indirgendi.
                        if (ctx.Request.ContentLength > 0 &&
                            ctx.Request.ContentType?.Contains("application/json") == true)
                        {
                            args[index] = await ctx.Request.ReadFromJsonAsync(type);
                        }
                        else
                        {
                            args[index] = null; // body boşsa null
                        }
                        continue;
                    }

                    // -------------------
                    // Query param
                    // -------------------
                    if (ctx.Request.Query.TryGetValue(p.Name!, out var qv))
                    {
                        args[index] = TryConvert(qv.ToString(), type);
                        continue;
                    }

                    // -------------------
                    // Route value
                    // -------------------
                    if (ctx.Request.RouteValues.TryGetValue(p.Name!, out var rv))
                    {
                        args[index] = TryConvert(rv, type);
                        continue;
                    }

                    // ----------------------
                    // 4. Default / Required check
                    // ----------------------
                    if (type.IsValueType && Nullable.GetUnderlyingType(type) == null)
                    {
                        // Nullable olmayan value type → default value
                        args[index] = Activator.CreateInstance(type);
                    }
                    else
                    {
                        args[index] = null;
                    }
                }

                // ----------------------
                // 5. Required parameter check (şimdilik kapalı)
                // ----------------------
                //for (int i = 0; i < parameters.Length; i++)
                //{
                //    if (args[i] == null && parameters[i].ParameterType.IsValueType && Nullable.GetUnderlyingType(parameters[i].ParameterType) == null)
                //    {
                //        throw new ArgumentException($"Required parameter '{parameters[i].Name}' of type '{parameters[i].ParameterType.Name}' is missing.");
                //    }
                //}

                return args;
            }

            private static object ConvertSimple(object input, Type targetType)
            {
                var t = Nullable.GetUnderlyingType(targetType) ?? targetType;

                if (input == null)
                    return null;

                return Convert.ChangeType(input, t);
            }

            // Güvenli conversion
            private static object TryConvert(object input, Type targetType)
            {
                if (input == null)
                    return null;

                var t = Nullable.GetUnderlyingType(targetType) ?? targetType;

                try
                {
                    if (t.IsEnum)
                        return Enum.Parse(t, input.ToString()!, true);

                    if (t == typeof(Guid))
                        return Guid.Parse(input.ToString()!);

                    if (t == typeof(DateTime))
                        return DateTime.Parse(input.ToString()!);

                    return Convert.ChangeType(input, t);
                }
                catch
                {
                    throw new ArgumentException($"Cannot convert value '{input}' to type '{t.Name}'");
                }
            }

            public static string GetControllerName(Type type)
            {
                var name = type.Name;

                name = name.TrimStart('I'); // IKulupClient -> KulupClient

                // "Client", "Service", "Api" gibi son ekleri kaldır
                var suffixes = new[] { "Client", "Service", "Api" };
                foreach (var s in suffixes)
                    if (name.EndsWith(s))
                        name = name.Substring(0, name.Length - s.Length);

                return name;
            }
    }
}