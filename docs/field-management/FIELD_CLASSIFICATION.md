# 字段分类体系文档（更新版）

> **版本**：v2.0
> **更新日期**：2026-05-05
> **基于**：person-service-api.md, masterdata-service-api.md

---

## 1. 概述

### 1.1 更新说明

本文档基于已完成的 PersonService API 和 MasterDataService API 文档，对原有字段分类体系进行了更新：

**主要变更**：
1. 明确了所有共享字段的 API 来源（PersonService 或 MasterDataService）
2. 更新了字段映射关系
3. 补充了服务间调用的字段获取方式

### 1.2 字段来源架构

```
┌─────────────────────────────────────────────────────────────────┐
│                         前端 fieldConfig.ts                       │
│                    （从 API 动态获取 options）                    │
└───────────────────────────────┬─────────────────────────────────┘
                                │
        ┌───────────────────────┼───────────────────────┐
        ▼                       ▼                       ▼
┌───────────────┐     ┌─────────────────┐     ┌───────────────┐
│ PersonService │     │ MasterDataService│     │ 各微服务私有  │
│   (5018)      │     │    (5019)        │     │   (5021+)    │
├───────────────┤     ├─────────────────┤     ├───────────────┤
│ • 人员信息     │     │ • 楼栋/房间      │     │ • 私有编号    │
│ • 部门枚举     │     │ • 业务枚举       │     │ • 私有业务    │
│ • 职位枚举     │     │ • 位置信息       │     │ • 时间戳      │
└───────────────┘     └─────────────────┘     └───────────────┘
```

---

## 2. 共享字段来源明细

### 2.1 来自 PersonService 的字段

> **API 端点**：`GET http://localhost:5018/api/persons`
> **枚举端点**：`GET http://localhost:5018/api/enums/{type}`

| 字段名 | 英文名 | 类型 | 说明 | 获取方式 |
|-------|--------|------|------|---------|
| 人员ID | id | int | 主键 | `GET /api/persons/{id}` |
| 员工编号 | staffId | text | 工号，唯一 | `GET /api/persons` |
| 姓名 | name | text | 人员姓名 | `GET /api/persons` |
| 性别 | gender | select | 男/女 | `GET /api/enums/genders` |
| 联系电话 | phone | text | 手机号 | `GET /api/persons` |
| 邮箱 | email | text | 邮箱 | `GET /api/persons` |
| 部门 | department | select | 工程部/客服部/... | `GET /api/enums/departments` |
| 职位 | role | select | operator/supervisor/... | `GET /api/enums/roles` |
| 入职日期 | joinDate | date | - | `GET /api/persons` |
| 员工状态 | status | select | 在职/离职/休假/停职 | `GET /api/persons` |
| 身份证号 | idCard | text | - | `GET /api/persons/{id}` (需权限) |
| 紧急联系人 | emergencyContact | text | - | `GET /api/persons/{id}` (需权限) |
| 紧急联系电话 | emergencyPhone | text | - | `GET /api/persons/{id}` (需权限) |
| 头像 | avatarUrl | text | URL | `GET /api/persons` |
| 人员类型 | personType | select | 员工/住户/业主/访客 | `GET /api/enums/person-types` |
| 房号 | unit | text | 住户用 | `GET /api/persons` |
| 楼层 | floor | text | 住户用 | `GET /api/persons` |
| 入住日期 | moveInDate | date | 住户用 | `GET /api/persons/{id}` |
| 车辆信息 | carInfo | text | 住户用 | `GET /api/persons/{id}` |

**PersonService 枚举值 API**：
```
GET /api/enums/genders         → [{value: "男", label: "男"}, {value: "女", label: "女"}]
GET /api/enums/departments     → [{value: "工程部", label: "工程部"}, ...]
GET /api/enums/roles           → [{value: "operator", label: "操作员", level: 1}, ...]
GET /api/enums/person-types    → [{value: "员工", label: "员工"}, ...]
```

### 2.2 来自 MasterDataService 的字段

> **API 端点**：`GET http://localhost:5019/api/enums/{category}`
> **楼栋/房间**：`GET http://localhost:5019/api/buildings`, `GET http://localhost:5019/api/rooms`

#### 2.2.1 楼栋/房间相关字段

