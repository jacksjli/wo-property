# WO-Property 模块间数据调用设计规范 v1.0

> 制定日期：2026-05-28
> 最后更新：2026-05-28
> 状态：已生效

---

## 一、核心原则

模块间数据调用必须保证三个一致性：

1. **字段命名一致性** - API 响应使用 camelCase
2. **租户上下文不断链** - 请求从头到尾携带 tenant_code
3. **服务发现走网关** - 不硬编码下游服务端口

---

## 二、字段命名规范

### 规则
- 后端 API 响应 **必须使用 camelCase**
- 数据库列名使用 **snake_case**（MySQL 规范）
- 前端请求/响应使用 **camelCase**

### 正确示例
```json
// ✅ 正确
{ "id": 1, "personName": "张三", "phoneNumber": "13800138000" }

// ❌ 错误
{ "Id": 1, "PersonName": "张三", "PhoneNumber": "13800138000" }
```

### ASP.NET Core 配置
```csharp
// Program.cs
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    });
```

### Dapper 查询转换
```csharp
// 使用 ToCamelCase() 扩展方法
var result = reader.GetName(i);  // "PersonName"
return ToCamelCase(result);     // "personName"
```

---

## 三、租户上下文传递

### 流程
```
1. 客户端 → Gateway: GET /api/tenant/xxx + Authorization: Bearer <JWT>
                                 + X-Project: YGHY001 (可选)

2. Gateway → AuthService: 验证 JWT

3. AuthService → 返回 JWT含 claim: tenant_code = "wo_property"

4. 下游服务 TenantRoutingMiddleware:
   - 从 JWT 读取 tenant_code
   - 调用 SetCurrentTenantCode("wo_property")
   - TenantDbFactory 生成连接: Database=wo_property
```

### 中间件优先级
```
app.UseCors()                 // CORS 最先
app.UseTenantRouting()       // 租户路由（任何认证前）
app.UseAuthentication()      // JWT 验证
app.UseAuthorization()       // 权限检查
```

### 禁止的行为
```
❌ X-Project Header 作为主要租户来源（AuthService 已解析 JWT）
❌ 硬编码 tenantCode = "YGHY001"
❌ 直接查询 center_db.tenants 表
```

---

## 四、服务发现规范

### 正确方式
```
✅ 前端调用：http://localhost:5000/api/tenant/persons
✅ 服务间调用：通过 Gateway 代理
✅ 配置：config/ports.json 中定义服务端口映射
```

### 禁止的方式
```
❌ 前端直接调用：http://localhost:5018/api/persons
❌ 后端服务直接调用：http://localhost:5018/api/persons
❌ 硬编码服务 URL
```

### config/ports.json 结构
```json
{
  "services": {
    "gateway": { "port": 5000, "url": "http://localhost:5000" },
    "auth": { "port": 5106 },
    "person": { "port": 5018 },
    ...
  }
}
```

---

## 五、数据库规范

### 软删除过滤
```sql
-- 所有查询必须包含
WHERE status != '已删除'
```

### 外键关联
- creator_id 类型必须一致（统一使用 int）
- 跨服务外键使用 tenant_id + 服务标识

### 表命名
- 使用 snake_case：`renovation_applications`
- EF Core 映射使用 `[ToTable("renovation_applications")]`

---

## 六、分页响应格式规范

### 标准分页响应格式
```json
{
  "success": true,
  "data": [...],
  "total": 100,
  "page": 1,
  "pageSize": 20
}
```

### 禁止的格式
```json
// ❌ 缺少字段
{ "data": [...], "count": 100 }

// ❌ 字段名不一致
{ "items": [...], "totalCount": 100, "pageIndex": 1 }
```

---

## 七、时间格式规范

### 规则
- **必须使用** `DateTime.UtcNow`
- **禁止使用** `DateTime.Now`

### 正确示例
```csharp
// ✅ 正确
CreatedAt = DateTime.UtcNow

// ❌ 错误
CreatedAt = DateTime.Now
```

### 原因
- 服务器时区不同，UTC 保证一致性
- 前端统一按本地时区显示

