import { createHttpClient } from './http'
import { getServiceUrl } from './config'

const httpClient = createHttpClient(getServiceUrl('person'))

export const personApi = {
  getList(params?: { page?: number; pageSize?: number; name?: string; phone?: string; department?: string; role?: string; status?: string }) {
    return httpClient.get('/api/tenant/persons', { params })
  },

  get(id: number) {
    return httpClient.get(`/api/tenant/persons/${id}`)
  },

  create(data: {
    employeeNo?: string; name: string; phone: string; gender?: string; birthday?: string;
    idCard?: string; email?: string; address?: string; education?: string;
    graduateSchool?: string; major?: string; role?: string; departmentId?: number;
    departmentName?: string; position?: string; employmentType?: string; hireDate?: string;
    contractStart?: string; contractEnd?: string; salary?: number; remark?: string;
    status?: string;
  }) {
    return httpClient.post('/api/tenant/persons', data)
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
    return httpClient.put(`/api/tenant/persons/${id}`, data)
  },

  delete(id: number) {
    return httpClient.delete(`/api/tenant/persons/${id}`)
  },

  // 批量导入人员
  importExcel(data: { rows: any[] }) {
    return httpClient.post('/api/tenant/persons/import', data)
  },

  // 下载导入模板
  downloadTemplate() {
    return httpClient.get('/api/tenant/persons/template', { responseType: 'blob' })
  },
}

export default personApi