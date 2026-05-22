<script setup lang="ts">
import { ref, onMounted, computed, watch } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Plus, Edit, Delete, Refresh, CaretRight, CaretBottom } from '@element-plus/icons-vue'
import { masterApi } from '@/api/http'
import { toPinyinCode } from '@/utils/pinyin'

// === State ===
const loading = ref(false)
const submitting = ref(false)
const areas = ref<any[]>([])
const selectedAreaId = ref<number | null>(null)
const expandedBuildings = ref<Set<number>>(new Set())

// === Dialogs ===
const areaDialogVisible = ref(false)
const buildingDialogVisible = ref(false)
const roomDialogVisible = ref(false)
const isAreaEdit = ref(false)
const isBuildingEdit = ref(false)
const isRoomEdit = ref(false)
const currentAreaId = ref<number | null>(null)
const currentBuildingId = ref<number | null>(null)
const currentRoomId = ref<number | null>(null)

// === Forms ===
const areaForm = ref({ code: '', name: '', sort: 0, isActive: true, region: '' })
const buildingForm = ref({ name: '', code: '', totalFloors: 1, totalUnits: 1, address: '', area: '' })
const roomForm = ref({ floor: '', unit: '', roomNumber: '', roomType: '', area: null as number | null })

// === Computed: selected area info ===
const selectedArea = computed(() => areas.value.find(a => a.id === selectedAreaId.value))

// === Load all areas (no buildings populated initially) ===
const loadAreas = async () => {
  loading.value = true
  try {
    const r: any = await masterApi.get('/hierarchy/area-buildings')
    if (r.success) {
      areas.value = r.data || []
    }
  } catch (e: any) { ElMessage.error(e.message || '加载失败') }
  finally { loading.value = false }
}

// === Load buildings+rooms for a specific area ===
const loadAreaBuildings = async (areaId: number) => {
  // If already loaded, skip
  const area = areas.value.find(a => a.id === areaId)
  if (area && area.buildings && area.buildings.length > 0) return

  const r: any = await masterApi.get('/hierarchy/area-buildings', { params: { areaId } })
  if (r.success && r.data?.buildings) {
    const area = areas.value.find(a => a.id === areaId)
    if (area) area.buildings = r.data.buildings
  }
}

// === Select area ===
const selectArea = async (areaId: number) => {
  selectedAreaId.value = areaId
  expandedBuildings.value.clear()
  await loadAreaBuildings(areaId)
}

// === Toggle building expansion ===
const toggleBuilding = (buildingId: number) => {
  if (expandedBuildings.value.has(buildingId)) {
    expandedBuildings.value.delete(buildingId)
  } else {
    expandedBuildings.value.add(buildingId)
  }
}

// === Area CRUD ===
const openAreaCreate = () => {
  isAreaEdit.value = false
  areaForm.value = { code: '', name: '', sort: 0, isActive: true, region: '' }
  areaDialogVisible.value = true
}

const openAreaEdit = (area: any) => {
  isAreaEdit.value = true
  currentAreaId.value = area.id
  areaForm.value = { code: area.code, name: area.name, sort: area.sortOrder || 0, isActive: area.status === 'Active', region: area.region || '' }
  areaDialogVisible.value = true
}

const handleAreaSave = async () => {
  if (!areaForm.value.code || !areaForm.value.name) {
    ElMessage.warning('请填写编码和名称')
    return
  }
  submitting.value = true
  try {
    const payload = { code: areaForm.value.code, name: areaForm.value.name, sort: areaForm.value.sort, isActive: areaForm.value.isActive, region: areaForm.value.region }
    if (isAreaEdit.value && currentAreaId.value) {
      await masterApi.put(`/areas/${currentAreaId.value}`, payload)
      ElMessage.success('更新成功')
    } else {
      await masterApi.post('/areas', payload)
      ElMessage.success('创建成功')
    }
    areaDialogVisible.value = false
    await loadAreas()
  } catch (e: any) { ElMessage.error(e.message || '操作失败') }
  finally { submitting.value = false }
}

const handleAreaDelete = async (area: any) => {
  // Check if area has buildings
  await loadAreaBuildings(area.id)
  const areaData = areas.value.find(a => a.id === area.id)
  if (areaData && areaData.buildings && areaData.buildings.length > 0) {
    ElMessage.warning('该区域下有楼栋，请先删除楼栋')
    return
  }
  try {
    await ElMessageBox.confirm(`确定删除区域「${area.name}」吗？`, '提示', { type: 'warning' })
    await masterApi.delete(`/areas/${area.id}`)
    ElMessage.success('删除成功')
    if (selectedAreaId.value === area.id) selectedAreaId.value = null
    await loadAreas()
  } catch (e: any) { if (e !== 'cancel') ElMessage.error(e.message || '删除失败') }
}

