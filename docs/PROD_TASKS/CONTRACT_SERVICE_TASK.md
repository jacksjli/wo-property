# 任务：填充 ContractService 业务功能

## 服务信息
- 路径：`/Users/mac/Projects/WO-Property-Management/src/WO.Property.ContractService/`
- 端口：5501
- 数据库：wo_property（已有表）

## 当前状态
- 已有 TenantContractController.cs（骨架，部分方法）
- 已有 ContractModels.cs（实体定义）
- 需要完成全部业务逻辑

## 业务需求
合同管理完整流程：
1. **合同登记** — 合同信息录入（编号、类型、甲方乙方、期限、金额）
2. **合同附件** — 上传合同扫描件
3. **合同执行** — 标记执行中/到期/续约
4. **合同查询** — 按类型/状态/期限查询
5. **到期提醒** — 30天内到期自动提醒

## API 端点设计
```
GET    /api/tenant/contracts              — 合同列表
POST   /api/tenant/contracts               — 创建合同
GET    /api/tenant/contracts/{id}         — 合同详情
PUT    /api/tenant/contracts/{id}          — 更新合同
DELETE /api/tenant/contracts/{id}         — 删除合同

POST   /api/tenant/contracts/{id}/activate  — 激活合同
POST   /api/tenant/contracts/{id}/terminate — 终止合同
POST   /api/tenant/contracts/{id}/renew     — 续约
GET    /api/tenant/contracts/expiring      — 30天内到期
```

## 设计原则
1. 使用 TenantDbContextFactory 动态切换租户数据库
2. 所有 API 需要 X-Project header 过滤
3. 完成后 rebuild 并验证服务启动正常

## 输出
完成后汇报：实现了哪些 API、数据库是否需修改、服务是否正常启动