# RPC Toolkit Server Example

Complete working example of a JSON-RPC 2.0 server using ASP.NET Core and RPC Toolkit.

## Features

- ✅ **Calculator Service** - Basic arithmetic operations (add, subtract, multiply, divide)
- ✅ **User Management** - CRUD operations for users
- ✅ **Middleware** - Rate limiting (100 req/min)
- ✅ **Batch Requests** - Process multiple RPC calls in a single HTTP request
- ✅ **CORS Support** - Cross-origin requests enabled
- ✅ **Error Handling** - Proper JSON-RPC 2.0 error responses

## Running the Server

```bash
cd examples/RpcServer.Example
dotnet run
```

Server will start on `http://localhost:5000`

## Testing with cURL

### Single Request - Ping

```bash
curl -X POST http://localhost:5000/rpc \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "method": "system.ping",
    "params": {},
    "id": 1
  }'
```

Response:
```json
{"jsonrpc":"2.0","result":"pong","id":1}
```

### Calculator - Add

```bash
curl -X POST http://localhost:5000/rpc \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "method": "calculator.add",
    "params": {"a": 5, "b": 3},
    "id": 2
  }'
```

Response:
```json
{"jsonrpc":"2.0","result":8,"id":2}
```

### Calculator - Divide

```bash
curl -X POST http://localhost:5000/rpc \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "method": "calculator.divide",
    "params": {"a": 10.5, "b": 2.5},
    "id": 3
  }'
```

Response:
```json
{"jsonrpc":"2.0","result":4.2,"id":3}
```

### User - Get by ID

```bash
curl -X POST http://localhost:5000/rpc \
  -H "Content-Type": "application/json" \
  -d '{
    "jsonrpc": "2.0",
    "method": "user.get",
    "params": {"id": 1},
    "id": 4
  }'
```

Response:
```json
{
  "jsonrpc":"2.0",
  "result":{
    "Id":1,
    "Name":"John Doe",
    "Email":"john@example.com",
    "CreatedAt":"2025-11-26T10:30:00Z"
  },
  "id":4
}
```

### User - List All

```bash
curl -X POST http://localhost:5000/rpc \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "method": "user.list",
    "params": {},
    "id": 5
  }'
```

Response:
```json
{
  "jsonrpc":"2.0",
  "result":[
    {"Id":1,"Name":"John Doe","Email":"john@example.com","CreatedAt":"2025-11-26T10:30:00Z"},
    {"Id":2,"Name":"Jane Smith","Email":"jane@example.com","CreatedAt":"2025-11-26T10:30:01Z"}
  ],
  "id":5
}
```

### User - Create

```bash
curl -X POST http://localhost:5000/rpc \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "method": "user.create",
    "params": {"name": "Bob Wilson", "email": "bob@example.com"},
    "id": 6
  }'
```

Response:
```json
{
  "jsonrpc":"2.0",
  "result":{
    "Id":3,
    "Name":"Bob Wilson",
    "Email":"bob@example.com",
    "CreatedAt":"2025-11-26T10:35:00Z"
  },
  "id":6
}
```

### Batch Request

```bash
curl -X POST http://localhost:5000/rpc \
  -H "Content-Type: application/json" \
  -d '[
    {"jsonrpc":"2.0","method":"calculator.add","params":{"a":1,"b":2},"id":"calc1"},
    {"jsonrpc":"2.0","method":"calculator.multiply","params":{"a":3,"b":4},"id":"calc2"},
    {"jsonrpc":"2.0","method":"system.version","params":{},"id":"sys1"}
  ]'
```

Response:
```json
[
  {"jsonrpc":"2.0","result":3,"id":"calc1"},
  {"jsonrpc":"2.0","result":12.0,"id":"calc2"},
  {"jsonrpc":"2.0","result":"1.0.0","id":"sys1"}
]
```

### Error Handling - Division by Zero

```bash
curl -X POST http://localhost:5000/rpc \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "method": "calculator.divide",
    "params": {"a": 10, "b": 0},
    "id": 7
  }'
```

Response:
```json
{
  "jsonrpc":"2.0",
  "error":{
    "code":-32603,
    "message":"Cannot divide by zero"
  },
  "id":7
}
```

### Error Handling - Method Not Found

```bash
curl -X POST http://localhost:5000/rpc \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "method": "nonexistent.method",
    "params": {},
    "id": 8
  }'
```

Response:
```json
{
  "jsonrpc":"2.0",
  "error":{
    "code":-32601,
    "message":"Method not found: nonexistent.method"
  },
  "id":8
}
```

## Available Methods

| Method | Params | Returns | Description |
|--------|--------|---------|-------------|
| `calculator.add` | `{a: int, b: int}` | `int` | Add two numbers |
| `calculator.subtract` | `{a: int, b: int}` | `int` | Subtract b from a |
| `calculator.multiply` | `{a: double, b: double}` | `double` | Multiply two numbers |
| `calculator.divide` | `{a: double, b: double}` | `double` | Divide a by b |
| `user.get` | `{id: int}` | `UserDto` | Get user by ID |
| `user.create` | `{name: string, email: string}` | `UserDto` | Create new user |
| `user.list` | `{}` | `UserDto[]` | List all users |
| `system.version` | `{}` | `string` | Get server version |
| `system.ping` | `{}` | `string` | Health check |
| `system.time` | `{}` | `DateTime` | Get current UTC time |

## Testing with PowerShell

```powershell
# Ping
Invoke-RestMethod -Method Post -Uri "http://localhost:5000/rpc" `
  -ContentType "application/json" `
  -Body '{"jsonrpc":"2.0","method":"system.ping","params":{},"id":1}'

# Calculator
Invoke-RestMethod -Method Post -Uri "http://localhost:5000/rpc" `
  -ContentType "application/json" `
  -Body '{"jsonrpc":"2.0","method":"calculator.add","params":{"a":5,"b":3},"id":2}'

# Batch
Invoke-RestMethod -Method Post -Uri "http://localhost:5000/rpc" `
  -ContentType "application/json" `
  -Body '[{"jsonrpc":"2.0","method":"calculator.add","params":{"a":1,"b":2},"id":"1"},{"jsonrpc":"2.0","method":"calculator.multiply","params":{"a":3,"b":4},"id":"2"}]'
```

## Project Structure

```
RpcServer.Example/
├── Program.cs              # Main entry point, RPC configuration
├── Services/
│   ├── CalculatorService.cs  # Calculator implementation
│   └── UserService.cs        # User management implementation
└── RpcServer.Example.csproj
```

## Features Demonstrated

1. **Dependency Injection** - Services registered in DI container
2. **Context Sharing** - RPC context passed to all methods
3. **Error Handling** - Exceptions mapped to JSON-RPC errors
4. **Batch Processing** - Multiple requests in single HTTP call
5. **Rate Limiting** - 100 requests per minute per IP
6. **CORS** - Cross-origin requests enabled
7. **Health Checks** - `/health` endpoint
8. **Service Discovery** - `/` endpoint lists all available methods
