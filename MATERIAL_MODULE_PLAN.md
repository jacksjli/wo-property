# 物料管理模块开发计划

## 模块概述
物料管理模块是WO物业管理系统的核心业务模块之一，负责管理物业维护所需的物料、库存、采购和领用流程。

## 功能特性

### 已完成功能 ✅
1. **物料分类管理**
   - 物料分类CRUD操作
   - 多级分类支持
   - 分类描述和层级关系

2. **物料基础管理**
   - 物料编码自动生成
   - 物料基本信息管理（名称、描述、单位、单价）
   - 库存参数配置（安全库存、最大库存）
   - 存放位置和供应商管理

3. **库存管理**
   - 入库操作（采购入库、退货入库）
   - 出库操作（领用出库、报废出库）
   - 实时库存跟踪
   - 库存预警机制

4. **采购管理**
   - 采购订单创建
   - 采购状态跟踪（待审批、已批准、已发货、已完成）
   - 采购明细管理

5. **统计报表**
   - 库存统计（物料种类、库存价值）
   - 低库存预警
   - 缺货物料统计
   - 交易记录查询

### 技术实现

#### 后端服务
- **服务名称**: WO.Property.MaterialService
- **运行端口**: 5004
- **数据库**: SQLite (property_materials.db)
- **技术栈**: .NET 10 Web API + Entity Framework Core
- **认证**: JWT Bearer Token
- **API文档**: Swagger UI

#### 前端页面
- **页面路径**: `/materials`
- **技术栈**: Vue 3 + TypeScript + Element Plus
- **状态管理**: Pinia
- **路由**: Vue Router
- **API集成**: Axios HTTP客户端

## 数据库设计

### 核心表结构
1. **MaterialCategories** - 物料分类表
2. **Materials** - 物料主表
3. **StockTransactions** - 库存交易记录表
4. **PurchaseOrders** - 采购订单表
5. **PurchaseOrderItems** - 采购订单明细表

### 关键字段
- 物料编码自动生成规则: `PO-{yyyyMMdd}-{随机数}`
- 库存预警: 当前库存 ≤ 安全库存时触发预警
- 交易记录: 记录所有入库/出库操作，支持关联工单

## API端点

### 健康检查
- `GET /health` - 服务健康状态

### 物料分类管理
- `GET /api/material-categories` - 获取所有分类
- `POST /api/material-categories` - 创建分类

### 物料管理
- `GET /api/materials` - 获取物料列表（支持分类筛选和搜索）
- `GET /api/materials/{id}` - 获取物料详情
- `POST /api/materials` - 创建物料
- `PUT /api/materials/{id}` - 更新物料

### 库存管理
- `POST /api/materials/{id}/stock-in` - 入库操作
- `POST /api/materials/{id}/stock-out` - 出库操作

### 采购管理
- `GET /api/purchase-orders` - 获取采购订单列表
- `POST /api/purchase-orders` - 创建采购订单

### 统计报表
- `GET /api/materials/statistics` - 获取库存统计和最近交易记录

## 前端页面功能

### 物料列表页面
- 物料分类筛选
- 关键字搜索
- 库存状态筛选（正常/低库存/缺货）
- 分页显示
- 实时库存状态显示
- 快速入库/出库操作

### 统计面板
- 物料种类统计
- 低库存物料数量
- 缺货物料数量
- 库存总价值
- 最近交易记录

### 操作对话框
- 添加物料对话框
- 入库操作对话框
- 出库操作对话框

## 测试数据

### 预置物料分类
1. 电气材料 - 电线、开关、插座等
2. 管道材料 - 水管、阀门、接头等
3. 工具设备 - 维修工具、测量仪器等
4. 清洁用品 - 清洁剂、拖把、垃圾袋等
5. 办公用品 - 纸张、笔、文件夹等

### 预置物料示例
1. 电线（2.5平方） - 25卷库存，安全库存10卷
2. PVC水管（25mm） - 120米库存，安全库存50米
3. 多功能工具箱 - 8套库存，安全库存5套

## 集成测试

### 服务健康检查
```bash
curl http://localhost:5004/health
```

### API测试
```bash
# 获取物料分类
curl http://localhost:5004/api/material-categories

# 获取物料列表
curl http://localhost:5004/api/materials

# 获取库存统计
curl http://localhost:5004/api/materials/statistics
```

## 下一步计划

### 短期计划（1-2周）
1. **采购订单完整流程**
   - 采购订单审批流程
   - 采购订单状态管理
   - 采购收货确认

2. **库存预警通知**
   - 低库存自动通知
   - 缺货预警邮件/消息
   - 定期库存报告

3. **物料关联功能**
   - 物料与工单关联
   - 物料与设备关联
   - 物料使用记录

### 中期计划（1个月）
1. **高级报表功能**
   - 库存周转率分析
   - 采购成本分析
   - 物料使用趋势分析

2. **批量操作**
   - 批量入库
   - 批量出库
   - 物料导入/导出

3. **供应商管理**
   - 供应商信息管理
   - 供应商评价系统
   - 采购历史记录

### 长期计划（2-3个月）
1. **移动端支持**
   - 库存盘点APP
   - 扫码入库/出库
   - 移动端报表查看

2. **智能预测**
   - 库存需求预测
   - 采购建议系统
   - 季节性需求分析

3. **多仓库管理**
   - 多仓库库存管理
   - 仓库间调拨
   - 仓库容量管理

## 部署说明

### 环境要求
- .NET 10 Runtime
- Node.js 18+
- SQLite数据库

### 启动命令
```bash
# 启动物料管理服务
cd src/WO.Property.MaterialService
dotnet run --urls "http://localhost:5004"

# 访问前端页面
http://localhost:5174/materials
```

### 配置文件
- `appsettings.json` - 应用配置
- `property_materials.db` - SQLite数据库文件
- `Program.cs` - 主程序文件

## 维护说明

### 数据库备份
```bash
# 备份数据库
cp property_materials.db property_materials_backup_$(date +%Y%m%d).db
```

### 日志查看
```bash
# 查看服务日志
tail -f logs/app.log
```

### 性能监控
- 监控API响应时间
- 监控数据库连接数
- 监控内存使用情况

## 故障排除

### 常见问题
1. **数据库连接失败**
   - 检查数据库文件权限
   - 检查数据库文件路径

2. **API调用失败**
   - 检查服务是否运行
   - 检查端口是否被占用
   - 检查JWT令牌是否有效

3. **前端页面加载失败**
   - 检查网络连接
   - 检查API服务状态
   - 清除浏览器缓存

### 联系支持
- 开发团队: WO物业管理开发组
- 技术支持: tech-support@wo-property.com
- 文档地址: https://docs.wo-property.com/material
