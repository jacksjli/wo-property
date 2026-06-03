/**
 * WO Property Management - PM2 进程管理配置
 * 
 * 使用方式：
 *   pm2 start ecosystem.config.js
 * 
 * 自动检测当前脚本所在目录作为项目根目录
 * 复制到任何 Mac 都能直接运行，无需手动修改路径
 */

const path = require('path')

// 自动检测项目根目录（脚本所在目录）
const PROJECT_ROOT = path.resolve(__dirname)

console.log(`[PM2] Project root: ${PROJECT_ROOT}`)

module.exports = {
  apps: [
    // ========================================================================
    // 核心服务（必须）
    // ========================================================================
    {
      name: 'GatewayService',
      namespace: 'wo-property',
      script: '/usr/local/share/dotnet/dotnet',
      args: `run --project ${PROJECT_ROOT}/src/WO.Property.GatewayService/WO.Property.GatewayService.csproj -- --urls="http://0.0.0.0:5000"`,
      cwd: PROJECT_ROOT + "/src/WO.Property.GatewayService",
      interpreter: 'none',
      autorestart: true,
      max_restarts: 10,
      min_uptime: '10s',
      restarts: 0,
      max_memory_restart: '500M',
      env: {
        ASPNETCORE_ENVIRONMENT: 'Development'
      },
      error_file: '/tmp/wo-gateway-err.log',
      out_file: '/tmp/wo-gateway-out.log',
      log_date_format: 'YYYY-MM-DD HH:mm:ss',
      instances: 1,
      execute_command: true
    },
    {
      name: 'AuthService',
      namespace: 'wo-property',
      script: '/usr/local/share/dotnet/dotnet',
      args: `run --project ${PROJECT_ROOT}/src/WO.Property.AuthService/WO.Property.AuthService.csproj -- --urls="http://0.0.0.0:5106"`,
      cwd: PROJECT_ROOT + "/src/WO.Property.GatewayService",
      interpreter: 'none',
      autorestart: true,
      max_restarts: 10,
      min_uptime: '10s',
      max_memory_restart: '300M',
      env: {
        ASPNETCORE_ENVIRONMENT: 'Development'
      },
      error_file: '/tmp/wo-auth-err.log',
      out_file: '/tmp/wo-auth-out.log',
      log_date_format: 'YYYY-MM-DD HH:mm:ss',
      instances: 1,
      execute_command: true
    },
    {
      name: 'PersonService',
      namespace: 'wo-property',
      script: '/usr/local/share/dotnet/dotnet',
      args: `run --project ${PROJECT_ROOT}/src/WO.Property.PersonService/WO.Property.PersonService.csproj -- --urls="http://0.0.0.0:5018"`,
      cwd: PROJECT_ROOT + "/src/WO.Property.GatewayService",
      interpreter: 'none',
      autorestart: true,
      max_restarts: 10,
      min_uptime: '10s',
      max_memory_restart: '300M',
      env: {
        ASPNETCORE_ENVIRONMENT: 'Development'
      },
      error_file: '/tmp/wo-person-err.log',
      out_file: '/tmp/wo-person-out.log',
      log_date_format: 'YYYY-MM-DD HH:mm:ss',
      instances: 1,
      execute_command: true
    },
    {
      name: 'MasterDataService',
      namespace: 'wo-property',
      script: '/usr/local/share/dotnet/dotnet',
      args: `run --project ${PROJECT_ROOT}/src/WO.Property.MasterDataService/WO.Property.MasterDataService.csproj -- --urls="http://0.0.0.0:5019"`,
      cwd: PROJECT_ROOT + "/src/WO.Property.GatewayService",
      interpreter: 'none',
      autorestart: true,
      max_restarts: 10,
      min_uptime: '10s',
      max_memory_restart: '300M',
      env: {
        ASPNETCORE_ENVIRONMENT: 'Development'
      },
      error_file: '/tmp/wo-master-err.log',
      out_file: '/tmp/wo-master-out.log',
      log_date_format: 'YYYY-MM-DD HH:mm:ss',
      instances: 1,
      execute_command: true
    },
    {
      name: 'TicketService',
      namespace: 'wo-property',
      script: '/usr/local/share/dotnet/dotnet',
      args: `run --project ${PROJECT_ROOT}/src/WO.Property.TicketService/WO.Property.TicketService.csproj -- --urls="http://0.0.0.0:5102"`,
      cwd: PROJECT_ROOT + "/src/WO.Property.GatewayService",
      interpreter: 'none',
      autorestart: true,
      max_restarts: 10,
      min_uptime: '10s',
      max_memory_restart: '400M',
      env: {
        ASPNETCORE_ENVIRONMENT: 'Development'
      },
      error_file: '/tmp/wo-ticket-err.log',
      out_file: '/tmp/wo-ticket-out.log',
      log_date_format: 'YYYY-MM-DD HH:mm:ss',
      instances: 1,
      execute_command: true
    },
    {
      name: 'DispatchService',
      namespace: 'wo-property',
      script: '/usr/local/share/dotnet/dotnet',
      args: `run --project ${PROJECT_ROOT}/src/WO.Property.DispatchService/WO.Property.DispatchService.csproj -- --urls="http://0.0.0.0:5241"`,
      cwd: PROJECT_ROOT + "/src/WO.Property.GatewayService",
      interpreter: 'none',
      autorestart: true,
      max_restarts: 10,
      min_uptime: '10s',
      max_memory_restart: '300M',
      env: {
        ASPNETCORE_ENVIRONMENT: 'Development'
      },
      error_file: '/tmp/wo-dispatch-err.log',
      out_file: '/tmp/wo-dispatch-out.log',
      log_date_format: 'YYYY-MM-DD HH:mm:ss',
      instances: 1,
      execute_command: true
    },
    {
      name: 'PaymentService',
      namespace: 'wo-property',
      script: '/usr/local/share/dotnet/dotnet',
      args: `run --project ${PROJECT_ROOT}/src/WO.Property.PaymentService/WO.Property.PaymentService.csproj -- --urls="http://0.0.0.0:5109"`,
      cwd: PROJECT_ROOT + "/src/WO.Property.GatewayService",
      interpreter: 'none',
      autorestart: true,
      max_restarts: 10,
      min_uptime: '10s',
      max_memory_restart: '300M',
      env: {
        ASPNETCORE_ENVIRONMENT: 'Development'
      },
      error_file: '/tmp/wo-payment-err.log',
      out_file: '/tmp/wo-payment-out.log',
      log_date_format: 'YYYY-MM-DD HH:mm:ss',
      instances: 1,
      execute_command: true
    },
    // ========================================================================
    // 扩展业务服务
    // ========================================================================
    {
      name: 'MaterialService',
      namespace: 'wo-property',
      script: '/usr/local/share/dotnet/dotnet',
      args: `run --project ${PROJECT_ROOT}/src/WO.Property.MaterialService/WO.Property.MaterialService.csproj -- --urls="http://0.0.0.0:5504"`,
      cwd: PROJECT_ROOT + "/src/WO.Property.GatewayService",
      interpreter: 'none',
      autorestart: true,
      max_restarts: 10,
      min_uptime: '10s',
      max_memory_restart: '300M',
      env: {
        ASPNETCORE_ENVIRONMENT: 'Development'
      },
      error_file: '/tmp/wo-material-err.log',
      out_file: '/tmp/wo-material-out.log',
      log_date_format: 'YYYY-MM-DD HH:mm:ss',
      instances: 1,
      execute_command: true
    },
    {
      name: 'DeviceService',
      namespace: 'wo-property',
      script: '/usr/local/share/dotnet/dotnet',
      args: `run --project ${PROJECT_ROOT}/src/WO.Property.DeviceService/WO.Property.DeviceService.csproj -- --urls="http://0.0.0.0:5530"`,
      cwd: PROJECT_ROOT + "/src/WO.Property.GatewayService",
      interpreter: 'none',
      autorestart: true,
      max_restarts: 10,
      min_uptime: '10s',
      max_memory_restart: '300M',
      env: {
        ASPNETCORE_ENVIRONMENT: 'Development'
      },
      error_file: '/tmp/wo-device-err.log',
      out_file: '/tmp/wo-device-out.log',
      log_date_format: 'YYYY-MM-DD HH:mm:ss',
      instances: 1,
      execute_command: true
    },
    {
      name: 'ContractService',
      namespace: 'wo-property',
      script: '/usr/local/share/dotnet/dotnet',
      args: `run --project ${PROJECT_ROOT}/src/WO.Property.ContractService/WO.Property.ContractService.csproj -- --urls="http://0.0.0.0:5501"`,
      cwd: PROJECT_ROOT + "/src/WO.Property.GatewayService",
      interpreter: 'none',
      autorestart: true,
      max_restarts: 10,
      min_uptime: '10s',
      max_memory_restart: '300M',
      env: {
        ASPNETCORE_ENVIRONMENT: 'Development'
      },
      error_file: '/tmp/wo-contract-err.log',
      out_file: '/tmp/wo-contract-out.log',
      log_date_format: 'YYYY-MM-DD HH:mm:ss',
      instances: 1,
      execute_command: true
    },
    {
      name: 'InspectionService',
      namespace: 'wo-property',
      script: '/usr/local/share/dotnet/dotnet',
      args: `run --project ${PROJECT_ROOT}/src/WO.Property.InspectionService/WO.Property.InspectionService.csproj -- --urls="http://0.0.0.0:5510"`,
      cwd: PROJECT_ROOT + "/src/WO.Property.GatewayService",
      interpreter: 'none',
      autorestart: true,
      max_restarts: 10,
      min_uptime: '10s',
      max_memory_restart: '300M',
      env: {
        ASPNETCORE_ENVIRONMENT: 'Development'
      },
      error_file: '/tmp/wo-inspection-err.log',
      out_file: '/tmp/wo-inspection-out.log',
      log_date_format: 'YYYY-MM-DD HH:mm:ss',
      instances: 1,
      execute_command: true
    },
    {
      name: 'VisitorService',
      namespace: 'wo-property',
      script: '/usr/local/share/dotnet/dotnet',
      args: `run --project ${PROJECT_ROOT}/src/WO.Property.VisitorService/WO.Property.VisitorService.csproj -- --urls="http://0.0.0.0:5513"`,
      cwd: PROJECT_ROOT + "/src/WO.Property.GatewayService",
      interpreter: 'none',
      autorestart: true,
      max_restarts: 10,
      min_uptime: '10s',
      max_memory_restart: '300M',
      env: {
        ASPNETCORE_ENVIRONMENT: 'Development'
      },
      error_file: '/tmp/wo-visitor-err.log',
      out_file: '/tmp/wo-visitor-out.log',
      log_date_format: 'YYYY-MM-DD HH:mm:ss',
      instances: 1,
      execute_command: true
    },
    {
      name: 'ComplaintService',
      namespace: 'wo-property',
      script: '/usr/local/share/dotnet/dotnet',
      args: `run --project ${PROJECT_ROOT}/src/WO.Property.ComplaintService/WO.Property.ComplaintService.csproj -- --urls="http://0.0.0.0:5201"`,
      cwd: PROJECT_ROOT + "/src/WO.Property.GatewayService",
      interpreter: 'none',
      autorestart: true,
      max_restarts: 10,
      min_uptime: '10s',
      max_memory_restart: '300M',
      env: {
        ASPNETCORE_ENVIRONMENT: 'Development'
      },
      error_file: '/tmp/wo-complaint-err.log',
      out_file: '/tmp/wo-complaint-out.log',
      log_date_format: 'YYYY-MM-DD HH:mm:ss',
      instances: 1,
      execute_command: true
    },
    {
      name: 'StatisticsService',
      namespace: 'wo-property',
      script: '/usr/local/share/dotnet/dotnet',
      args: `run --project ${PROJECT_ROOT}/src/WO.Property.StatisticsService/WO.Property.StatisticsService.csproj -- --urls="http://0.0.0.0:5250"`,
      cwd: PROJECT_ROOT + "/src/WO.Property.GatewayService",
      interpreter: 'none',
      autorestart: true,
      max_restarts: 10,
      min_uptime: '10s',
      max_memory_restart: '300M',
      env: {
        ASPNETCORE_ENVIRONMENT: 'Development'
      },
      error_file: '/tmp/wo-statistics-err.log',
      out_file: '/tmp/wo-statistics-out.log',
      log_date_format: 'YYYY-MM-DD HH:mm:ss',
      instances: 1,
      execute_command: true
    },
    {
      name: 'CleaningService',
      namespace: 'wo-property',
      script: '/usr/local/share/dotnet/dotnet',
      args: `run --project ${PROJECT_ROOT}/src/WO.Property.CleaningService/WO.Property.CleaningService.csproj -- --urls="http://0.0.0.0:5516"`,
      cwd: PROJECT_ROOT + "/src/WO.Property.GatewayService",
      interpreter: 'none',
      autorestart: true,
      max_restarts: 10,
      min_uptime: '10s',
      max_memory_restart: '300M',
      env: {
        ASPNETCORE_ENVIRONMENT: 'Development'
      },
      error_file: '/tmp/wo-cleaning-err.log',
      out_file: '/tmp/wo-cleaning-out.log',
      log_date_format: 'YYYY-MM-DD HH:mm:ss',
      instances: 1,
      execute_command: true
    },
    {
      name: 'ExpressService',
      namespace: 'wo-property',
      script: '/usr/local/share/dotnet/dotnet',
      args: `run --project ${PROJECT_ROOT}/src/WO.Property.ExpressService/WO.Property.ExpressService.csproj -- --urls="http://0.0.0.0:5517"`,
      cwd: PROJECT_ROOT + "/src/WO.Property.GatewayService",
      interpreter: 'none',
      autorestart: true,
      max_restarts: 10,
      min_uptime: '10s',
      max_memory_restart: '300M',
      env: {
        ASPNETCORE_ENVIRONMENT: 'Development'
      },
      error_file: '/tmp/wo-express-err.log',
      out_file: '/tmp/wo-express-out.log',
      log_date_format: 'YYYY-MM-DD HH:mm:ss',
      instances: 1,
      execute_command: true
    },
    {
      name: 'RenovationService',
      namespace: 'wo-property',
      script: '/usr/local/share/dotnet/dotnet',
      args: `run --project ${PROJECT_ROOT}/src/WO.Property.RenovationService/WO.Property.RenovationService.csproj -- --urls="http://0.0.0.0:5521"`,
      cwd: PROJECT_ROOT + "/src/WO.Property.GatewayService",
      interpreter: 'none',
      autorestart: true,
      max_restarts: 10,
      min_uptime: '10s',
      max_memory_restart: '300M',
      env: {
        ASPNETCORE_ENVIRONMENT: 'Development'
      },
      error_file: '/tmp/wo-renovation-err.log',
      out_file: '/tmp/wo-renovation-out.log',
      log_date_format: 'YYYY-MM-DD HH:mm:ss',
      instances: 1,
      execute_command: true
    },
    {
      name: 'ParkingService',
      namespace: 'wo-property',
      script: '/usr/local/share/dotnet/dotnet',
      args: `run --project ${PROJECT_ROOT}/src/WO.Property.ParkingService/WO.Property.ParkingService.csproj -- --urls="http://0.0.0:0:5525"`,
      cwd: PROJECT_ROOT + "/src/WO.Property.GatewayService",
      interpreter: 'none',
      autorestart: true,
      max_restarts: 10,
      min_uptime: '10s',
      max_memory_restart: '300M',
      env: {
        ASPNETCORE_ENVIRONMENT: 'Development'
      },
      error_file: '/tmp/wo-parking-err.log',
      out_file: '/tmp/wo-parking-out.log',
      log_date_format: 'YYYY-MM-DD HH:mm:ss',
      instances: 1,
      execute_command: true
    },
    {
      name: 'CommunityService',
      namespace: 'wo-property',
      script: '/usr/local/share/dotnet/dotnet',
      args: `run --project ${PROJECT_ROOT}/src/WO.Property.CommunityService/WO.Property.CommunityService.csproj -- --urls="http://0.0.0.0:5522"`,
      cwd: PROJECT_ROOT + "/src/WO.Property.GatewayService",
      interpreter: 'none',
      autorestart: true,
      max_restarts: 10,
      min_uptime: '10s',
      max_memory_restart: '300M',
      env: {
        ASPNETCORE_ENVIRONMENT: 'Development'
      },
      error_file: '/tmp/wo-community-err.log',
      out_file: '/tmp/wo-community-out.log',
      log_date_format: 'YYYY-MM-DD HH:mm:ss',
      instances: 1,
      execute_command: true
    },
    {
      name: 'MobileService',
      namespace: 'wo-property',
      script: '/usr/local/share/dotnet/dotnet',
      args: `run --project ${PROJECT_ROOT}/src/WO.Property.MobileService/WO.Property.MobileService.csproj -- --urls="http://0.0.0.0:5526"`,
      cwd: PROJECT_ROOT + "/src/WO.Property.GatewayService",
      interpreter: 'none',
      autorestart: true,
      max_restarts: 10,
      min_uptime: '10s',
      max_memory_restart: '300M',
      env: {
        ASPNETCORE_ENVIRONMENT: 'Development'
      },
      error_file: '/tmp/wo-mobile-err.log',
      out_file: '/tmp/wo-mobile-out.log',
      log_date_format: 'YYYY-MM-DD HH:mm:ss',
      instances: 1,
      execute_command: true
    },
    // ========================================================================
    // 前端
    // ========================================================================
    {
      name: 'Frontend',
      namespace: 'wo-property',
      script: 'npm',
      args: 'run dev',
      cwd: `${PROJECT_ROOT}/src/admin-portal`,
      interpreter: 'none',
      autorestart: true,
      max_restarts: 10,
      min_uptime: '10s',
      max_memory_restart: '500M',
      env: {
        NODE_ENV: 'development',
        FORCE_COLOR: '0'
      },
      error_file: '/tmp/wo-frontend-err.log',
      out_file: '/tmp/wo-frontend-out.log',
      log_date_format: 'YYYY-MM-DD HH:mm:ss',
      instances: 1,
      execute_command: true
    }
  ]
}