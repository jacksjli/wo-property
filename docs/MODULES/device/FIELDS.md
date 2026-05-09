# 设备模块字段定义

> **版本**：v1.0
> **模块**：device
> **最后更新**：2026-05-05

---

## 1. 字段分类表

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

---

## 2. 共享字段来源汇总

### 2.1 MasterDataService 提供

| 字段名 | API 端点 | 说明 |
|--------|----------|------|
| deviceType | GET /api/enums/device-types | 设备类型 |
| inspectionCycle | GET /api/enums/inspection-cycles | 巡检周期 |
| location | - | 位置信息 |

### 2.2 PersonService 提供

| 字段名 | API 端点 | 说明 |
|--------|----------|------|
| responsible | GET /api/persons/{id} | 负责人姓名 |

---

## 3. 枚举值来源

### 3.1 设备类型 (device-types)

```json
[
  { "code": "Elevator", "name": "电梯" },
  { "code": "Fire", "name": "消防" },
  { "code": "CCTV", "name": "监控" },
  { "code": "AccessControl", "name": "门禁" },
  { "code": "AC", "name": "空调" },
  { "code": "Lighting", "name": "照明" },
  { "code": "Plumbing", "name": "给排水" },
  { "code": "Power", "name": "供电" },
  { "code": "Other", "name": "其他" }
]
```

### 3.2 巡检周期 (inspection-cycles)

```json
[
  { "code": "Daily", "name": "每日" },
  { "code": "Weekly", "name": "每周" },
  { "code": "Monthly", "name": "每月" },
  { "code": "Quarterly", "name": "每季度" },
  { "code": "Yearly", "name": "每年" }
]
```

---

**文档版本**：v1.0
**作者**：前端工程师
**审核**：软件架构师
**状态**：待评审