| 字段名 | 英文名 | 类型 | 说明 | 获取方式 |
|-------|--------|------|------|---------|
| 楼栋 | buildingId | select | 楼栋下拉 | `GET /api/buildings` |
| 楼层 | floor | text | 楼层 | 来自 Rooms 表 |
| 房号 | roomNo / unit | text | 房间号 | `GET /api/buildings/{id}/rooms` |
| 地址 | address | text | 地址 | 来自 Rooms 表 |
| 安装位置 | location | text | 设备位置 | 手动输入或选择 |

#### 2.2.2 业务枚举字段

| 字段名 | 英文名 | Category | 获取方式 |
|-------|--------|----------|---------|
| 工单类型 | type | ticket-types | `GET /api/enums/ticket-types` |
| 优先级 | priority | priorities | `GET /api/enums/priorities` |
| 工单状态 | status | ticket-statuses | `GET /api/enums/ticket-statuses` |
| 设备类型 | deviceType | device-types | `GET /api/enums/device-types` |
| 物料分类 | category | material-categories | `GET /api/enums/material-categories` |
| 合同类型 | contractType | contract-types | `GET /api/enums/contract-types` |
| 付款方式 | paymentMethod | payment-methods | `GET /api/enums/payment-methods` |
| 付款状态 | paymentStatus | payment-statuses | `GET /api/enums/payment-statuses` |
| 巡检周期 | inspectionCycle | inspection-cycles | `GET /api/enums/inspection-cycles` |
| 投诉类型 | type | complaint-types | `GET /api/enums/complaint-types` |
| 投诉来源 | source | complaint-sources | `GET /api/enums/complaint-sources` |
| 钥匙类型 | keyType | key-types | `GET /api/enums/key-types` |
| 访客类型 | visitorType | visitor-types | `GET /api/enums/visitor-types` |
| 通知类型 | type | notification-types | `GET /api/enums/notification-types` |
| 通知优先级 | level | notification-priorities | `GET /api/enums/notification-priorities` |
| 学历 | education | educations | `GET /api/enums/educations` |
| 用工类型 | employmentType | employment-types | `GET /api/enums/employment-types` |
| 收费类型 | feeType | fee-types | `GET /api/enums/fee-types` |
| 收费周期 | cycle | fee-cycles | `GET /api/enums/fee-cycles` |
| 住户状态 | status | resident-statuses | `GET /api/enums/resident-statuses` |
| 车位状态 | status | parking-statuses | `GET /api/enums/parking-statuses` |
| 车位类型 | type | parking-types | `GET /api/enums/parking-types` |
| 计量单位 | unit | units | `GET /api/enums/units` |

---

## 3. 各模块字段分类更新版

### 3.1 工单模块（ticket）

**数据库表**: Tickets (TicketService)

| 序号 | 显示名 | 字段名 | 类型 | 分类 | 来源服务 | API端点 | 必填 |
|------|--------|--------|------|------|----------|---------|------|
| 1 | 工单编号 | ticketNo | text | 私有 | TicketService | - | 是 |
| 2 | 工单标题 | title | text | 私有 | TicketService | - | 是 |
| 3 | 工单类型 | type | select | 共享 | MasterDataService | GET /api/enums/ticket-types | 是 |
| 4 | 优先级 | priority | select | 共享 | MasterDataService | GET /api/enums/priorities | 是 |
| 5 | 工单状态 | status | select | 共享 | MasterDataService | GET /api/enums/ticket-statuses | 是 |
| 6 | 工单描述 | description | textarea | 私有 | TicketService | - | 否 |
| 7 | 创建人 | creatorName | text | 共享 | PersonService | GET /api/persons/{id} | 否 |
| 8 | 创建时间 | createTime | date | 私有 | 系统 | - | 否 |
| 9 | 指派人 | assigneeName | text | 共享 | PersonService | GET /api/persons/{id} | 否 |
| 10 | 处理时间 | handleTime | date | 私有 | TicketService | - | 否 |
| 11 | 完成时间 | completeTime | date | 私有 | TicketService | - | 否 |
| 12 | 联系人 | contactName | text | 共享 | PersonService | - | 否 |
| 13 | 联系电话 | contactPhone | text | 共享 | PersonService | - | 否 |
| 14 | 位置 | location | text | 共享 | MasterDataService | - | 否 |
| 15 | 备注 | remark | textarea | 共享 | 系统 | - | 否 |