---

## 八、错误响应格式规范

### 标准错误响应格式
```json
{
  "success": false,
  "message": "错误信息描述"
}
```

### 标准成功响应格式
```json
{
  "success": true,
  "data": {...},
  "message": "操作成功"  // 可选
}
```

### 禁止混用格式
```
❌ return Ok(new { success = false, message = "错误" })
❌ return NotFound(new { error = "未找到" })
✅ return Ok(new { success = false, message = "错误" })
```

---

## 九、状态值枚举规范

### 规则
- **必须使用英文状态值**
- **禁止使用中文状态值**

### 标准状态值
| 状态 | 英文值 | 说明 |
|------|--------|------|
| 待处理 | pending | |
| 已批准 | approved | |
| 已拒绝 | rejected | |
| 进行中 | processing | |
| 已完成 | completed | |
| 已取消 | cancelled | |
| 活跃 | active | |
| 未激活 | inactive | |

### 禁止的格式
```
❌ Status = "待处理"
❌ Status = "进行中"
❌ Status = "已完成"
✅ Status = "pending"
✅ Status = "completed"
```

---

## 十、自动检查机制

### 检查脚本
```bash
bash scripts/auto-check.sh              # 检查所有10项
bash scripts/auto-check.sh --api       # 只检查 API 字段
bash scripts/auto-check.sh --tenant    # 只检查租户
bash scripts/auto-check.sh --pagination # 只检查分页格式
bash scripts/auto-check.sh --time      # 只检查时间格式
bash scripts/auto-check.sh --error     # 只检查错误响应格式
bash scripts/auto-check.sh --auth      # 只检查授权头
bash scripts/auto-check.sh --status    # 只检查状态值枚举
```

### 检查项（共10项）
| # | 检查项 | 规则 | 级别 |
|---|--------|------|------|
| 1 | API 字段命名 | 响应必须 camelCase | P0 |
| 2 | 租户中间件 | 必须使用 SetCurrentTenantCode | P0 |
| 3 | 硬编码端口 | 不许硬编码 4位数字端口 | P1 |
| 4 | 软删除过滤 | WHERE 条件必须包含 status 检查 | P1 |
| 5 | 连接字符串 | 必须使用 TenantDbFactory + wo_property | P0 |
| 6 | 分页响应格式 | 必须包含 success/data/total/page/pageSize | P1 |
| 7 | 时间格式 | 必须使用 DateTime.UtcNow，禁止 DateTime.Now | P0 |
| 8 | 错误响应格式 | 必须使用 { success: false, message: "" } | P2 |
| 9 | JWT/授权头 | 必须正确解析 Bearer token | P1 |
| 10 | 状态值枚举 | 必须使用英文状态值，禁止中文 | P2 |

### 失败处理
| 级别 | 处理方式 |
|------|---------|
| P0 | 立即修复，阻止代码提交 |
| P1 | 24小时内修复 |
| P2 | 下次代码审查时修复 |

### 检查触发时机
1. **提交前** - 通过 git hooks
2. **每天定时** - cron job
3. **手动触发** - `bash scripts/auto-check.sh`

---

## 十一、违规处理

| 级别 | 违规类型 | 处理方式 |
|------|---------|---------|
| P0 | API 字段 PascalCase | 立即修复，24小时内 |
| P0 | 使用 DateTime.Now | 立即修复 |
| P0 | 租户上下文断裂 | 立即修复，12小时内 |
| P1 | 硬编码端口 | 修复，3天内 |
| P1 | 软删除遗漏 | 下次代码审查时修复 |
| P2 | 状态值使用中文 | 下次代码审查时修复 |

---

## 十二、相关文档

- `docs/design/API_FIELD_NAMING_STANDARD_v1.0.md` - 字段命名标准
- `docs/audit/FIELD_NAMING_AUDIT_2026-05-28.md` - 字段审计报告
- `scripts/auto-check.sh` - 自动检查脚本
- `scripts/post-test-hook.sh` - 测试后置钩子

---

_本文档是 WO-Property 项目架构标准的一部分_