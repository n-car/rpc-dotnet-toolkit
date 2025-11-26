using RpcToolkit;
using RpcToolkit.AspNetCore;
using RpcToolkit.Logging;
using RpcToolkit.Middleware;
using RpcToolkit.Validation;
using RpcServer.Example.Services;

var builder = WebApplication.CreateBuilder(args);

// Configure RPC context with shared services
builder.Services.AddSingleton<IUserService, UserService>();
builder.Services.AddSingleton<ICalculatorService, CalculatorService>();

// Configure RPC Endpoint
builder.Services.AddRpcEndpoint(
    contextFactory: provider => new
    {
        UserService = provider.GetRequiredService<IUserService>(),
        CalculatorService = provider.GetRequiredService<ICalculatorService>()
    },
    options: new RpcOptions
    {
        EnableBatch = true,
        BatchOptions = new RpcBatchOptions
        {
            MaxSize = 50,
            Parallel = true,
            MaxParallelism = 4,
            ContinueOnError = false,
            CollectMetrics = true
        },
        LoggerOptions = new RpcLoggerOptions
        {
            Level = RpcLogLevel.Info,
            Format = RpcLogFormat.Text,
            IncludeTimestamp = true
        },
        EnableMiddleware = true,
        EnableIntrospection = true,  // Enable introspection methods
        IntrospectionPrefix = "__rpc"
    },
    configure: endpoint =>
    {
        // Register Calculator Methods with schemas
        endpoint.AddMethod<AddParams, int>("calculator.add", (Func<AddParams?, object?, int>)((p, ctx) =>
        {
            dynamic context = ctx!;
            return context.CalculatorService.Add(p!.A, p.B);
        }), new MethodConfig
        {
            Description = "Add two numbers",
            ExposeSchema = true,
            Schema = new
            {
                type = "object",
                properties = new
                {
                    A = new { type = "number" },
                    B = new { type = "number" }
                },
                required = new[] { "A", "B" }
            }
        });

        endpoint.AddMethod<SubtractParams, int>("calculator.subtract", (Func<SubtractParams?, object?, int>)((p, ctx) =>
        {
            dynamic context = ctx!;
            return context.CalculatorService.Subtract(p!.A, p.B);
        }), new MethodConfig
        {
            Description = "Subtract two numbers",
            ExposeSchema = true
        });

        endpoint.AddMethod<MultiplyParams, double>("calculator.multiply", (Func<MultiplyParams?, object?, double>)((p, ctx) =>
        {
            dynamic context = ctx!;
            return context.CalculatorService.Multiply(p!.A, p.B);
        }), new MethodConfig
        {
            Description = "Multiply two numbers",
            ExposeSchema = true
        });

        endpoint.AddMethod<DivideParams, double>("calculator.divide", (Func<DivideParams?, object?, double>)((p, ctx) =>
        {
            dynamic context = ctx!;
            return context.CalculatorService.Divide(p!.A, p.B);
        }), new MethodConfig
        {
            Description = "Divide two numbers",
            ExposeSchema = true
        });

        // Register User Methods with schemas
        endpoint.AddMethod<GetUserParams, UserDto>("user.get", (Func<GetUserParams?, object?, UserDto>)((p, ctx) =>
        {
            dynamic context = ctx!;
            var user = context.UserService.GetUser(p!.Id);
            if (user == null)
                throw new RpcToolkit.Exceptions.InvalidParamsException($"User {p.Id} not found");
            return user;
        }), new MethodConfig
        {
            Description = "Get user by ID",
            ExposeSchema = true
        });

        endpoint.AddMethod<CreateUserParams, UserDto>("user.create", (Func<CreateUserParams?, object?, UserDto>)((p, ctx) =>
        {
            dynamic context = ctx!;
            return context.UserService.CreateUser(p!.Name, p.Email);
        }), new MethodConfig
        {
            Description = "Create a new user",
            ExposeSchema = true
        });

        endpoint.AddMethod<object, UserDto[]>("user.list", (Func<object?, object?, UserDto[]>)((p, ctx) =>
        {
            dynamic context = ctx!;
            return context.UserService.ListUsers();
        }), new MethodConfig
        {
            Description = "List all users",
            ExposeSchema = true
        });

        // Utility Methods (not exposed in introspection)
        endpoint.AddMethod<object, string>("system.version", (Func<object?, object?, string>)((p, ctx) => "1.0.0"));
        endpoint.AddMethod<object, string>("system.ping", (Func<object?, object?, string>)((p, ctx) => "pong"));
        endpoint.AddMethod<object, DateTime>("system.time", (Func<object?, object?, DateTime>)((p, ctx) => DateTime.UtcNow));

        // Configure Middleware
        var middleware = endpoint.GetMiddleware();
        if (middleware != null)
        {
            // Add rate limiting (100 requests per minute)
            middleware.Add(
                new RateLimitMiddleware(100, 60, "ip"),
                "before"
            );

            // Add logging (requires ILogger from DI)
            // middleware.Add(new RpcLoggingMiddleware(logger), "before");
        }
    }
);

var app = builder.Build();

// Configure middleware pipeline
app.UseRpc(new RpcMiddlewareOptions
{
    Path = "/rpc",
    EnableCors = true,
    AllowedOrigins = new[] { "*" },
    AllowCredentials = false,
    IncludeExceptionDetails = app.Environment.IsDevelopment()
});

// Health check endpoint
app.MapGet("/health", () => new { status = "healthy", timestamp = DateTime.UtcNow });

// Info endpoint
app.MapGet("/", () => Results.Json(new
{
    service = "RPC Toolkit Example Server",
    version = "1.0.0",
    endpoint = "/rpc",
    introspection = new
    {
        enabled = true,
        methods = new[]
        {
            "__rpc.listMethods",
            "__rpc.describe",
            "__rpc.describeAll",
            "__rpc.version",
            "__rpc.capabilities"
        }
    },
    methods = new[]
    {
        "calculator.add",
        "calculator.subtract",
        "calculator.multiply",
        "calculator.divide",
        "user.get",
        "user.create",
        "user.list",
        "system.version",
        "system.ping",
        "system.time"
    }
}));

app.Run();

// DTO Classes
public record AddParams(int A, int B);
public record SubtractParams(int A, int B);
public record MultiplyParams(double A, double B);
public record DivideParams(double A, double B);
public record GetUserParams(int Id);
public record CreateUserParams(string Name, string Email);