**共享字段来源汇总**：
- `type`, `priority`, `status` → MasterDataService `/api/enums/{category}`
- `creatorName`, `assigneeName`, `contactName`, `contactPhone` → PersonService `/api/persons/{id}`
- `location` → 手动输入或 MasterDataService 楼栋/房间

### 3.2 设备模块（device）

**数据库表**: Devices (DeviceService)

| 序号 | 显示名 | 字段名 | 类型 | 分类 | 来源服务 | API端点 | 必填 |
|------|--------|--------|------|------|----------|---------|------|
| 1 | 设备编号 | deviceNo | text | 私有 | DeviceService | - | 是 |
| 2 | 设备名称 | deviceName | text | 私有 | DeviceService | - | 是 |
| 3 | 设备类型 | deviceType | select | 共享 | MasterDataService | GET /api/enums/device-types | 是 |
| 4 | 设备型号 | model | text | 私有 | DeviceService | - | 否 |
| 5 | 设备品牌 | brand | text | 私有 | DeviceService | - | 否 |
| 6 | 安装位置 | location | text | 共享 | MasterDataService | - | 是 |
| 7 | 设备状态 | status | select | 私有 | DeviceService | - | 是 |
| 8 | 巡检周期 | inspectionCycle | select | 共享 | MasterDataService | GET /api/enums/inspection-cycles | 是 |
| 9 | 购买日期 | purchaseDate | date | 私有 | DeviceService | - | 否 |
| 10 | 维保截止 | warrantyEndDate | date | 私有 | DeviceService | - | 否 |
| 11 | 供应商 | supplier | text | 私有 | DeviceService | - | 否 |
| 12 | 负责人 | responsible | text | 共享 | PersonService | GET /api/persons/{id} | 否 |
| 13 | 备注 | remark | textarea | 共享 | 系统 | - | 否 |

**共享字段来源汇总**：
- `deviceType` → MasterDataService `/api/enums/device-types`
- `inspectionCycle` → MasterDataService `/api/enums/inspection-cycles`
- `location` → 手动输入
- `responsible` → PersonService `/api/persons/{id}`

### 3.3 物料模块（material）

**数据库表**: Materials (MaterialService)

| 序号 | 显示名 | 字段名 | 类型 | 分类 | 来源服务 | API端点 | 必填 |
|------|--------|--------|------|------|----------|---------|------|
| 1 | 物料编号 | materialNo | text | 私有 | MaterialService | - | 是 |
| 2 | 物料名称 | materialName | text | 私有 | MaterialService | - | 是 |
| 3 | 物料分类 | category | select | 共享 | MasterDataService | GET /api/enums/material-categories | 是 |
| 4 | 规格型号 | specification | text | 私有 | MaterialService | - | 否 |
| 5 | 单位 | unit | select | 共享 | MasterDataService | GET /api/enums/units | 是 |
| 6 | 库存数量 | quantity | number | 私有 | MaterialService | - | 是 |
| 7 | 库存上限 | maxStock | number | 私有 | MaterialService | - | 否 |
| 8 | 库存下限 | minStock | number | 私有 | MaterialService | - | 否 |
| 9 | 单价 | unitPrice | number | 私有 | MaterialService | - | 否 |
| 10 | 供应商 | supplier | text | 私有 | MaterialService | - | 否 |
| 11 | 存放位置 | location | text | 共享 | MasterDataService | - | 否 |
| 12 | 物料状态 | status | select | 私有 | MaterialService | - | 是 |
| 13 | 备注 | remark | textarea | 共享 | 系统 | - | 否 |

### 3.4 合同模块（contract）

**数据库表**: Contracts (ContractService)

| 序号 | 显示名 | 字段名 | 类型 | 分类 | 来源服务 | API端点 | 必填 |
|------|--------|--------|------|------|----------|---------|------|
| 1 | 合同编号 | contractNo | text | 私有 | ContractService | - | 是 |
| 2 | 合同名称 | contractName | text | 私有 | ContractService | - | 是 |
| 3 | 合同类型 | contractType | select | 共享 | MasterDataService | GET /api/enums/contract-types | 是 |
| 4 | 甲方 | partyA | text | 私有 | ContractService | - | 是 |
| 5 | 乙方 | partyB | text | 私有 | ContractService | - | 是 |
| 6 | 合同金额 | amount | number | 私有 | ContractService | - | 是 |
| 7 | 签订日期 | signDate | date | 私有 | ContractService | - | 否 |
| 8 | 开始日期 | startDate | date | 私有 | ContractService | - | 是 |
| 9 | 结束日期 | endDate | date | 私有 | ContractService | - | 是 |
| 10 | 付款方式 | paymentMethod | select | 共享 | MasterDataService | GET /api/enums/payment-methods | 否 |
| 11 | 付款状态 | paymentStatus | select | 共享 | MasterDataService | GET /api/enums/payment-statuses | 是 |
| 12 | 合同状态 | status | select | 共享 | 系统 | - | 是 |
| 13 | 负责人 | responsible | text | 共享 | PersonService | GET /api/persons/{id} | 否 |
| 14 | 备注 | remark | textarea | 共享 | 系统 | - | 否 |