// === Building CRUD ===
const openBuildingCreate = () => {
  if (!selectedAreaId.value) { ElMessage.warning('请先选择区域'); return }
  isBuildingEdit.value = false
  buildingForm.value = { name: '', code: '', totalFloors: 1, totalUnits: 1, address: '', area: selectedArea.value?.name || '' }
  buildingDialogVisible.value = true
}

const handleBuildingSave = async () => {
  if (!buildingForm.value.name || !buildingForm.value.code) {
    ElMessage.warning('请填写名称和编码')
    return
  }
  submitting.value = true
  try {
    const payload = { area: buildingForm.value.area || (selectedAreaId.value ? areas.value.find(a => a.id === selectedAreaId.value)?.name : ''), ...buildingForm.value }
    let r: any
    if (isBuildingEdit.value && currentBuildingId.value) {
      // 更新模式
      r = await masterApi.put(`/hierarchy/buildings/${currentBuildingId.value}`, payload)
      if (r.success) {
        ElMessage.success('楼栋更新成功')
        buildingDialogVisible.value = false
        await loadAreaBuildings(selectedAreaId.value!)
      } else {
        ElMessage.error(r.message || '更新失败')
      }
    } else {
      // 创建模式
      r = await masterApi.post('/hierarchy/buildings', payload)
      if (r.success) {
        ElMessage.success('楼栋创建成功')
        buildingDialogVisible.value = false
        await loadAreaBuildings(selectedAreaId.value!)
        // Refresh the area in the list
        const area = areas.value.find(a => a.id === selectedAreaId.value)
        if (area) area.buildings = []
      } else {
        ElMessage.error(r.message || '创建失败')
      }
    }
  } catch (e: any) { ElMessage.error(e.message || '操作失败') }
  finally { submitting.value = false }
}

const openBuildingEdit = (building: any) => {
  isBuildingEdit.value = true
  currentBuildingId.value = building.id
  buildingForm.value = { name: building.name, code: building.code, totalFloors: building.totalFloors, totalUnits: building.totalUnits, address: building.address || '', area: building.area || '' }
  buildingDialogVisible.value = true
}

const handleBuildingDelete = async (building: any) => {
  // Check if building has rooms
  const areaData = areas.value.find(a => a.id === selectedAreaId.value)
  const buildingData = areaData?.buildings?.find((b: any) => b.id === building.id)
  if (buildingData && buildingData.rooms && buildingData.rooms.length > 0) {
    ElMessage.warning('该楼栋下有房号，请先删除房号')
    return
  }
  try {
    await ElMessageBox.confirm(`确定删除楼栋「${building.name}」吗？`, '提示', { type: 'warning' })
    const r: any = await masterApi.delete(`/hierarchy/buildings/${building.id}`)
    if (r.success) {
      ElMessage.success('删除成功')
      await loadAreaBuildings(selectedAreaId.value!)
    } else {
      ElMessage.error(r.message || '删除失败')
    }
  } catch (e: any) { if (e !== 'cancel') ElMessage.error(e.message || '删除失败') }
}

// === Room CRUD ===
const openRoomCreate = (buildingId: number) => {
  isRoomEdit.value = false
  currentRoomId.value = null
  roomForm.value = { buildingId, floor: '', unit: '', roomNumber: '', roomType: '', area: null }
  roomDialogVisible.value = true
}

const openRoomEdit = (room: any) => {
  isRoomEdit.value = true
  currentRoomId.value = room.id
  roomForm.value = { buildingId: room.buildingId, floor: room.floor || '', unit: room.unit || '', roomNumber: room.roomNumber, roomType: room.roomType || '', area: room.area }
  roomDialogVisible.value = true
}

const handleRoomSave = async () => {
  if (!roomForm.value.roomNumber) {
    ElMessage.warning('请填写房号')
    return
  }
  submitting.value = true
  try {
    const payload = { buildingId: roomForm.value.buildingId, floor: roomForm.value.floor, unit: roomForm.value.unit, roomNumber: roomForm.value.roomNumber, roomType: roomForm.value.roomType, area: roomForm.value.area }
    let r: any
    if (isRoomEdit.value && currentRoomId.value) {
      r = await masterApi.put(`/hierarchy/rooms/${currentRoomId.value}`, payload)
      if (r.success) {
        ElMessage.success('房号更新成功')
        roomDialogVisible.value = false
        const area = areas.value.find(a => a.id === selectedAreaId.value)
        if (area) area.buildings = []
        await loadAreaBuildings(selectedAreaId.value!)
      } else {
        ElMessage.error(r.message || '更新失败')
      }
    } else {
      r = await masterApi.post('/hierarchy/rooms', payload)
      if (r.success) {
        ElMessage.success('房号创建成功')
        roomDialogVisible.value = false
        const area = areas.value.find(a => a.id === selectedAreaId.value)
        if (area) area.buildings = []
        await loadAreaBuildings(selectedAreaId.value!)
      } else {
        ElMessage.error(r.message || '创建失败')
      }
    }
  } catch (e: any) { ElMessage.error(e.message || '操作失败') }
  finally { submitting.value = false }
}

