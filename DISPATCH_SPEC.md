# 智能派单模块 - 重新开发规范

## 概述
彻底删除旧的智能派单模块，按照WO物业管理系统的模块开发原则重新开发。

## 模块功能
1. **派单规则管理** - 配置自动派单规则
2. **派单任务管理** - 查看和管理派单任务
3. **超时规则配置** - 配置不同颜色工单的超时规则
4. **派单统计** - 派单成功率和响应时间统计

## 技术架构

### 后端服务
- **服务名称**: WO.Property.DispatchService
- **端口**: 5003
- **技术栈**: .NET 8 + Entity Framework Core + SQLite
- **认证**: 统一JWT认证（与AuthService一致）

### 前端模块
- **路径**: `/src/admin-portal/src/views/dispatch/`
- **组件**: DispatchList.vue, DispatchRules.vue, TimeoutSettings.vue
- **Store**: `dispatch.ts`, `timeout.ts`
- **路由**: `/dispatch`, `/dispatch-rules`, `/timeout-settings`

## 数据库设计

### DispatchRule (派单规则)
- id (主键)
- ruleNo (规则编号)
- name (规则名称)
- description (规则描述)
- type (规则类型: ticket_type/device/inspection/complaint/other)
- ticketColor (工单颜色: green/blue/orange/red)
- location (地点)
- locationType (地点匹配方式: exact/contains)
- priority (优先级 1-10)
- autoAssign (是否自动派单)
- notifyBackup (是否通知备份人员)
- allowTransfer (允许转单)
- timeoutEscalation (超时升级)
- operatorIds (操作人员ID列表)
- supervisorId (主管ID)
- managerId (经理ID)
- departmentHeadId (部门负责人ID)
- companyHeadId (公司负责人ID)
- backupIds (备用人员ID列表)
- notifyMethods (通知方式: sms/app/phone/email)
- status (状态: draft/active/inactive)
- matchCount (匹配次数)
- successCount (成功次数)
- avgResponseTime (平均响应时间)
- createdAt (创建时间)
- updatedAt (更新时间)
- createdBy (创建人)
- remark (备注)

### DispatchTask (派单任务)
- id (主键)
- taskNo (任务编号)
- ticketId (关联工单ID)
- ruleId (关联规则ID)
- assignedTo (指派给)
- assignedBy (指派人)
- assignedAt (指派时间)
- status (状态: pending/accepted/rejected/completed/cancelled)
- acceptedAt (接受时间)
- completedAt (完成时间)
- responseTime (响应时间)
- timeoutAt (超时时间)
- escalationLevel (升级层级)
- notes (备注)

### TimeoutRule (超时规则)
- id (主键)
- color (工单颜色: green/blue/orange/red)
- role (角色: operator/supervisor/manager/department_head/company_head)
- hours (超时小时数)
- enabled (是否启用)

## API设计

### 派单规则API
- `GET /api/dispatch-rules` - 获取所有规则
- `GET /api/dispatch-rules/{id}` - 获取单个规则
- `POST /api/dispatch-rules` - 创建规则
- `PUT /api/dispatch-rules/{id}` - 更新规则
- `DELETE /api/dispatch-rules/{id}` - 删除规则
- `POST /api/dispatch-rules/{id}/toggle-status` - 切换规则状态
- `POST /api/dispatch-rules/match` - 匹配规则

### 派单任务API
- `GET /api/dispatch-tasks` - 获取所有任务
- `GET /api/dispatch-tasks/{id}` - 获取单个任务
- `POST /api/dispatch-tasks` - 创建任务
- `PUT /api/dispatch-tasks/{id}` - 更新任务
- `POST /api/dispatch-tasks/{id}/accept` - 接受任务
- `POST /api/dispatch-tasks/{id}/complete` - 完成任务
- `POST /api/dispatch-tasks/{id}/escalate` - 升级任务

### 超时规则API
- `GET /api/timeout-rules` - 获取所有超时规则
- `PUT /api/timeout-rules` - 更新超时规则
- `POST /api/timeout-rules/reset` - 重置为默认

### 统计API
- `GET /api/dispatch/stats` - 获取派单统计
- `GET /api/dispatch/analytics` - 获取分析数据

## 前端组件设计

### DispatchList.vue
- 派单任务列表
- 任务状态管理
- 搜索和筛选
- 批量操作

### DispatchRules.vue
- 派单规则列表
- 规则增删改查
- 规则状态管理
- 规则匹配测试

### TimeoutSettings.vue
- 超时规则矩阵配置
- 颜色和角色配置
- 规则启用/禁用

## 开发原则
1. 遵循现有微服务架构模式
2. 使用统一JWT认证
3. 前端使用Vue 3 + TypeScript + Element Plus
4. 数据库使用SQLite（开发环境）
5. 遵循现有代码风格和命名规范
6. 包含完整的API文档
7. 支持字段配置系统

## 部署配置
- 添加到docker-compose.yml
- 配置Nginx反向代理
- 添加到服务状态监控

## 测试账号
- 使用统一测试账号：admin/Admin@123

## 开发计划
1. 创建后端服务结构
2. 实现数据库模型和API
3. 创建前端store和组件
4. 添加路由和权限控制
5. 集成字段配置系统
6. 测试和调试
7. 部署配置