### 3.5 投诉模块（complaint）

**数据库表**: Complaints (ComplaintService)

| 序号 | 显示名 | 字段名 | 类型 | 分类 | 来源服务 | API端点 | 必填 |
|------|--------|--------|------|------|----------|---------|------|
| 1 | 投诉编号 | complaintNo | text | 私有 | ComplaintService | - | 是 |
| 2 | 投诉标题 | title | text | 私有 | ComplaintService | - | 是 |
| 3 | 投诉类型 | type | select | 共享 | MasterDataService | GET /api/enums/complaint-types | 是 |
| 4 | 投诉来源 | source | select | 共享 | MasterDataService | GET /api/enums/complaint-sources | 是 |
| 5 | 投诉人 | complainant | text | 共享 | PersonService | - | 是 |
| 6 | 联系电话 | phone | text | 共享 | PersonService | - | 是 |
| 7 | 房号 | roomNo | text | 共享 | MasterDataService | - | 否 |
| 8 | 投诉内容 | content | textarea | 私有 | ComplaintService | - | 是 |
| 9 | 优先级 | priority | select | 共享 | MasterDataService | GET /api/enums/priorities | 是 |
| 10 | 处理状态 | handleStatus | select | 共享 | 系统 | - | 是 |
| 11 | 处理人 | handler | text | 共享 | PersonService | GET /api/persons/{id} | 否 |
| 12 | 处理时间 | handleTime | date | 私有 | ComplaintService | - | 否 |
| 13 | 处理结果 | result | textarea | 私有 | ComplaintService | - | 否 |
| 14 | 关闭时间 | closeTime | date | 私有 | ComplaintService | - | 否 |
| 15 | 备注 | remark | textarea | 共享 | 系统 | - | 否 |

### 3.6 钥匙模块（key）

**数据库表**: Keys (KeyService)

| 序号 | 显示名 | 字段名 | 类型 | 分类 | 来源服务 | API端点 | 必填 |
|------|--------|--------|------|------|----------|---------|------|
| 1 | 钥匙编号 | keyNo | text | 私有 | KeyService | - | 是 |
| 2 | 钥匙名称 | keyName | text | 私有 | KeyService | - | 是 |
| 3 | 钥匙类型 | keyType | select | 共享 | MasterDataService | GET /api/enums/key-types | 是 |
| 4 | 关联位置 | location | text | 共享 | MasterDataService | - | 是 |
| 5 | 钥匙状态 | status | select | 私有 | KeyService | - | 是 |
| 6 | 借用人 | borrower | text | 共享 | PersonService | GET /api/persons/{id} | 否 |
| 7 | 借用时间 | borrowTime | date | 私有 | KeyService | - | 否 |
| 8 | 预计归还 | expectedReturn | date | 私有 | KeyService | - | 否 |
| 9 | 实际归还 | actualReturn | date | 私有 | KeyService | - | 否 |
| 10 | 备注 | remark | textarea | 共享 | 系统 | - | 否 |

### 3.7 访客模块（visitor）

**数据库表**: Visitors (VisitorService)