const handleRoomDelete = async (room: any) => {
  try {
    await ElMessageBox.confirm(`确定删除房号「${room.roomNumber}」吗？`, '提示', { type: 'warning' })
    const r: any = await masterApi.delete(`/hierarchy/rooms/${room.id}`)
    if (r.success) {
      ElMessage.success('删除成功')
      await loadAreaBuildings(selectedAreaId.value!)
    } else {
      ElMessage.error(r.message || '删除失败')
    }
  } catch (e: any) { if (e !== 'cancel') ElMessage.error(e.message || '删除失败') }
}

onMounted(() => { loadAreas() })
</script>

<template>
  <div class="area-building-view">
    <!-- Page Header -->
    <div class="page-header">
      <h2>区域·楼栋·房号</h2>
      <div class="header-actions">
        <el-button :icon="Refresh" @click="loadAreas">刷新</el-button>
      </div>
    </div>

    <div class="main-layout">
      <!-- Left Panel: Area List -->
      <div class="left-panel">
        <div class="panel-header">
          <span>区域列表</span>
          <el-button type="primary" size="small" :icon="Plus" @click="openAreaCreate">新增</el-button>
        </div>
        <el-table :data="areas" v-loading="loading" stripe highlight-current-row @row-click="(row: any) => selectArea(row.id)" :current-row-key="selectedAreaId">
          <el-table-column prop="name" label="区域" />
          <el-table-column prop="code" label="编码" width="80" />
          <el-table-column label="操作" width="110" align="center">
            <template #default="{ row }">
              <el-button link type="primary" size="small" @click.stop="openAreaEdit(row)">编辑</el-button>
              <el-button link type="danger" size="small" @click.stop="handleAreaDelete(row)">删除</el-button>
            </template>
          </el-table-column>
        </el-table>
      </div>

      <!-- Right Panel: Buildings & Rooms -->
      <div class="right-panel">
        <!-- No area selected -->
        <div v-if="!selectedAreaId" class="empty-state">
          <p>请从左侧选择一个区域</p>
        </div>

        <!-- Area Detail -->
        <div v-else-if="selectedArea" class="area-detail">
          <div class="area-detail-header">
            <h3>{{ selectedArea.name }} ({{ selectedArea.code }})</h3>
            <el-button type="primary" :icon="Plus" @click="openBuildingCreate">新增楼栋</el-button>
          </div>

          <div class="buildings-list">
            <div v-if="!selectedArea.buildings || selectedArea.buildings.length === 0" class="empty-state">
              <p>该区域下暂无楼栋</p>
            </div>

            <div v-for="building in (selectedArea.buildings || [])" :key="building.id" class="building-item">
              <!-- Building Header -->
              <div class="building-header" @click="toggleBuilding(building.id)">
                <el-icon v-if="expandedBuildings.has(building.id)" class="expand-icon"><CaretBottom /></el-icon>
                <el-icon v-else class="expand-icon"><CaretRight /></el-icon>
                <span class="building-name">{{ building.name }} ({{ building.code }})</span>
                <span class="building-meta">{{ building.totalFloors }}层 / {{ building.totalUnits }}单元{{ building.area ? ' / ' + building.area : '' }}</span>
                <div class="building-actions">
                  <el-button link type="primary" size="small" @click.stop="openBuildingEdit(building)">编辑</el-button>
                  <el-button link type="danger" size="small" @click.stop="handleBuildingDelete(building)">删除</el-button>
                </div>
              </div>

              <!-- Rooms (expanded) -->
              <div v-if="expandedBuildings.has(building.id)" class="rooms-grid">
                <div v-if="!building.rooms || building.rooms.length === 0" class="empty-rooms">暂无房号</div>
                <div v-for="room in (building.rooms || [])" :key="room.id" class="room-item">
                  <span class="room-number">{{ room.roomNumber }}</span>
                  <span class="room-info">{{ room.roomType || '' }} {{ room.area ? room.area + '㎡' : '' }}</span>
                  <el-button link type="primary" size="small" @click.stop="openRoomEdit(room)">编辑</el-button>
                  <el-button link type="danger" size="small" @click.stop="handleRoomDelete(room)">删除</el-button>
                </div>
                <!-- Add Room -->
                <div class="room-item add-room" @click.stop="openRoomCreate(building.id)">
                  <el-icon><Plus /></el-icon>
                  <span>新增房号</span>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Area Dialog -->
    <el-dialog v-model="areaDialogVisible" :title="isAreaEdit ? '编辑区域' : '新增区域'" width="450px">
      <el-form label-width="80px">
        <el-form-item label="编码" required>
          <el-input v-model="areaForm.code" :disabled="isAreaEdit" />
        </el-form-item>
        <el-form-item label="名称" required>
          <el-input v-model="areaForm.name" />
        </el-form-item>
        <el-form-item label="所属大区">
          <el-input v-model="areaForm.region" placeholder="如：北京市" />
        </el-form-item>
        <el-form-item label="排序">
          <el-input-number v-model="areaForm.sort" :min="0" />
        </el-form-item>
        <el-form-item label="状态">
          <el-switch v-model="areaForm.isActive" active-text="启用" inactive-text="停用" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="areaDialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="submitting" @click="handleAreaSave">保存</el-button>
      </template>
    </el-dialog>

    <!-- Building Dialog -->
    <el-dialog v-model="buildingDialogVisible" :title="isBuildingEdit ? '编辑楼栋' : '新增楼栋'" width="450px">
      <el-form label-width="80px">
        <el-form-item label="所属区域">
          <el-input :value="isBuildingEdit ? buildingForm.area : selectedArea?.name" disabled />
        </el-form-item>
        <el-form-item label="编码" required>
          <el-input v-model="buildingForm.code" :disabled="isBuildingEdit" />
        </el-form-item>
        <el-form-item label="名称" required>
          <el-input v-model="buildingForm.name" />
        </el-form-item>
        <el-form-item label="地址">
          <el-input v-model="buildingForm.address" />
        </el-form-item>
        <el-form-item label="总层数">
          <el-input-number v-model="buildingForm.totalFloors" :min="1" />
        </el-form-item>
        <el-form-item label="单元数">
          <el-input-number v-model="buildingForm.totalUnits" :min="1" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="buildingDialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="submitting" @click="handleBuildingSave">保存</el-button>
      </template>
    </el-dialog>

    <!-- Room Dialog -->
    <el-dialog v-model="roomDialogVisible" :title="isRoomEdit ? '编辑房号' : '新增房号'" width="450px">
      <el-form label-width="80px">
        <el-form-item label="房号" required>
          <el-input v-model="roomForm.roomNumber" placeholder="如：101" />
        </el-form-item>
        <el-form-item label="楼层">
          <el-input v-model="roomForm.floor" placeholder="如：1" />
        </el-form-item>
        <el-form-item label="单元">
          <el-input v-model="roomForm.unit" placeholder="如：1" />
        </el-form-item>
        <el-form-item label="类型">
          <el-input v-model="roomForm.roomType" placeholder="如：住宅、商铺" />
        </el-form-item>
        <el-form-item label="面积(㎡)">
          <el-input-number v-model="roomForm.area" :min="0" :precision="2" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="roomDialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="submitting" @click="handleRoomSave">保存</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<style scoped>
