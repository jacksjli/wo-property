import client from './http';
import { getServiceUrl } from './config';
import type { ApiResponse } from './http';

export interface LinkedRecord {
  id: number;
  fieldKey: string;
  value: string;
  label: string;
}

export interface LinkedRecordGroup {
  module: string;
  moduleName: string;
  records: LinkedRecord[];
}

export interface LinkedByPhoneResponse {
  phone: string;
  totalModules: number;
  groups: LinkedRecordGroup[];
}

export interface LinkedByNameResponse {
  name: string;
  totalModules: number;
  groups: LinkedRecordGroup[];
}

export const linkedService = {
  /** 按电话查询跨模块关联记录 */
  getByPhone: (phone: string) =>
    client.get<ApiResponse<LinkedByPhoneResponse>>(
      `${getServiceUrl('person')}/api/linked/by-phone/${encodeURIComponent(phone)}`
    ),

  /** 按姓名查询跨模块关联记录 */
  getByName: (name: string) =>
    client.get<ApiResponse<LinkedByNameResponse>>(
      `${getServiceUrl('person')}/api/linked/by-name/${encodeURIComponent(name)}`
    ),
};
