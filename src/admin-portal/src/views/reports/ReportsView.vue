<template>
  <div class="reports-view">
    <div class="page-header">
      <h2>统计分析</h2>
    </div>

    <el-tabs v-model="activeTab" type="border-card">
      <!-- 第一批：设备/工单/物料报表 -->
      <el-tab-pane label="设备报表" name="device">
        <el-table :data="deviceList" v-loading="loading" border stripe>
          <el-table-column prop="code" label="设备编码" width="120" />
          <el-table-column prop="name" label="设备名称" />
          <el-table-column prop="status" label="状态" width="100" />
          <el-table-column prop="maintenanceType" label="维护类型" width="120" />
          <el-table-column prop="maintenanceCost" label="维护费用" width="100">
            <template #default="{ row }">
              {{ row.maintenanceCost ? `¥${row.maintenanceCost}` : '-' }}
            </template>
          </el-table-column>
          <el-table-column prop="createdAt" label="创建时间" width="160">
            <template #default="{ row }">
              {{ formatDate(row.createdAt) }}
            </template>
          </el-table-column>
        </el-table>
        <el-empty v-if="!loading && deviceList.length === 0" description="暂无设备报表数据" />
      </el-tab-pane>

      <el-tab-pane label="工单报表" name="ticket">
        <el-table :data="ticketList" v-loading="loading" border stripe>
          <el-table-column prop="ticketNumber" label="工单编号" width="150" />
          <el-table-column prop="title" label="标题" />
          <el-table-column prop="status" label="状态" width="100" />
          <el-table-column prop="priority" label="优先级" width="80" />
          <el-table-column prop="assignedTo" label="处理人" width="80" />
          <el-table-column prop="createdAt" label="创建时间" width="160">
            <template #default="{ row }">
              {{ formatDate(row.createdAt) }}
            </template>
          </el-table-column>
          <el-table-column prop="resolvedAt" label="解决时间" width="160">
            <template #default="{ row }">
              {{ row.resolvedAt ? formatDate(row.resolvedAt) : '-' }}
            </template>
          </el-table-column>
        </el-table>
        <el-empty v-if="!loading && ticketList.length === 0" description="暂无工单报表数据" />
      </el-tab-pane>

      <el-tab-pane label="物料报表" name="material">
        <el-table :data="materialList" v-loading="loading" border stripe>
          <el-table-column prop="code" label="物料编码" width="120" />
          <el-table-column prop="name" label="物料名称" />
          <el-table-column prop="currentStock" label="当前库存" width="100" />
          <el-table-column prop="safetyStock" label="安全库存" width="100" />
          <el-table-column prop="unitPrice" label="单价" width="100">
            <template #default="{ row }">
              {{ row.unitPrice ? `¥${row.unitPrice}` : '-' }}
            </template>
          </el-table-column>
          <el-table-column prop="createdAt" label="创建时间" width="160">
            <template #default="{ row }">
              {{ formatDate(row.createdAt) }}
            </template>
          </el-table-column>
        </el-table>
        <el-empty v-if="!loading && materialList.length === 0" description="暂无物料报表数据" />
      </el-tab-pane>

      <el-tab-pane label="满意度调查" name="satisfaction">
        <el-table :data="satisfactionList" v-loading="loading" border stripe>
          <el-table-column prop="ticketId" label="工单ID" width="100" />
          <el-table-column prop="rating" label="评分" width="100">
            <template #default="{ row }">
              <el-rate v-model="row.rating" disabled text-color="#ff9900" />
            </template>
          </el-table-column>
          <el-table-column prop="comment" label="评价" />
          <el-table-column prop="respondentName" label="评价人" width="100" />
          <el-table-column prop="submittedAt" label="提交时间" width="160">
            <template #default="{ row }">
              {{ formatDate(row.submittedAt) }}
            </template>
          </el-table-column>
        </el-table>
        <el-empty v-if="!loading && satisfactionList.length === 0" description="暂无满意度调查数据" />
      </el-tab-pane>

      <!-- 第二批 -->
      <el-tab-pane label="采购订单" name="purchase">
        <el-table :data="purchaseList" v-loading="loading" border stripe>
          <el-table-column prop="orderNumber" label="订单编号" width="150" />
          <el-table-column prop="orderDate" label="订单日期" width="120">
            <template #default="{ row }">
              {{ formatDate(row.orderDate) }}
            </template>
          </el-table-column>
          <el-table-column prop="supplier" label="供应商" />
          <el-table-column prop="totalAmount" label="总金额" width="120">
            <template #default="{ row }">
              {{ row.totalAmount ? `¥${row.totalAmount}` : '-' }}
            </template>
          </el-table-column>
          <el-table-column prop="status" label="状态" width="100" />
          <el-table-column prop="createdBy" label="创建人" width="100" />
          <el-table-column prop="createdAt" label="创建时间" width="160">
            <template #default="{ row }">
              {{ formatDate(row.createdAt) }}
            </template>
          </el-table-column>
        </el-table>
        <el-empty v-if="!loading && purchaseList.length === 0" description="暂无采购订单数据" />
      </el-tab-pane>

      <el-tab-pane label="库存事务" name="stock">
        <el-table :data="stockList" v-loading="loading" border stripe>
          <el-table-column prop="materialId" label="物料ID" width="80" />
          <el-table-column prop="transactionType" label="事务类型" width="100" />
          <el-table-column prop="quantity" label="数量" width="80" />
          <el-table-column prop="unitPrice" label="单价" width="100">
            <template #default="{ row }">
              {{ row.unitPrice ? `¥${row.unitPrice}` : '-' }}
            </template>
          </el-table-column>
          <el-table-column prop="totalAmount" label="总金额" width="100">
            <template #default="{ row }">
              {{ row.totalAmount ? `¥${row.totalAmount}` : '-' }}
            </template>
          </el-table-column>
          <el-table-column prop="operator" label="操作员" width="100" />
          <el-table-column prop="createdAt" label="创建时间" width="160">
            <template #default="{ row }">
              {{ formatDate(row.createdAt) }}
            </template>
          </el-table-column>
        </el-table>
        <el-empty v-if="!loading && stockList.length === 0" description="暂无库存事务数据" />
      </el-tab-pane>

      <el-tab-pane label="枚举定义" name="enum">
        <el-table :data="enumList" v-loading="loading" border stripe>
          <el-table-column prop="category" label="分类" width="120" />
          <el-table-column prop="code" label="编码" width="120" />
          <el-table-column prop="name" label="名称" />
          <el-table-column prop="description" label="描述" />
          <el-table-column prop="sortOrder" label="排序" width="80" />
          <el-table-column prop="isActive" label="启用" width="80">
            <template #default="{ row }">
              <el-tag :type="row.isActive ? 'success' : 'info'">{{ row.isActive ? '是' : '否' }}</el-tag>
            </template>
          </el-table-column>
        </el-table>
        <el-empty v-if="!loading && enumList.length === 0" description="暂无枚举定义数据" />
      </el-tab-pane>

      <el-tab-pane label="综合报表" name="general">
        <el-table :data="generalList" v-loading="loading" border stripe>
          <el-table-column prop="reportNumber" label="报表编号" width="150" />
          <el-table-column prop="title" label="标题" />
          <el-table-column prop="type" label="类型" width="100" />
          <el-table-column prop="category" label="分类" width="100" />
          <el-table-column prop="startDate" label="开始日期" width="120">
            <template #default="{ row }">
              {{ row.startDate ? formatDate(row.startDate) : '-' }}
            </template>
          </el-table-column>
          <el-table-column prop="endDate" label="结束日期" width="120">
            <template #default="{ row }">
              {{ row.endDate ? formatDate(row.endDate) : '-' }}
            </template>
          </el-table-column>
          <el-table-column prop="generatedBy" label="生成人" width="100" />
          <el-table-column prop="generatedAt" label="生成时间" width="160">
            <template #default="{ row }">
              {{ formatDate(row.generatedAt) }}
            </template>
          </el-table-column>
        </el-table>
        <el-empty v-if="!loading && generalList.length === 0" description="暂无综合报表数据" />
      </el-tab-pane>
    </el-tabs>
  </div>
