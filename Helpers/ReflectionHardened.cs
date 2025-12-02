using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace RefitDynamicApi.Helpers
{
    internal class Helper_Backup
    {
        private static readonly JsonSerializerOptions SafeJsonOptions = new()
        {
            MaxDepth = 32,                     // JSON bomb / excessive nesting koruması
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip
        };

        private static readonly HashSet<Type> AllowedSimpleTypes =
        [
            typeof(string),
    typeof(int), typeof(long),
    typeof(decimal),
    typeof(bool),
    typeof(DateTime),
    typeof(Guid)
        ];

        /// <summary>
        /// Parametreleri güvenli şekilde resolve eder.
        /// </summary>
        private static async Task<object[]> GetMethodParamsAsync(MethodInfo method, HttpContext ctx)
        {
            var parameters = method.GetParameters();
            var args = new object[parameters.Length];

            //----------------------------------------------------------------------
            // 0) Güvenli body param filtreleme  (interface, delegate, unsafe class hariç)
            //----------------------------------------------------------------------
            var bodyParam = parameters.FirstOrDefault(p =>
                IsSafeBodyType(p.ParameterType)
            );

            foreach (var (p, index) in parameters.Select((p, i) => (p, i)))
            {
                var type = p.ParameterType;

                //----------------------------------------------------------------------
                // 1) BODY PARAM
                //----------------------------------------------------------------------
                if (bodyParam != null && p == bodyParam)
                {
                    args[index] = await BindJsonBodyAsync(ctx, type);
                    continue;
                }

                //----------------------------------------------------------------------
                // 2) QUERY PARAM
                //----------------------------------------------------------------------
                if (ctx.Request.Query.TryGetValue(p.Name!, out var qv))
                {
                    args[index] = SafeConvert(qv.ToString(), type);
                    continue;
                }

                //----------------------------------------------------------------------
                // 3) ROUTE VALUE
                //----------------------------------------------------------------------
                if (ctx.Request.RouteValues.TryGetValue(p.Name!, out var rv))
                {
                    args[index] = SafeConvert(rv, type);
                    continue;
                }

                //----------------------------------------------------------------------
                // 4) DEFAULT VALUE
                //----------------------------------------------------------------------
                args[index] = CreateDefault(type);
            }

            return args;
        }

        #region Helpers

        // BODY parametresine sadece class + array + DTO olarak kullanılabilir türler girer
        private static bool IsSafeBodyType(Type t)
        {
            // string / value type body olamaz
            if (t == typeof(string) || t.IsValueType)
                return false;

            // array allowed
            if (t.IsArray)
                return true;

            // class ama abstract, interface ve delegate değilse kabul et
            if (t.IsClass &&
                !t.IsAbstract &&
                !typeof(Delegate).IsAssignableFrom(t) &&
                !t.IsInterface)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// JSON Body güvenli şekilde okunur (max size, max depth, geçersiz JSON block)
        /// </summary>
        private static async Task<object> BindJsonBodyAsync(HttpContext ctx, Type type)
        {
            // Content check
            if (ctx.Request.ContentLength is null || ctx.Request.ContentLength == 0)
                return null;

            if (!ctx.Request.ContentType?.Contains("application/json") ?? true)
                return null;

            // Max body size control (örnek 1MB)
            if (ctx.Request.ContentLength > 1024 * 1024)
                throw new BadHttpRequestException("JSON body too large (max 1MB).");

            try
            {
                return await JsonSerializer.DeserializeAsync(ctx.Request.Body, type, SafeJsonOptions)
                       ?? Activator.CreateInstance(type);
            }
            catch (JsonException)
            {
                throw new BadHttpRequestException("Invalid JSON payload.");
            }
        }

        private static object SafeConvert(object input, Type targetType)
        {
            if (input == null)
                return null;

            var t = Nullable.GetUnderlyingType(targetType) ?? targetType;

            try
            {
                if (t.IsEnum)
                    return Enum.Parse(t, input.ToString()!, ignoreCase: true);

                if (AllowedSimpleTypes.Contains(t))
                    return Convert.ChangeType(input, t);

                throw new ArgumentException($"Type {t.Name} is not allowed for simple conversion.");
            }
            catch
            {
                throw new ArgumentException($"Cannot convert '{input}' to type '{t.Name}'");
            }
        }

        private static object CreateDefault(Type type)
        {
            if (type.IsValueType)
                return Activator.CreateInstance(type);

            return null;
        }

        #endregion

    }
}