.area-building-view { padding: 20px; }
.page-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 20px; }
.page-header h2 { margin: 0; }
.main-layout { display: flex; gap: 20px; height: calc(100vh - 180px); }
.left-panel { width: 320px; flex-shrink: 0; background: #fff; border-radius: 8px; padding: 16px; overflow-y: auto; }
.right-panel { flex: 1; background: #fff; border-radius: 8px; padding: 16px; overflow-y: auto; }
.panel-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 12px; font-weight: bold; }
.area-detail-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 16px; padding-bottom: 12px; border-bottom: 1px solid #eee; }
.area-detail-header h3 { margin: 0; }
.buildings-list { display: flex; flex-direction: column; gap: 8px; }
.building-item { border: 1px solid #e8e8e8; border-radius: 6px; overflow: hidden; }
.building-header { display: flex; align-items: center; gap: 8px; padding: 12px; background: #f9f9f9; cursor: pointer; }
.building-header:hover { background: #f0f0f0; }
.expand-icon { font-size: 14px; color: #999; }
.building-name { font-weight: bold; flex: 1; }
.building-meta { font-size: 12px; color: #999; }
.building-actions { display: flex; gap: 4px; }
.rooms-grid { display: flex; flex-wrap: wrap; gap: 8px; padding: 12px; background: #fff; }
.room-item { display: flex; align-items: center; gap: 6px; padding: 8px 12px; border: 1px solid #ddd; border-radius: 4px; min-width: 100px; }
.room-number { font-weight: bold; }
.room-info { font-size: 12px; color: #888; }
.add-room { border-style: dashed; cursor: pointer; color: #888; }
.add-room:hover { border-color: #409eff; color: #409eff; }
.empty-state { display: flex; align-items: center; justify-content: center; height: 200px; color: #999; }
.empty-rooms { width: 100%; text-align: center; padding: 12px; color: #ccc; font-size: 13px; }
</style>