using System.Security.Cryptography;
using System.Text;
using System.Security.Claims;
using AgroConecta.Api.Data;
using AgroConecta.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AgroConecta.Api.Middlewares;

public class IdempotencyMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, AgroConectaDbContext db)
    {
        if (!HttpMethods.IsPost(context.Request.Method) || !context.Request.Path.StartsWithSegments("/api")
            || context.Request.Path.StartsWithSegments("/api/auth")) { await next(context); return; }
        var key = context.Request.Headers["Idempotency-Key"].ToString();
        if (string.IsNullOrWhiteSpace(key) || key.Length > 128)
        {
            await Results.Problem(statusCode: 400, title: "Informe Idempotency-Key com 1 a 128 caracteres.").ExecuteAsync(context); return;
        }
        context.Request.EnableBuffering();
        var body = await new StreamReader(context.Request.Body, leaveOpen: true).ReadToEndAsync();
        context.Request.Body.Position = 0;
        static string Hash(string text) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)));
        var scopedKey = Hash($"{context.User.FindFirstValue(ClaimTypes.NameIdentifier)}|{context.Request.Path.Value!.TrimEnd('/').ToLowerInvariant()}|{key}");
        var requestHash = Hash(context.Request.Method + context.Request.QueryString + body);
        await using var tx = await db.Database.BeginTransactionAsync(context.RequestAborted);
        if (db.Database.IsNpgsql())
        {
            // Transaction-scoped lock also serializes concurrent requests across API replicas.
            var lockId = BitConverter.ToInt64(Convert.FromHexString(scopedKey), 0);
            await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock({lockId})", context.RequestAborted);
        }
        var saved = await db.IdempotencyRecords.AsNoTracking().SingleOrDefaultAsync(x => x.Key == scopedKey);
        if (saved is not null)
        {
            if (saved.RequestHash != requestHash)
                await Results.Problem(statusCode: 409, title: "Chave já utilizada com outro conteúdo.").ExecuteAsync(context);
            else
            {
                context.Response.StatusCode = saved.StatusCode; context.Response.ContentType = saved.ContentType;
                if (saved.Location is not null) context.Response.Headers.Location = saved.Location;
                context.Response.Headers["Idempotency-Replayed"] = "true";
                await context.Response.WriteAsync(saved.Body);
            }
            return;
        }
        var original = context.Response.Body;
        await using var buffer = new MemoryStream();
        context.Response.Body = buffer;
        try
        {
            await next(context);
            buffer.Position = 0;
            var responseBody = await new StreamReader(buffer, leaveOpen: true).ReadToEndAsync();
            if (context.Response.StatusCode is >= 200 and < 300)
            {
                db.IdempotencyRecords.Add(new IdempotencyRecord { Key = scopedKey, RequestHash = requestHash,
                    StatusCode = context.Response.StatusCode, ContentType = context.Response.ContentType ?? "application/json",
                    Body = responseBody, Location = context.Response.Headers.Location.FirstOrDefault() });
                await db.SaveChangesAsync(context.RequestAborted);
                await tx.CommitAsync(context.RequestAborted);
            }
            else await tx.RollbackAsync(context.RequestAborted);
            buffer.Position = 0; context.Response.Body = original;
            await buffer.CopyToAsync(original, context.RequestAborted);
        }
        finally { context.Response.Body = original; }
    }
}
