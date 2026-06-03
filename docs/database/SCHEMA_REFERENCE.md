# WO Property 数据库 Schema 对照表
# Database Schema as Source of Truth
# 最后更新: 2026-05-25

## 原则

**数据库是唯一真相来源 (Database is the source of truth)**

所有代码模型、TenantDbContext 配置、设计文档都必须与本文件一致。

---

## wo_property 数据库 - 完整表结构

### 核心业务表

#### Tickets (工单)
| 列名 | 类型 | 说明 |
|------|------|------|
| Id | int | PK |
| TicketNumber | varchar(20) | 工单编号，UNI |
| Title | varchar(100) | 标题 |
| Description | text | 描述 |
| TicketType | varchar(20) | 类型 |
| category | varchar(50) | 分类 |
| ticket_type_id | bigint | 工单类型ID |
| Priority | varchar(10) | 优先级 |
| color | varchar(10) | 颜色 |
| Location | varchar(200) | 位置 |
| location_detail | varchar(500) | 位置详情 |
| area_id | bigint | 区域ID |
| images | text | 图片 |
| rating | int | 评分 |
| Status | varchar(20) | 状态 |
| current_role | varchar(20) | 当前角色 |
| escalation_level | int | 升级级别 |
| dispatch_status | varchar(20) | 派单状态 |
| tenant_id | bigint | 租户ID |

#### Contracts (合同)
| 列名 | 类型 | 说明 |
|------|------|------|
| Id | int | PK |
| ContractNumber | varchar(30) | 合同编号，UNI |
| ContractName | varchar(100) | 合同名称 |
| ContractType | varchar(20) | 合同类型 |
| PartyA | varchar(100) | 甲方 |
| PartyB | varchar(100) | 乙方 |
| SignedDate | date | 签订日期 |
| StartDate | date | 开始日期 |
| EndDate | date | 结束日期 |
| Amount | decimal(14,2) | 金额 |
| Status | varchar(20) | 状态 |
| AttachmentUrl | varchar(255) | 附件URL |
| Remarks | text | 备注 |

#### keys (钥匙)
| 列名 | 类型 | 说明 |
|------|------|------|
| Id | int | PK |
| KeyNo | varchar(50) | 钥匙编号，UNI |
| Name | varchar(100) | 名称 |
| Type | varchar(20) | 类型 |
| Location | varchar(100) | 位置 |
| Building | varchar(30) | 楼栋 |
| Floor | varchar(20) | 楼层 |
| DoorNo | varchar(30) | 门号 |
| Quantity | int | 数量 |
| Status | varchar(20) | 状态 |
| Holder | varchar(50) | 持有人 |
| HolderPhone | varchar(20) | 持有人电话 |
| LastBorrowTime | datetime | 最后借用时间 |
| BorrowCount | int | 借用次数 |
| Photo | varchar(255) | 照片 |
| Remark | text | 备注 |

#### Visitors (访客)
| 列名 | 类型 | 说明 |
|------|------|------|
| Id | int | PK |
| VisitorName | varchar(50) | 访客姓名 |
| VisitorPhone | varchar(20) | 访客电话 |
| IdCardNumber | varchar(30) | 身份证号 |
| VisitPurpose | varchar(50) | 访问目的 |
| VisitDate | date | 访问日期 |
| VisitTime | time | 访问时间 |
| LeaveTime | time | 离开时间 |
| BuildingId | int | 楼栋ID |
| RoomId | int | 房间ID |
| HostName | varchar(50) | 主人姓名 |
| HostPhone | varchar(20) | 主人电话 |
| Status | varchar(20) | 状态 |
| Remarks | text | 备注 |

#### FinanceRecords (财务记录)
| 列名 | 类型 | 说明 |
|------|------|------|
| Id | int | PK |
| RecordNumber | varchar(30) | 记录编号，UNI |
| Type | varchar(10) | 类型 |
| Category | varchar(30) | 分类 |
| Amount | decimal(14,2) | 金额 |
| Balance | decimal(14,2) | 余额 |
| PaymentMethod | varchar(20) | 支付方式 |
| RecordDate | date | 记录日期 |
| Handler | varchar(50) | 经办人 |
| RelatedParty | varchar(100) | 相关方 |
| ContractNo | varchar(30) | 合同编号 |
| BillNo | varchar(30) | 账单编号 |
| Description | text | 描述 |
| ReceiptNo | varchar(30) | 收据编号 |
| Status | varchar(20) | 状态 |
| Remarks | text | 备注 |