| 序号 | 显示名 | 字段名 | 类型 | 分类 | 来源服务 | API端点 | 必填 |
|------|--------|--------|------|------|----------|---------|------|
| 1 | 访客编号 | visitorNo | text | 私有 | VisitorService | - | 是 |
| 2 | 访客姓名 | visitorName | text | 私有 | VisitorService | - | 是 |
| 3 | 访客类型 | visitorType | select | 共享 | MasterDataService | GET /api/enums/visitor-types | 是 |
| 4 | 联系电话 | phone | text | 共享 | PersonService | - | 是 |
| 5 | 身份证号 | idCard | text | 私有 | VisitorService | - | 否 |
| 6 | 受访住户 | visitedResident | text | 共享 | PersonService | - | 是 |
| 7 | 受访房号 | roomNo | text | 共享 | MasterDataService | - | 是 |
| 8 | 来访事由 | purpose | textarea | 私有 | VisitorService | - | 是 |
| 9 | 来访时间 | checkInTime | date | 私有 | VisitorService | - | 是 |
| 10 | 离开时间 | checkOutTime | date | 私有 | VisitorService | - | 否 |
| 11 | 访客状态 | status | select | 共享 | 系统 | - | 是 |
| 12 | 放行签字 | signature | text | 私有 | VisitorService | - | 否 |
| 13 | 备注 | remark | textarea | 共享 | 系统 | - | 否 |

### 3.8 住户模块（resident）

**数据来源**: PersonService (PersonType='住户')

| 序号 | 显示名 | 字段名 | 类型 | 分类 | 来源服务 | API端点 | 必填 |
|------|--------|--------|------|------|----------|---------|------|
| 1 | 住户姓名 | name | text | 共享 | PersonService | GET /api/persons | 是 |
| 2 | 联系电话 | phone | text | 共享 | PersonService | GET /api/persons | 是 |
| 3 | 房号 | unit | text | 共享 | PersonService | GET /api/persons | 是 |
| 4 | 楼层 | floor | text | 共享 | PersonService | GET /api/persons | 否 |
| 5 | 住户状态 | status | select | 共享 | PersonService | - | 是 |
| 6 | 身份证号 | idCard | text | 共享 | PersonService | GET /api/persons/{id} | 否 |
| 7 | 入住日期 | moveInDate | date | 共享 | PersonService | GET /api/persons/{id} | 否 |
| 8 | 迁出日期 | moveOutDate | date | 共享 | PersonService | GET /api/persons/{id} | 否 |
| 9 | 紧急联系人 | emergencyContact | text | 共享 | PersonService | GET /api/persons/{id} | 否 |
| 10 | 紧急电话 | emergencyPhone | text | 共享 | PersonService | GET /api/persons/{id} | 否 |
| 11 | 车辆信息 | carInfo | text | 共享 | PersonService | GET /api/persons/{id} | 否 |
| 12 | 备注 | remark | textarea | 共享 | 系统 | - | 否 |

**关键变更**：住户模块数据完全来自 PersonService，通过 `PersonType='住户'` 筛选。

---

## 4. 字段来源映射表（完整版）

### 4.1 按来源服务分组

#### PersonService (5018) 提供

| 字段 | 说明 | API |
|------|------|-----|
| id | 人员主键 | GET /api/persons/{id} |
| staffId | 工号 | GET /api/persons |
| name | 姓名 | GET /api/persons |
| gender | 性别 | GET /api/enums/genders |
| phone | 电话 | GET /api/persons |
| email | 邮箱 | GET /api/persons |
| department | 部门 | GET /api/enums/departments |
| role | 职位 | GET /api/enums/roles |
| joinDate | 入职日期 | GET /api/persons |
| status | 状态 | GET /api/persons |
| idCard | 身份证 | GET /api/persons/{id} |
| emergencyContact | 紧急联系人 | GET /api/persons/{id} |
| emergencyPhone | 紧急电话 | GET /api/persons/{id} |
| avatarUrl | 头像 | GET /api/persons |
| personType | 人员类型 | GET /api/enums/person-types |
| unit | 房号（住户） | GET /api/persons |
| floor | 楼层（住户） | GET /api/persons |
| moveInDate | 入住日期 | GET /api/persons/{id} |
| carInfo | 车辆信息 | GET /api/persons/{id} |

#### MasterDataService (5019) 提供