</template>

<script setup>
import { ref, watch } from 'vue'
import { ElMessage } from 'element-plus'
import { reportsApi } from '@/api/reports'

const activeTab = ref('device')
const loading = ref(false)

const deviceList = ref([])
const ticketList = ref([])
const materialList = ref([])
const satisfactionList = ref([])
const purchaseList = ref([])
const stockList = ref([])
const enumList = ref([])
const generalList = ref([])

const formatDate = (dateStr) => {
  if (!dateStr) return '-'
  return new Date(dateStr).toLocaleString('zh-CN')
}

const loadDeviceReports = async () => {
  loading.value = true
  try {
    const res = await reportsApi.getDeviceReports()
    if (res.success) deviceList.value = res.data || []
  } catch (e) { ElMessage.error('加载设备报表失败') } finally { loading.value = false }
}

const loadTicketReports = async () => {
  loading.value = true
  try {
    const res = await reportsApi.getTicketReports()
    if (res.success) ticketList.value = res.data || []
  } catch (e) { ElMessage.error('加载工单报表失败') } finally { loading.value = false }
}

const loadMaterialReports = async () => {
  loading.value = true
  try {
    const res = await reportsApi.getMaterialReports()
    if (res.success) materialList.value = res.data || []
  } catch (e) { ElMessage.error('加载物料报表失败') } finally { loading.value = false }
}