#### PaymentRecords (支付记录)
| 列名 | 类型 | 说明 |
|------|------|------|
| Id | int | PK |
| PaymentNumber | varchar(20) | 支付编号，UNI |
| ResidentId | int | 住户ID |
| RoomId | int | 房间ID |
| PaymentType | varchar(20) | 支付类型 |
| Amount | decimal(10,2) | 金额 |
| PeriodStart | date | 开始期间 |
| PeriodEnd | date | 结束期间 |
| DueDate | date | 到期日期 |
| PaidDate | date | 支付日期 |
| Status | varchar(20) | 状态 |
| PaymentMethod | varchar(20) | 支付方式 |
| TransactionId | varchar(50) | 交易ID |
| Remarks | text | 备注 |

#### InspectionRecords (巡检记录)
| 列名 | 类型 | 说明 |
|------|------|------|
| Id | int | PK |
| InspectionTitle | varchar(100) | 巡检标题 |
| BuildingId | int | 楼栋ID |
| InspectionArea | varchar(100) | 巡检区域 |
| InspectorName | varchar(50) | 巡检员姓名 |
| InspectionDate | date | 巡检日期 |
| InspectionTime | time | 巡检时间 |
| Status | varchar(20) | 状态 |
| Result | varchar(20) | 结果 |
| Findings | text | 发现 |
| NextInspectionDate | date | 下次巡检日期 |
| Remarks | text | 备注 |

#### Materials (物料)
| 列名 | 类型 | 说明 |
|------|------|------|
| Id | int | PK |
| MaterialNo | varchar(50) | 物料编号 |
| Name | varchar(100) | 名称 |
| Category | varchar(30) | 分类 |
| Spec | varchar(100) | 规格 |
| Unit | varchar(20) | 单位 |
| Quantity | int | 数量 |
| MinQuantity | int | 最小数量 |
| Price | decimal(10,2) | 价格 |
| Location | varchar(100) | 位置 |
| Status | varchar(20) | 状态 |
| Supplier | varchar(100) | 供应商 |
| PurchaseDate | date | 购买日期 |
| ExpirationDate | date | 过期日期 |
| LastCheckDate | date | 最后检查日期 |
| NextCheckDate | date | 下次检查日期 |
| CheckCycle | varchar(20) | 检查周期 |
| Remark | text | 备注 |

#### Devices (设备)
| 列名 | 类型 | 说明 |
|------|------|------|
| Id | int | PK |
| DeviceCode | varchar(30) | 设备编号，UNI |
| DeviceName | varchar(100) | 设备名称 |
| DeviceTypeId | int | 设备类型ID |
| BuildingId | int | 楼栋ID |
| Floor | int | 楼层 |
| Location | varchar(100) | 位置 |
| Status | varchar(20) | 状态 |
| LastMaintenanceDate | date | 最后维护日期 |
| NextMaintenanceDate | date | 下次维护日期 |
| PurchaseDate | date | 购买日期 |
| SupplierId | int | 供应商ID |
| Remarks | text | 备注 |

#### Personnel (人员)
| 列名 | 类型 | 说明 |
|------|------|------|
| Id | int | PK |
| EmployeeNo | varchar(30) | 员工编号，UNI |
| Name | varchar(50) | 姓名 |
| Avatar | varchar(255) | 头像 |
| Gender | varchar(20) | 性别 |
| Birthday | date | 生日 |
| IdCard | varchar(30) | 身份证 |
| Phone | varchar(20) | 电话 |
| Email | varchar(100) | 邮箱 |
| Address | varchar(200) | 地址 |
| Education | varchar(20) | 学历 |
| GraduateSchool | varchar(100) | 毕业学校 |
| Major | varchar(50) | 专业 |
| Role | varchar(30) | 角色 |
| DepartmentId | int | 部门ID |
| DepartmentName | varchar(50) | 部门名称 |
| Position | varchar(50) | 职位 |
| EmploymentType | varchar(20) | 雇佣类型 |
| HireDate | date | 入职日期 |
| ContractStart | date | 合同开始 |

#### Buildings (楼栋)
| 列名 | 类型 | 说明 |
|------|------|------|
| Id | int | PK |
| Name | varchar(100) | 名称 |
| Code | varchar(50) | 编号，UNI |
| Description | varchar(500) | 描述 |
| Address | varchar(200) | 地址 |
| TotalFloors | int | 总楼层 |
| TotalUnits | int | 总单元 |
| Area | varchar(100) | 区域 |
| Status | varchar(20) | 状态 |

#### Rooms (房间)
| 列名 | 类型 | 说明 |
|------|------|------|
| Id | int | PK |
| BuildingId | int | 楼栋ID |
| Floor | varchar(10) | 楼层 |
| Unit | varchar(20) | 单元 |
| RoomNumber | varchar(50) | 房间号 |
| RoomType | varchar(20) | 房间类型 |
| Area | decimal(10,2) | 面积 |
| Status | varchar(20) | 状态 |

