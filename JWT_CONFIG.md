# WO物业管理软件 - 统一JWT配置

## 概述
为了解决微服务架构中的认证兼容性问题，我们建立了统一的认证授权服务。所有服务使用相同的JWT配置，实现单点登录和跨服务认证。

## 统一认证服务
- **服务地址**: http://localhost:5006
- **数据库**: property_unified_auth.db
- **功能**: 用户注册、登录、令牌颁发、令牌验证

## JWT配置参数

### 核心配置
```json
{
  "JwtSettings": {
    "SecretKey": "WO-Property-Management-Unified-Secret-Key-2026-For-All-Services",
    "Issuer": "wo-property-unified-auth",
    "Audience": "wo-property-services",
    "ExpiryDays": 7
  }
}
```

### 技术参数
- **算法**: HS256 (HMAC SHA-256)
- **令牌有效期**: 7天
- **时钟偏差**: 0秒（严格验证）
- **签名密钥**: 256位对称密钥

## 服务集成指南

### 1. 后端服务配置
所有.NET服务应使用以下JWT配置：

```csharp
// 在Program.cs中添加
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"] ?? "WO-Property-Management-Unified-Secret-Key-2026-For-All-Services";
var issuer = jwtSettings["Issuer"] ?? "wo-property-unified-auth";
var audience = jwtSettings["Audience"] ?? "wo-property-services";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = issuer,
        ValidAudience = audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
        ClockSkew = TimeSpan.Zero
    };
});
```

### 2. 前端配置
前端应使用统一认证服务的API：

```typescript
// API配置
export const API_CONFIG = {
  AUTH_SERVICE: {
    BASE_URL: 'http://localhost:5006',
    ENDPOINTS: {
      LOGIN: '/api/auth/login',
      REGISTER: '/api/auth/register',
      ME: '/api/auth/me',
      VALIDATE: '/api/auth/validate'
    }
  }
};

// 获取令牌后，可用于所有服务
const token = '从统一认证服务获取的JWT令牌';

// 调用其他服务时使用相同的令牌
fetch('http://localhost:5002/api/tickets', {
  headers: {
    'Authorization': `Bearer ${token}`
  }
});
```

### 3. 令牌验证API
其他服务可以通过调用统一认证服务的验证API来验证令牌：

```bash
# 验证令牌
curl -X POST http://localhost:5006/api/auth/validate \
  -H "Content-Type: application/json" \
  -d '{"token":"YOUR_JWT_TOKEN"}'
```

## 用户角色定义

### 预定义角色
1. **Administrator** (管理员)
   - 最高权限
   - 可以管理所有用户和系统设置
   - 可以访问所有API

2. **Technician** (技术人员)
   - 可以处理工单和设备维护
   - 可以管理物料库存
   - 不能管理用户和系统设置

3. **User** (普通用户)
   - 可以提交工单
   - 可以查看设备状态
   - 可以查看物料库存
   - 权限最低

### 角色声明
在JWT令牌中，角色通过`ClaimTypes.Role`声明：
```json
{
  "http://schemas.microsoft.com/ws/2008/06/identity/claims/role": "Administrator"
}
```

## 测试账户

### 预置用户
1. **管理员**
   - 用户名: admin
   - 密码: Admin@123
   - 邮箱: admin@wo-property.com
   - 角色: Administrator

2. **技术人员**
   - 用户名: tech
   - 密码: Tech@123
   - 邮箱: tech@wo-property.com
   - 角色: Technician

3. **普通用户**
   - 用户名: user
   - 密码: User@123
   - 邮箱: user@wo-property.com
   - 角色: User

## 令牌结构示例

### JWT Header
```json
{
  "alg": "HS256",
  "typ": "JWT"
}
```

### JWT Payload
```json
{
  "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier": "1",
  "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name": "admin",
  "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress": "admin@wo-property.com",
  "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/givenname": "系统管理员",
  "http://schemas.microsoft.com/ws/2008/06/identity/claims/role": "Administrator",
  "Status": "Active",
  "jti": "d01aa956-92e1-4e0c-bba1-34f9aa69e0e8",
  "exp": 1777299585,
  "iss": "wo-property-unified-auth",
  "aud": "wo-property-services"
}
```

### JWT Signature
使用HS256算法和统一密钥生成的签名。

## 安全注意事项

### 密钥管理
1. **开发环境**: 使用文档中的默认密钥
2. **测试环境**: 应使用不同的密钥
3. **生产环境**: 必须使用强随机密钥，并通过安全的方式管理

### 令牌安全
1. **传输安全**: 始终使用HTTPS传输令牌
2. **存储安全**: 前端应将令牌存储在安全的地方（如HttpOnly Cookie）
3. **令牌刷新**: 实现令牌刷新机制，避免长期有效的令牌
4. **撤销机制**: 实现令牌撤销列表（黑名单）用于登出

### 权限验证
1. **服务端验证**: 所有API端点都应验证用户角色和权限
2. **最小权限原则**: 只授予必要的最小权限
3. **输入验证**: 验证所有用户输入，防止注入攻击

## 故障排除

### 常见问题
1. **令牌无效**
   - 检查令牌是否过期
   - 验证签名是否正确
   - 确认Issuer和Audience匹配

2. **权限不足**
   - 检查用户角色
   - 验证API端点是否要求特定角色
   - 确认令牌中的角色声明正确

3. **跨域问题**
   - 确保CORS配置正确
   - 检查前端请求头是否正确设置

### 调试工具
1. **JWT调试器**: https://jwt.io
2. **日志查看**: 检查服务日志中的认证错误
3. **API测试**: 使用Postman或curl测试API

## 更新日志

### 2026-04-20
- 创建统一认证服务
- 建立统一JWT配置
- 提供测试账户和集成指南

## 联系支持
- **技术负责人**: 飞翔的芦苇 (AI助手)
- **项目状态**: 统一认证服务已部署，等待其他服务集成
- **支持邮箱**: tech-support@wo-property.com