| 字段 | Category | API |
|------|----------|-----|
| ticket_type | 工单类型 | GET /api/enums/ticket-types |
| priority | 优先级 | GET /api/enums/priorities |
| ticket_status | 工单状态 | GET /api/enums/ticket-statuses |
| device_type | 设备类型 | GET /api/enums/device-types |
| material_category | 物料分类 | GET /api/enums/material-categories |
| contract_type | 合同类型 | GET /api/enums/contract-types |
| payment_method | 付款方式 | GET /api/enums/payment-methods |
| payment_status | 付款状态 | GET /api/enums/payment-statuses |
| inspection_cycle | 巡检周期 | GET /api/enums/inspection-cycles |
| complaint_type | 投诉类型 | GET /api/enums/complaint-types |
| complaint_source | 投诉来源 | GET /api/enums/complaint-sources |
| key_type | 钥匙类型 | GET /api/enums/key-types |
| visitor_type | 访客类型 | GET /api/enums/visitor-types |
| notification_type | 通知类型 | GET /api/enums/notification-types |
| notification_priority | 通知优先级 | GET /api/enums/notification-priorities |
| education | 学历 | GET /api/enums/educations |
| employment_type | 用工类型 | GET /api/enums/employment-types |
| fee_type | 收费类型 | GET /api/enums/fee-types |
| fee_cycle | 收费周期 | GET /api/enums/fee-cycles |
| resident_status | 住户状态 | GET /api/enums/resident-statuses |
| parking_status | 车位状态 | GET /api/enums/parking-statuses |
| parking_type | 车位类型 | GET /api/enums/parking-types |
| unit | 计量单位 | GET /api/enums/units |

#### 楼栋/房间 API

| 资源 | API | 用途 |
|------|-----|------|
| Buildings | GET /api/buildings | 获取楼栋列表 |
| Buildings | GET /api/buildings/{id} | 获取楼栋详情 |
| Rooms | GET /api/buildings/{id}/rooms | 获取某楼栋的房间 |
| Rooms | GET /api/rooms/{id} | 获取房间详情 |
| Rooms | GET /api/rooms/search | 搜索房间 |

---

## 5. 前端 fieldConfig.ts 改造指南

### 5.1 字段配置结构更新

```typescript
// 字段配置示例
const TICKET_DEFAULT_FIELDS: FieldDefinition[] = [
  // 私有字段（本地定义）
  { name: '工单编号', key: 'ticketNo', type: 'text', source: 'local', required: true },
  { name: '工单标题', key: 'title', type: 'text', source: 'local', required: true },
  { name: '工单描述', key: 'description', type: 'textarea', source: 'local' },
  
  // 来自 MasterDataService 的字段
  { name: '工单类型', key: 'type', type: 'select', source: 'api', 
    apiEndpoint: '/enums/ticket-types', apiService: 'masterdata' },
  { name: '优先级', key: 'priority', type: 'select', source: 'api',
    apiEndpoint: '/enums/priorities', apiService: 'masterdata' },
  { name: '工单状态', key: 'status', type: 'select', source: 'api',
    apiEndpoint: '/enums/ticket-statuses', apiService: 'masterdata' },
  
  // 来自 PersonService 的字段
  { name: '创建人', key: 'creatorName', type: 'text', source: 'api',
    apiEndpoint: '/persons', apiService: 'person' },
  { name: '指派人', key: 'assigneeName', type: 'text', source: 'api',
    apiEndpoint: '/persons', apiService: 'person' },
  
  // 系统字段
  { name: '备注', key: 'remark', type: 'textarea', source: 'system' },
]
```

### 5.2 API 服务封装

```typescript
// services/ApiServices.ts

// PersonService API
export const PersonService = {
  baseURL: 'http://localhost:5018/api',
  
  getPersons(params?: any) {
    return axios.get(`${this.baseURL}/persons`, { params })
  },
  
  getPersonById(id: number) {
    return axios.get(`${this.baseURL}/persons/${id}`)
  },
  
  getEnums(type: 'genders' | 'departments' | 'roles' | 'person-types') {
    return axios.get(`${this.baseURL}/enums/${type}`)
  },
  
  // 批量获取枚举值
  getBatchEnums(categories: string[]) {
    return axios.post(`${this.baseURL}/enums/batch`, { categories })
  }
}

// MasterDataService API
export const MasterDataService = {
  baseURL: 'http://localhost:5019/api',
  
  getBuildings() {
    return axios.get(`${this.baseURL}/buildings`)
  },
  
  getRoomsByBuilding(buildingId: number) {
    return axios.get(`${this.baseURL}/buildings/${buildingId}/rooms`)
  },
  
  getEnums(category: string) {
    return axios.get(`${this.baseURL}/enums/${category}`)
  },
  
  // 批量获取枚举值
  getBatchEnums(categories: string[]) {
    return axios.post(`${this.baseURL}/enums/batch`, { categories })
  }
}
```

