# WO 物业管理软件 - 端口分配表 (2026-05-06)

## 基础服务
| 端口 | 服务 | 状态 |
|------|------|------|
| 5000 | API Gateway | ✅ 运行中 |
| 5006 | AuthService | ✅ 运行中 |
| 5018 | PersonService | ✅ 运行中 |
| 5019 | MasterDataService | ✅ 运行中 |

## 核心业务服务
| 端口 | 服务 | 状态 |
|------|------|------|
| 5002 | TicketService | ✅ 运行中 |
| 5003 | DispatchService | ✅ 运行中 |
| 5004 | MaterialService | ✅ 运行中 |
| 5005 | NotificationService | ✅ 运行中 |
| 5007 | DeviceService | ✅ 运行中 |
| 5008 | ContractService | ✅ 运行中 |
| 5009 | FinanceService | ✅ 运行中 |
| 5010 | InspectionService | ✅ 运行中 |
| 5011 | ComplaintService | ✅ 运行中 |
| 5012 | KeyService | ✅ 运行中 |
| 5013 | VisitorService | ✅ 运行中 |
| 5014 | StatisticsService | ✅ 运行中 |
| 5015 | MobileService | ✅ 运行中 |

## 新增服务 (2026-05-06)
| 端口 | 服务 | 功能 |
|------|------|------|
| 5001 | AccessControlService | 门禁/权限控制 |
| 5016 | AnnouncementService | 公告管理 |
| 5021 | CleaningService | 清洁管理 |
| 5022 | CommunityService | 社区管理 |
| 5023 | DeliveryService | 配送管理 |
| 5024 | ExpressService | 快递管理 |
| 5025 | ParkingService | 车位管理 |
| 5026 | PaymentService | 缴费管理 |
| 5027 | ProjectConfigService | 项目配置 |
| 5028 | RenovationService | 装修管理 |

## 前端
| 端口 | 服务 | 状态 |
|------|------|------|
| 5173 | admin-portal | ✅ 运行中 |

## 待定
- GatewayService (完整版) - 需要与现有 APIGateway 整合或替换