const loadSatisfactionSurveys = async () => {
  loading.value = true
  try {
    const res = await reportsApi.getSatisfactionSurveys()
    if (res.success) satisfactionList.value = res.data || []
  } catch (e) { ElMessage.error('加载满意度调查失败') } finally { loading.value = false }
}

const loadPurchaseOrders = async () => {
  loading.value = true
  try {
    const res = await reportsApi.getPurchaseOrders()
    if (res.success) purchaseList.value = res.data || []
  } catch (e) { ElMessage.error('加载采购订单失败') } finally { loading.value = false }
}

const loadStockTransactions = async () => {
  loading.value = true
  try {
    const res = await reportsApi.getStockTransactions()
    if (res.success) stockList.value = res.data || []
  } catch (e) { ElMessage.error('加载库存事务失败') } finally { loading.value = false }
}

const loadEnumDefinitions = async () => {
  loading.value = true
  try {
    const res = await reportsApi.getEnumDefinitions()
    if (res.success) enumList.value = res.data || []
  } catch (e) { ElMessage.error('加载枚举定义失败') } finally { loading.value = false }
}

const loadGeneralReports = async () => {
  loading.value = true
  try {
    const res = await reportsApi.getGeneralReports()
    if (res.success) generalList.value = res.data || []
  } catch (e) { ElMessage.error('加载综合报表失败') } finally { loading.value = false }
}

const loadData = () => {
  switch (activeTab.value) {
    case 'device': loadDeviceReports(); break
    case 'ticket': loadTicketReports(); break
    case 'material': loadMaterialReports(); break
    case 'satisfaction': loadSatisfactionSurveys(); break
    case 'purchase': loadPurchaseOrders(); break
    case 'stock': loadStockTransactions(); break
    case 'enum': loadEnumDefinitions(); break
    case 'general': loadGeneralReports(); break
  }
}

watch(activeTab, loadData)
loadData()
</script>

<style scoped>
.reports-view { padding: 20px; }
.page-header { margin-bottom: 20px; }
.page-header h2 { margin: 0; }
</style>