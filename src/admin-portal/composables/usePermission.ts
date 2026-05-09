import { ref, computed } from 'vue'
import { ElMessageBox } from 'element-plus'

const userRole = ref<string>('')
const userName = ref<string>('')

// 管理员密码（实际项目中应从服务端验证）
const ADMIN_PASSWORD = 'Admin@123'

export const usePermission = () => {
  // 从 localStorage 加载用户信息
  const loadUserInfo = () => {
    const userStr = localStorage.getItem('user')
    if (userStr) {
      try {
        const user = JSON.parse(userStr)
        userRole.value = user.role || ''
        userName.value = user.name || ''
      } catch (e) {
        userRole.value = ''
        userName.value = ''
      }
    }
  }

  // 是否是系统管理员
  const isAdmin = computed(() => {
    const role = userRole.value.toLowerCase()
    return role === 'administrator' || role === 'admin' || role === '系统管理员'
  })

  // 验证管理员密码
  const verifyAdminPassword = async (): Promise<boolean> => {
    if (isAdmin.value) {
      return true
    }

    try {
      const { value } = await ElMessageBox.prompt(
        '请输入系统管理员密码以继续',
        '权限验证',
        {
          confirmButtonText: '确认',
          cancelButtonText: '取消',
          inputType: 'password',
          beforeClose: (action, instance, done) => {
            if (action === 'confirm') {
              const inputValue = instance.inputValue || ''
              if (inputValue === ADMIN_PASSWORD) {
                done()
              } else {
                instance.validationMessage = '密码错误'
              }
            } else {
              done()
            }
          }
        }
      )
      return value === ADMIN_PASSWORD
    } catch {
      return false
    }
  }

  // 保存前二次确认
  const confirmSave = async (message: string = '确定要保存配置吗？'): Promise<boolean> => {
    try {
      await ElMessageBox.confirm(
        message,
        '确认保存',
        {
          confirmButtonText: '确定保存',
          cancelButtonText: '取消',
          type: 'warning'
        }
      )
      return true
    } catch {
      return false
    }
  }

  // 初始化
  loadUserInfo()

  return {
    userRole,
    userName,
    isAdmin,
    verifyAdminPassword,
    confirmSave,
    loadUserInfo
  }
}