#### Residents (住户)
| 列名 | 类型 | 说明 |
|------|------|------|
| Id | int | PK |
| Name | varchar(50) | 姓名 |
| Phone | varchar(20) | 电话 |
| IdCardNumber | varchar(30) | 身份证号 |
| BuildingId | int | 楼栋ID |
| RoomId | int | 房间ID |
| ResidentType | varchar(20) | 住户类型 |
| CheckInDate | date | 入住日期 |
| Status | varchar(20) | 状态 |
| Remarks | text | 备注 |

#### complaints (投诉)
| 列名 | 类型 | 说明 |
|------|------|------|
| Id | int | PK |
| ComplaintNo | varchar(50) | 投诉编号 |
| Title | varchar(200) | 标题 |
| Type | varchar(30) | 类型 |
| Source | varchar(20) | 来源 |
| Priority | varchar(20) | 优先级 |
| Description | text | 描述 |
| ComplainantName | varchar(50) | 投诉人姓名 |
| ComplainantPhone | varchar(20) | 投诉人电话 |
| ComplainantRoom | varchar(30) | 投诉人房间 |
| Location | varchar(100) | 位置 |
| HandlerName | varchar(50) | 处理人姓名 |
| Deadline | date | 截止日期 |
| Remark | text | 备注 |
| HandleStatus | varchar(20) | 处理状态 |
| HandleProgress | text | 处理进度 |
| Feedback | text | 反馈 |
| Rating | int | 评分 |

#### dispatch_records (派单记录)
| 列名 | 类型 | 说明 |
|------|------|------|
| id | bigint | PK |
| ticket_id | bigint | 工单ID |
| ticket_code | varchar(50) | 工单编号 |
| dispatch_time | datetime | 派单时间 |
| from_person_id | int | 从人员ID |
| from_person_name | varchar(50) | 从人员姓名 |
| to_person_id | int | 到人员ID |
| to_person_name | varchar(50) | 到人员姓名 |
| status | varchar(20) | 状态 |
| source | varchar(20) | 来源 |
| workflow_instance_id | varchar(50) | 工作流实例ID |
| completed_at | datetime | 完成时间 |
| confirmed_at | datetime | 确认时间 |
| confirmed_by | int | 确认人ID |
| confirmed_by_name | varchar(50) | 确认人姓名 |
| rating_id | bigint | 评分ID |
| tenant_code | varchar(50) | 租户代码 |
| project_id | int | 项目ID |

---

## 字段映射规则

### 命名规范
1. 数据库列名使用 PascalCase 或 snake_case（见上表）
2. C# 模型属性名必须与数据库列名完全匹配
3. 使用 `HasColumnName("exact_db_column_name")` 显式映射

### 类型映射
| 数据库类型 | C# 类型 |
|-----------|--------|
| varchar(n) | string |
| int | int |
| bigint | long |
| decimal(m,n) | decimal |
| datetime | DateTime? |
| date | DateTime? |
| time | TimeSpan? |
| text | string? |
| tinyint(1) | bool |

### 枚举处理
- **不要**在数据库中存储枚举名
- 数据库存储 string 值（如 "active", "pending", "completed"）
- C# 模型使用 `string` 类型，不要用 `enum`
- 如需强类型枚举，在 Controller 层转换

---

## 服务对应表

| 服务 | 端口 | 使用的表 |
|------|------|---------|
| TicketService | 5102 | Tickets |
| DispatchService | 5241 | dispatch_records, dispatch_rules, ticket_dispatch_mapping |
| ContractService | 5501 | Contracts |
| PersonService | 5018 | Personnel, Users |
| MaterialService | 5504 | materials, MaterialCategories |
| DeviceService | 5530 | Devices, DeviceTypes |
| VisitorService | 5513 | Visitors |
| KeyService | 5512 | keys |
| InspectionService | 5510 | InspectionRecords, inspection_plans, inspection_tasks, inspection_issues |
| FinanceService | 5509 | FinanceRecords, PaymentRecords |
| StatisticsService | 5250 | metric_snapshots, MetricSnapshots, trend_records |
| CenterService | 5016 | center_db.projects (不是 wo_property) |
| MasterDataService | 5019 | FieldDefinitions, ModuleFields |

---

## 验证检查清单

每次修改代码前必须检查：

- [ ] Model 属性名 vs 数据库列名 完全一致
- [ ] Model 类型 vs 数据库列类型 匹配
- [ ] 枚举值（如有）vs 数据库存储值 一致
- [ ] 可空字段正确标记为 nullable (?)
- [ ] TenantDbContext 的 HasColumnName 正确

---

## 相关文件

- 本文件: `docs/database/SCHEMA_REFERENCE.md`
- 服务重构记录: `docs/design/SERVICE_REFACTOR_2026-05-25.md`
- 审计日志: `docs/AUDIT_LOG.md`