### 5.3 缓存策略

```typescript
// 枚举值缓存
class EnumCache {
  private cache = new Map<string, { data: any; expireAt: number }>()
  private cacheDuration = 5 * 60 * 1000 // 5 分钟

  async getEnums(service: 'person' | 'masterdata', category: string) {
    const key = `${service}:${category}`
    const cached = this.cache.get(key)
    
    if (cached && cached.expireAt > Date.now()) {
      return cached.data
    }
    
    // 重新获取
    const data = service === 'person' 
      ? await PersonService.getEnums(category)
      : await MasterDataService.getEnums(category)
    
    this.cache.set(key, { data, expireAt: Date.now() + this.cacheDuration })
    return data
  }
}
```

---

## 6. 共享字段统计

### 6.1 按模块统计

| 模块 | 总字段数 | 私有字段 | PersonService | MasterDataService | 系统字段 |
|------|---------|---------|--------------|-------------------|---------|
| ticket | 15 | 4 | 3 (creator, assignee, contact) | 5 (type, priority, status, location) | 3 |
| device | 13 | 5 | 1 (responsible) | 3 (deviceType, inspectionCycle, location) | 4 |
| material | 13 | 6 | 0 | 3 (category, unit, location) | 4 |
| contract | 14 | 6 | 1 (responsible) | 3 (contractType, paymentMethod, paymentStatus) | 4 |
| complaint | 15 | 4 | 3 (complainant, phone, handler) | 5 (type, source, priority, roomNo) | 3 |
| key | 10 | 3 | 1 (borrower) | 2 (keyType, location) | 4 |
| visitor | 13 | 3 | 2 (phone, visitedResident) | 2 (visitorType, roomNo) | 6 |
| resident | 12 | 0 | 12 (全部来自 PersonService) | 0 | 0 |

### 6.2 按服务统计

| 来源服务 | 字段数 | 说明 |
|---------|--------|------|
| PersonService | 35+ | 人员相关所有字段 |
| MasterDataService | 30+ | 枚举值 + 楼栋房间 |
| 系统 | 10+ | createdAt, updatedAt, remark |
| 各微服务私有 | 50+ | 编号、业务特有字段 |

---

## 7. 实施建议

### 7.1 前端改造步骤

```
Phase 1: 基础改造（第1周）
  1. 统一 fieldConfig.ts 结构，添加 source 字段
  2. 实现 PersonService 和 MasterDataService 的 API 封装
  3. 实现枚举值缓存机制

Phase 2: 字段改造（第2周）
  1. 工单模块字段改接 API
  2. 设备模块字段改接 API
  3. 其他模块按需改造

Phase 3: 优化（第3周）
  1. 实现批量获取枚举值（减少 HTTP 请求）
  2. 优化缓存策略
  3. 降级方案（API 不可用时的 fallback）
```

### 7.2 后端配合

```
1. PersonService 需要实现：
   - GET /api/enums/{type} 批量接口（已规划）
   
2. MasterDataService 需要实现：
   - GET /api/enums/{category} 单个获取
   - POST /api/enums/batch 批量获取（已规划）
   
3. 各微服务需要实现：
   - 在业务表中存储 PersonId 而非冗余姓名
   - 提供关联查询时自动填充姓名（JOIN 或服务调用）
```

---

## 8. 风险与决策点

### 8.1 风险

| 风险 | 影响 | 应对 |
|------|------|------|
| API 调用延迟 | 页面加载变慢 | 本地缓存 + 骨架屏 |
| 服务不可用 | 下拉框无法加载 | 降级到硬编码 |
| 服务间数据不一致 | 人员已删除但业务表还有 ID | 定期同步 + 软删除 |
| 枚举值变更 | 旧数据兼容问题 | 版本控制 + 数据迁移 |

### 8.2 决策点

```
决策 1：是否在业务表中存储人员姓名？
        → 当前方案：存储 PersonId，定期同步姓名到冗余字段
        → 或者：查询时通过 JOIN 或服务调用获取

决策 2：枚举值本地缓存时间？
        → 当前方案：5 分钟
        → 可根据实际调整

决策 3：服务不可用时的 fallback？
        → 当前方案：使用本地硬编码作为降级
        → 恢复后自动切换回 API
```

---

**文档版本**：v2.0
**作者**：软件架构师（虚拟）
**审核**：软件负责人
**状态**：待评审