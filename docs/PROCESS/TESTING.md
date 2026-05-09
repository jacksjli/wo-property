# 测试文档

> **版本**：v1.0
> **最后更新**：2026-05-05

---

## 1. 测试策略

### 1.1 测试金字塔

```
                    ┌───────────┐
                    │   E2E     │   少量，高价值
                    │   Tests   │   (端到端测试)
                    └─────┬─────┘
                    ┌─────┴─────┐
                    │Integration│  适量，中等价值
                    │   Tests   │   (API 集成测试)
                    └─────┬─────┘
                    ┌─────┴─────┐
                    │   Unit    │   大量，低成本
                    │   Tests   │   (单元测试)
                    └───────────┘
```

### 1.2 测试覆盖率目标

| 层级 | 覆盖率目标 |
|------|-----------|
| 单元测试 | 70%+ |
| 集成测试 | 50%+ |
| E2E 测试 | 核心流程覆盖 |

---

## 2. 单元测试

### 2.1 测试结构

```csharp
// ✅ 正确：AAA 模式（Arrange-Act-Assert）
[Fact]
public async Task CreateTicket_WithValidRequest_ReturnsCreatedTicket()
{
    // Arrange
    var request = new CreateTicketRequest
    {
        Title = "水管漏水",
        Priority = Priority.High
    };
    
    // Act
    var result = await _ticketService.CreateAsync(request);
    
    // Assert
    Assert.NotNull(result);
    Assert.Equal("水管漏水", result.Title);
    Assert.Equal(Priority.High, result.Priority);
}
```

### 2.2 单元测试规范

```
✓ 应该测试：
  - 正常流程
  - 边界条件
  - 错误处理
  - 并发场景（如果适用）

✗ 不需要测试：
  - 第三方库本身
  - 框架核心功能
  - 简单 getter/setter
```

---

## 3. 集成测试

### 3.1 API 测试

```csharp
// ✅ 正确：使用 TestServer 进行集成测试
[Fact]
public async Task GetTickets_WithValidQuery_ReturnsPaginatedList()
{
    // Arrange
    var client = _factory.CreateClient();
    client.DefaultRequestHeaders.Authorization = 
        new AuthenticationHeaderValue("Bearer", _testToken);
    
    // Act
    var response = await client.GetAsync("/api/tickets?page=1&pageSize=20");
    
    // Assert
    response.EnsureSuccessStatusCode();
    var content = await response.Content.ReadFromJsonAsync<PaginatedResult<TicketDto>>();
    Assert.NotNull(content);
    Assert.Equal(20, content.Data.Count);
}
```

### 3.2 数据库测试

```csharp
// ✅ 正确：使用真实数据库或 Docker
[Fact]
public async Task TicketRepository_CreatedAt_IsSet()
{
    // Arrange
    var ticket = new Ticket { Title = "Test" };
    
    // Act
    await _repository.AddAsync(ticket);
    await _context.SaveChangesAsync();
    
    // Assert
    Assert.NotEqual(default, ticket.CreatedAt);
}
```

---

## 4. E2E 测试

### 4.1 Playwright 测试

```typescript
// ✅ 正确：E2E 测试示例
test('工单创建流程', async ({ page }) => {
  // 登录
  await page.goto('/login');
  await page.fill('[name=username]', 'admin');
  await page.fill('[name=password]', 'password');
  await page.click('button[type=submit]');
  
  // 创建工单
  await page.goto('/tickets/new');
  await page.fill('[name=title]', '水管漏水');
  await page.selectOption('[name=priority]', 'high');
  await page.click('button[type=submit]');
  
  // 验证
  await expect(page.locator('.alert-success')).toContainText('工单创建成功');
});
```

---

## 5. 测试数据

### 5.1 测试数据工厂

```csharp
// ✅ 正确：使用工厂模式创建测试数据
public static class TestDataFactory
{
    public static Ticket CreateTicket(string title = "测试工单")
    {
        return new Ticket
        {
            Title = title,
            Priority = Priority.Normal,
            Status = TicketStatus.Created,
            CreatorId = 1,
            CreatedAt = DateTime.UtcNow
        };
    }
    
    public static Person CreatePerson(string name = "张三")
    {
        return new Person
        {
            Name = name,
            StaffId = $"EMP-{Guid.NewGuid():N}",
            Phone = "13800000000",
            Department = "工程部"
        };
    }
}
```

---

## 6. CI/CD 集成

### 6.1 GitHub Actions

```yaml
name: Tests

on: [push, pull_request]

jobs:
  test:
    runs-on: ubuntu-latest
    
    services:
      mysql:
        image: mysql:8.0
        env:
          MYSQL_ROOT_PASSWORD: test
          MYSQL_DATABASE: test_db
        ports:
          - 3306:3306
    
    steps:
      - uses: actions/checkout@v3
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '8.0'
      
      - name: Restore dependencies
        run: dotnet restore
      
      - name: Build
        run: dotnet build --no-restore
      
      - name: Run tests
        run: dotnet test --no-build --verbosity normal
```

---

## 7. 测试检查清单

### 7.1 上线前测试

```
□ 单元测试全部通过
□ 集成测试全部通过
□ E2E 核心流程通过
□ 性能测试达标
□ 安全扫描无高危漏洞
□ 数据库迁移测试通过
□ 回滚方案测试通过
```

---

**文档版本**：v1.0
**作者**：QA 工程师
**审核**：软件负责人
**状态**：待评审
