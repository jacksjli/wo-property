import { createHttpClient } from './http'
import { getServiceUrl } from './config'

const httpClient = createHttpClient(getServiceUrl('person'))

export const personApi = {
  // 注意: PersonService 使用 /api/persons 前缀，不是 /api/tenant/persons
  getList(params?: { page?: number; pageSize?: number; name?: string; phone?: string; department?: string; role?: string; status?: string }) {
    return httpClient.get('/api/persons', { params })
  },

  get(id: number) {
    return httpClient.get(`/api/persons/${id}`)
  },

  create(data: {
    employeeNo?: string; name: string; phone: string; gender?: string; birthday?: string;
    idCard?: string; email?: string; address?: string; education?: string;
    graduateSchool?: string; major?: string; role?: string; departmentId?: number;
    departmentName?: string; position?: string; employmentType?: string; hireDate?: string;
    contractStart?: string; contractEnd?: string; salary?: number; remark?: string;
    status?: string;
  }) {
    return httpClient.post('/api/persons', data)
  },

  update(id: number, data: Partial<{
    name: string; avatar: string; gender: string; birthday: string; idCard: string;
    phone: string; email: string; address: string; education: string; graduateSchool: string;
    major: string; role: string; departmentId: number; departmentName: string; position: string;
    employmentType: string; hireDate: string; contractStart: string; contractEnd: string;
    salary: number; bankAccount: string; socialSecurityNo: string; status: string;
    specialties: string; backups: string; emergencyContactName: string;
    emergencyContactRelationship: string; emergencyContactPhone: string; remark: string;
    ticketTypeIds: string; specialtyIds: string; areaIds: string; isSupervisor: boolean;
    maxConcurrentTickets: number;
  }>) {
    return httpClient.put(`/api/persons/${id}`, data)
  },

  // 删除人员（带防御性检查）
  delete(id: number) {
    if (id == null || id === undefined) {
      console.error('[personApi] delete called with invalid id:', id)
      return Promise.reject(new Error('Invalid person id'))
    }
    return httpClient.delete(`/api/persons/${id}`)
  },

  // 批量导入人员
  importExcel(data: { rows: any[] }) {
    return httpClient.post('/api/persons/import', data)
  },

  // 下载导入模板
  downloadTemplate() {
    return httpClient.get('/api/persons/template', { responseType: 'blob' })
  },

  // 导出人员列表
  exportPersons() {
    return httpClient.get('/api/persons/export')
  },
}

export default personApi