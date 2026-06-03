const ENV = {
  development: {
    // 使用本地 Gateway（真机调试）
    TICKET_API: 'http://192.168.1.3:5000/api',
    DISPATCH_API: 'http://192.168.1.3:5000/api/tenant/dispatch',
    PERSON_API: 'http://192.168.1.3:5000/api',
    MASTERDATA_API: 'http://192.168.1.3:5000/api',
    AUTH_API: 'http://192.168.1.3:5000/api'
  },
  production: {
    // 生产环境需配置真实域名（带 https）
    TICKET_API: 'https://your-domain.com/api',
    DISPATCH_API: 'https://your-domain.com/api/tenant/dispatch',
    PERSON_API: 'https://your-domain.com/api',
    MASTERDATA_API: 'https://your-domain.com/api',
    AUTH_API: 'https://your-domain.com/api'
  }
}

const currentEnv = 'development'
export default ENV[currentEnv]