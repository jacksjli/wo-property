<template>
  <div class="material-list-view">
    <!-- 页面标题 -->
    <div class="page-header">
      <div class="header-left">
        <h1>物料管理</h1>
        <p>管理物料库存、采购和领用</p>
      </div>
      <div class="header-right">
        <el-button type="primary" @click="showCreateDialog = true">
          <el-icon><Plus /></el-icon>
          添加物料
        </el-button>
        <el-button @click="showStockInDialog = true">
          <el-icon><Box /></el-icon>
          入库操作
        </el-button>
        <el-button @click="showStockOutDialog = true">
          <el-icon><TakeawayBox /></el-icon>
          出库操作
        </el-button>
      </div>
    </div>
    
    <!-- 统计卡片 -->
    <div class="stats-cards">
      <el-card class="stat-card" shadow="never">
        <div class="stat-content">
          <div class="stat-icon total-materials">
            <el-icon><Box /></el-icon>
          </div>
          <div class="stat-info">
            <div class="stat-value">{{ statistics.totalMaterials }}</div>
            <div class="stat-label">物料种类</div>
          </div>
        </div>
      </el-card>
      
      <el-card class="stat-card" shadow="never">
        <div class="stat-content">
          <div class="stat-icon low-stock">
            <el-icon><Warning /></el-icon>
          </div>
          <div class="stat-info">
            <div class="stat-value">{{ statistics.lowStockMaterials }}</div>
            <div class="stat-label">低库存物料</div>
          </div>
        </div>
      </el-card>
      
      <el-card class="stat-card" shadow="never">
        <div class="stat-content">
          <div class="stat-icon out-of-stock">
            <el-icon><CircleClose /></el-icon>
          </div>
          <div class="stat-info">
            <div class="stat-value">{{ statistics.outOfStockMaterials }}</div>
            <div class="stat-label">缺货物料</div>
          </div>
        </div>
      </el-card>
      
      <el-card class="stat-card" shadow="never">
        <div class="stat-content">
          <div class="stat-icon stock-value">
            <el-icon><Money /></el-icon>
          </div>
          <div class="stat-info">
            <div class="stat-value">¥{{ statistics.totalStockValue.toLocaleString() }}</div>
            <div class="stat-label">库存总价值</div>
          </div>
        </div>
      </el-card>
    </div>
    
    <!-- 筛选工具栏 -->
    <el-card class="filter-card" shadow="never">
      <div class="filter-toolbar">
        <el-input
          v-model="searchQuery"
          placeholder="搜索物料编码、名称、描述..."
          class="search-input"
          clearable
          @input="handleSearch"
        >
          <template #prefix>
            <el-icon><Search /></el-icon>
          </template>
        </el-input>
        
        <div class="filter-actions">
          <el-select
            v-model="filterCategory"
            placeholder="所有分类"
            clearable
            @change="handleFilter"
          >
            <el-option 
              v-for="category in categories" 
              :key="category.id"
              :label="category.name"
              :value="category.id"
            />
          </el-select>
          
          <el-select
            v-model="filterStockStatus"
            placeholder="库存状态"
            clearable
            @change="handleFilter"
          >
            <el-option label="正常库存" value="normal" />
            <el-option label="低库存" value="low" />
            <el-option label="缺货" value="out" />
          </el-select>
          
          <el-button @click="resetFilters">
            <el-icon><Refresh /></el-icon>
            重置筛选
          </el-button>
        </div>
      </div>
    </el-card>
    
    <!-- 物料表格 -->
    <el-card class="materials-table-card" shadow="never">
      <template #header>
        <div class="table-header">
          <h3>物料列表</h3>
          <div class="table-actions">
            <el-button type="text" @click="refreshMaterials">
              <el-icon><Refresh /></el-icon>
              刷新
            </el-button>
          </div>
        </div>
      </template>
      
      <el-table
        :data="filteredMaterials"
        v-loading="loading"
        style="width: 100%"
        @sort-change="handleSort"
      >
        <el-table-column prop="code" label="物料编码" width="120" sortable />
        
        <el-table-column prop="name" label="物料名称" width="180" sortable>
          <template #default="scope">
            <div class="material-cell">
              <div class="material-name">{{ scope.row.name }}</div>
              <div class="material-desc" v-if="scope.row.description">
                {{ scope.row.description }}
              </div>
            </div>
          </template>
        </el-table-column>
        
        <el-table-column prop="categoryId" label="分类" width="120">
          <template #default="scope">
            {{ getCategoryName(scope.row.categoryId) }}
          </template>
        </el-table-column>
        
        <el-table-column prop="unit" label="单位" width="80" />
        
        <el-table-column prop="unitPrice" label="单价" width="100" sortable>
          <template #default="scope">
            ¥{{ scope.row.unitPrice.toFixed(2) }}
          </template>
        </el-table-column>
        
        <el-table-column prop="currentStock" label="当前库存" width="120" sortable>
          <template #default="scope">
            <div class="stock-cell">
              <span :class="getStockStatusClass(scope.row)">
                {{ scope.row.currentStock }}
              </span>
              <span class="stock-unit">{{ scope.row.unit }}</span>
            </div>
          </template>
        </el-table-column>
        
        <el-table-column prop="safetyStock" label="安全库存" width="100">
          <template #default="scope">
            {{ scope.row.safetyStock }} {{ scope.row.unit }}
          </template>
        </el-table-column>
        
        <el-table-column prop="location" label="存放位置" width="150" />
        
        <el-table-column prop="supplier" label="供应商" width="120" />
        
        <el-table-column label="操作" width="200" fixed="right">
          <template #default="scope">
            <div class="action-buttons">
              <el-button
                type="text"
                size="small"
                @click="editMaterial(scope.row)"
              >
                编辑
              </el-button>
              
              <el-button
                type="text"
                size="small"
                @click="showStockIn(scope.row)"
              >
                入库
              </el-button>
              
              <el-button
                type="text"
                size="small"
                @click="showStockOut(scope.row)"
                :disabled="scope.row.currentStock === 0"
              >
                出库
              </el-button>
            </div>
          </template>
        </el-table-column>
      </el-table>
      
      <!-- 分页 -->
      <div class="pagination-container">
        <el-pagination
          v-model:current-page="currentPage"
          v-model:page-size="pageSize"
          :page-sizes="[10, 20, 50, 100]"
          :total="totalMaterials"
          layout="total, sizes, prev, pager, next, jumper"
          @size-change="handleSizeChange"
          @current-change="handleCurrentChange"
        />
      </div>
    </el-card>
    
    <!-- 最近交易记录 -->
    <el-card class="transactions-card" shadow="never">
      <template #header>
        <h3>最近交易记录</h3>
      </template>
      
      <div v-if="recentTransactions.length > 0" class="transactions-list">
        <div 
          v-for="transaction in recentTransactions" 
          :key="transaction.id"
          class="transaction-item"
        >
          <div class="transaction-icon">
            <el-icon v-if="transaction.transactionType === '入库'"><Box /></el-icon>
            <el-icon v-else><TakeawayBox /></el-icon>
          </div>
          <div class="transaction-content">
            <div class="transaction-header">
              <span class="transaction-type" :class="getTransactionTypeClass(transaction.transactionType)">
                {{ transaction.transactionType }}
              </span>
              <span class="transaction-amount">
                {{ transaction.quantity }} {{ getMaterialUnit(transaction.materialId) }}
              </span>
            </div>
            <div class="transaction-details">
              <span class="transaction-material">
                {{ getMaterialName(transaction.materialId) }}
              </span>
              <span class="transaction-time">
                {{ formatDate(transaction.createdAt) }}
              </span>
            </div>
          </div>
        </div>
      </div>
      
      <div v-else class="empty-transactions">
        <el-empty description="暂无交易记录" />
      </div>
    </el-card>
    
    <!-- 创建物料对话框 -->
    <el-dialog
      v-model="showCreateDialog"
      title="添加物料"
      width="600px"
      :close-on-click-modal="false"
    >
      <el-form
        ref="createFormRef"
        :model="createForm"
        :rules="createRules"
        label-width="100px"
      >
        <el-form-item label="物料编码" prop="code">
          <el-input v-model="createForm.code" placeholder="请输入物料编码" />
        </el-form-item>
        
        <el-form-item label="物料名称" prop="name">
          <el-input v-model="createForm.name" placeholder="请输入物料名称" />
        </el-form-item>
        
        <el-form-item label="物料描述">
          <el-input
            v-model="createForm.description"
            type="textarea"
            :rows="2"
            placeholder="请输入物料描述"
          />
        </el-form-item>
        
        <el-row :gutter="20">
          <el-col :span="12">
            <el-form-item label="物料分类" prop="categoryId">
              <el-select 
                v-model="createForm.categoryId" 
                placeholder="请选择分类"
                style="width: 100%"
              >
                <el-option 
                  v-for="category in categories" 
                  :key="category.id"
                  :label="category.name"
                  :value="category.id"
                />
              </el-select>
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="单位" prop="unit">
              <el-input v-model="createForm.unit" placeholder="如：个、米、卷" />
            </el-form-item>
          </el-col>
        </el-row>
        
        <el-row :gutter="20">
          <el-col :span="12">
            <el-form-item label="单价" prop="unitPrice">
              <el-input
                v-model="createForm.unitPrice"
                type="number"
                :min="0"
                :step="0.01"
                placeholder="请输入单价"
              >
                <template #append>元</template>
              </el-input>
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="安全库存">
              <el-input-number
                v-model="createForm.safetyStock"
                :min="0"
                :step="1"
                placeholder="安全库存"
              />
            </el-form-item>
          </el-col>
        </el-row>
        
        <el-row :gutter="20">
          <el-col :span="12">
            <el-form-item label="最大库存">
              <el-input-number
                v-model="createForm.maxStock"
                :min="0"
                :step="1"
                placeholder="最大库存"
              />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="存放位置">
              <el-input v-model="createForm.location" placeholder="如：A区-1排-3层" />
            </el-form-item>
          </el-col>
        </el-row>
        
        <el-form-item label="供应商">
          <el-input v-model="createForm.supplier" placeholder="请输入供应商" />
        </el-form-item>
        
        <el-form-item label="备注">
          <el-input
            v-model="createForm.notes"
            type="textarea"
            :rows="2"
            placeholder="请输入备注信息"
          />
        </el-form-item>
      </el-form>
      
      <template #footer>
        <span class="dialog-footer">
          <el-button @click="showCreateDialog = false">取消</el-button>
          <el-button type="primary" @click="createMaterial" :loading="creating">
            创建物料
          </el-button>
        </span>
      </template>
    </el-dialog>
    
    <!-- 入库对话框 -->
    <el-dialog
      v-model="showStockInDialog"
      :title="selectedMaterial ? `入库 - ${selectedMaterial.name}` : '入库操作'"
      width="500px"
      :close-on-click-modal="false"
    >
      <el-form
        ref="stockInFormRef"
        :model="stockInForm"
        :rules="stockInRules"
        label-width="100px"
      >
        <el-form-item v-if="!selectedMaterial" label="选择物料" prop="materialId">
          <el-select 
            v-model="stockInForm.materialId" 
            placeholder="请选择物料"
            style="width: 100%"
            @change="handleMaterialSelect"
          >
            <el-option 
              v-for="material in materials" 
              :key="material.id"
              :label="`${material.code} - ${material.name}`"
              :value="material.id"
            />
          </el-select>
        </el-form-item>
        
        <el-form-item label="入库数量" prop="quantity">
          <el-input-number
            v-model="stockInForm.quantity"
            :min="1"
            :step="1"
            placeholder="请输入入库数量"
            style="width: 100%"
          />
          <span class="form-hint" v-if="selectedMaterial">
            {{ selectedMaterial.unit }}
          </span>
        </el-form-item>
        
        <el-form-item label="入库单价">
          <el-input
            v-model="stockInForm.unitPrice"
            type="number"
            :min="0"
            :step="0.01"
            placeholder="请输入入库单价"
          >
            <template #append>元</template>
          </el-input>
          <div class="form-hint" v-if="selectedMaterial">
            当前单价: ¥{{ selectedMaterial.unitPrice.toFixed(2) }}
          </div>
        </el-form-item>
        
        <el-form-item label="参考单号">
          <el-input v-model="stockInForm.referenceNumber" placeholder="请输入参考单号" />
        </el-form-item>
        
        <el-form-item label="备注">
          <el-input
            v-model="stockInForm.notes"
            type="textarea"
            :rows="3"
            placeholder="请输入备注信息"
          />
        </el-form-item>
      </el-form>
      
      <template #footer>
        <span class="dialog-footer">
          <el-button @click="showStockInDialog = false">取消</el-button>
          <el-button type="primary" @click="performStockIn" :loading="processingStock">
            确认入库
          </el-button>
        </span>
      </template>
    </el-dialog>
    
    <!-- 出库对话框 -->
    <el-dialog
      v-model="showStockOutDialog"
      :title="selectedMaterial ? `出库 - ${selectedMaterial.name}` : '出库操作'"
      width="500px"
      :close-on-click-modal="false"
    >
      <el-form
        ref="stockOutFormRef"
        :model="stockOutForm"
        :rules="stockOutRules"
        label-width="100px"
      >
        <el-form-item v-if="!selectedMaterial" label="选择物料" prop="materialId">
          <el-select 
            v-model="stockOutForm.materialId" 
            placeholder="请选择物料"
            style="width: 100%"
            @change="handleMaterialSelect"
          >
            <el-option 
              v-for="material in materials" 
              :key="material.id"
              :label="`${material.code} - ${material.name}`"
              :value="material.id"
            />
          </el-select>
        </el-form-item>
        
        <el-form-item label="出库数量" prop="quantity">
          <el-input-number
            v-model="stockOutForm.quantity"
            :min="1"
            :max="selectedMaterial ? selectedMaterial.currentStock : undefined"
            :step="1"
            placeholder="请输入出库数量"
            style="width: 100%"
          />
          <span class="form-hint" v-if="selectedMaterial">
            {{ selectedMaterial.unit }} (当前库存: {{ selectedMaterial.currentStock }})
          </span>
        </el-form-item>
        
        <el-form-item label="关联工单">
          <el-input
            v-model="stockOutForm.ticketId"
            type="number"
            :min="1"
            placeholder="请输入工单ID"
          />
        </el-form-item>
        
        <el-form-item label="参考单号">
          <el-input v-model="stockOutForm.referenceNumber" placeholder="请输入参考单号" />
        </el-form-item>
        
        <el-form-item label="备注">
          <el-input
            v-model="stockOutForm.notes"
            type="textarea"
            :rows="3"
            placeholder="请输入备注信息"
          />
        </el-form-item>
      </el-form>
      
      <template #footer>
        <span class="dialog-footer">
          <el-button @click="showStockOutDialog = false">取消</el-button>
          <el-button type="primary" @click="performStockOut" :loading="processingStock">
            确认出库
          </el-button>
        </span>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import { ElMessage, ElMessageBox, type FormInstance, type FormRules } from 'element-plus';
import { 
  Plus, Box, TakeawayBox, Warning, CircleClose, Money, 
  Search, Refresh 
} from '@element-plus/icons-vue';
import { useAuthStore } from '@/stores/auth';
import { materialApi, type Material, type MaterialCategory, type StockTransaction } from '@/api/material';

const authStore = useAuthStore();

// 物料数据
const materials = ref<Material[]>([]);
const categories = ref<MaterialCategory[]>([]);
const recentTransactions = ref<StockTransaction[]>([]);
const loading = ref(false);

// 筛选和搜索
const searchQuery = ref('');
const filterCategory = ref<number | null>(null);
const filterStockStatus = ref<string>('');

// 分页
const currentPage = ref(1);
const pageSize = ref(20);
const totalMaterials = ref(0);

// 统计信息
const statistics = ref({
  totalMaterials: 0,
  lowStockMaterials: 0,
  outOfStockMaterials: 0,
  totalStockValue: 0
});

// 对话框状态
const showCreateDialog = ref(false);
const showStockInDialog = ref(false);
const showStockOutDialog = ref(false);
const creating = ref(false);
const processingStock = ref(false);

// 选中的物料
const selectedMaterial = ref<Material | null>(null);

// 创建物料表单
const createFormRef = ref<FormInstance>();
const createForm = ref({
  code: '',
  name: '',
  description: '',
  categoryId: 0,
  unit: '',
  unitPrice: 0,
  safetyStock: 0,
  maxStock: 1000,
  location: '',
  supplier: '',
  notes: ''
});

// 入库表单
const stockInFormRef = ref<FormInstance>();
const stockInForm = ref({
  materialId: 0,
  quantity: 1,
  unitPrice: undefined as number | undefined,
  referenceNumber: '',
  notes: '',
  createdBy: 0
});

// 出库表单
const stockOutFormRef = ref<FormInstance>();
const stockOutForm = ref({
  materialId: 0,
  quantity: 1,
  unitPrice: undefined as number | undefined,
  referenceNumber: '',
  ticketId: undefined as number | undefined,
  notes: '',
  createdBy: 0
});

// 表单验证规则
const createRules: FormRules = {
  code: [
    { required: true, message: '请输入物料编码', trigger: 'blur' },
    { min: 3, message: '物料编码至少3个字符', trigger: 'blur' }
  ],
  name: [
    { required: true, message: '请输入物料名称', trigger: 'blur' },
    { min: 2, message: '物料名称至少2个字符', trigger: 'blur' }
  ],
  categoryId: [
    { required: true, message: '请选择物料分类', trigger: 'change' }
  ],
  unit: [
    { required: true, message: '请输入单位', trigger: 'blur' }
  ],
  unitPrice: [
    { required: true, message: '请输入单价', trigger: 'blur' },
    { type: 'number', message: '单价必须为数字', trigger: 'blur' }
  ]
};

const stockInRules: FormRules = {
  materialId: [
    { required: true, message: '请选择物料', trigger: 'change' }
  ],
  quantity: [
    { required: true, message: '请输入入库数量', trigger: 'blur' },
    { type: 'number', min: 1, message: '入库数量必须大于0', trigger: 'blur' }
  ]
};

const stockOutRules: FormRules = {
  materialId: [
    { required: true, message: '请选择物料', trigger: 'change' }
  ],
  quantity: [
    { required: true, message: '请输入出库数量', trigger: 'blur' },
    { type: 'number', min: 1, message: '出库数量必须大于0', trigger: 'blur' }
  ]
};

// 获取分类名称
const getCategoryName = (categoryId: number) => {
  const category = categories.value.find(c => c.id === categoryId);
  return category ? category.name : '未知分类';
};

// 获取物料名称
const getMaterialName = (materialId: number) => {
  const material = materials.value.find(m => m.id === materialId);
  return material ? material.name : '未知物料';
};

// 获取物料单位
const getMaterialUnit = (materialId: number) => {
  const material = materials.value.find(m => m.id === materialId);
  return material ? material.unit : '';
};

// 获取库存状态类
const getStockStatusClass = (material: Material) => {
  if (material.currentStock === 0) return 'stock-out';
  if (material.currentStock <= material.safetyStock) return 'stock-low';
  return 'stock-normal';
};

// 获取交易类型类
const getTransactionTypeClass = (type: string) => {
  return type === '入库' ? 'transaction-in' : 'transaction-out';
};

// 格式化日期
const formatDate = (dateString: string) => {
  if (!dateString) return '';
  const date = new Date(dateString);
  return date.toLocaleString('zh-CN', {
    month: 'short',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit'
  });
};

// 过滤后的物料
const filteredMaterials = computed(() => {
  let filtered = [...materials.value];
  
  // 搜索过滤
  if (searchQuery.value) {
    const query = searchQuery.value.toLowerCase();
    filtered = filtered.filter(material =>
      material.code.toLowerCase().includes(query) ||
      material.name.toLowerCase().includes(query) ||
      (material.description && material.description.toLowerCase().includes(query))
    );
  }
  
  // 分类过滤
  if (filterCategory.value) {
    filtered = filtered.filter(material => material.categoryId === filterCategory.value);
  }
  
  // 库存状态过滤
  if (filterStockStatus.value) {
    if (filterStockStatus.value === 'low') {
      filtered = filtered.filter(material => material.currentStock <= material.safetyStock && material.currentStock > 0);
    } else if (filterStockStatus.value === 'out') {
      filtered = filtered.filter(material => material.currentStock === 0);
    } else if (filterStockStatus.value === 'normal') {
      filtered = filtered.filter(material => material.currentStock > material.safetyStock);
    }
  }
  
  // 更新总数
  totalMaterials.value = filtered.length;
  
  // 分页
  const start = (currentPage.value - 1) * pageSize.value;
  const end = start + pageSize.value;
  return filtered.slice(start, end);
});

// 处理搜索
const handleSearch = () => {
  currentPage.value = 1;
};

// 处理筛选
const handleFilter = () => {
  currentPage.value = 1;
};

// 重置筛选
const resetFilters = () => {
  searchQuery.value = '';
  filterCategory.value = null;
  filterStockStatus.value = '';
  currentPage.value = 1;
};

// 处理排序
const handleSort = (sort: any) => {
  console.log('排序:', sort);
  // 这里可以实现实际的排序逻辑
};

// 处理分页大小变化
const handleSizeChange = (size: number) => {
  pageSize.value = size;
  currentPage.value = 1;
};

// 处理当前页变化
const handleCurrentChange = (page: number) => {
  currentPage.value = page;
};

// 刷新物料数据
const refreshMaterials = async () => {
  loading.value = true;
  try {
    await Promise.all([
      loadCategories(),
      loadMaterials(),
      loadStatistics()
    ]);
    ElMessage.success('物料数据已刷新');
  } catch (error) {
    ElMessage.error('刷新失败');
  } finally {
    loading.value = false;
  }
};

// 加载分类数据
const loadCategories = async () => {
  try {
    const response = await materialApi.getCategories();
    if (response.success) {
      categories.value = response.data.categories;
    }
  } catch (error) {
    console.error('加载分类失败:', error);
  }
};

// 加载物料数据
const loadMaterials = async () => {
  try {
    const response = await materialApi.getMaterials();
    if (response.success) {
      materials.value = response.data.materials;
    }
  } catch (error) {
    console.error('加载物料失败:', error);
  }
};

// 加载统计数据和交易记录
const loadStatistics = async () => {
  try {
    const response = await materialApi.getStatistics();
    if (response.success) {
      statistics.value = response.data.statistics;
      recentTransactions.value = response.data.recentTransactions;
    }
  } catch (error) {
    console.error('加载统计失败:', error);
  }
};

// 创建物料
const createMaterial = async () => {
  if (!createFormRef.value) return;
  
  try {
    await createFormRef.value.validate();
    creating.value = true;
    
    const response = await materialApi.createMaterial(createForm.value);
    
    if (response.success) {
      ElMessage.success('物料创建成功');
      showCreateDialog.value = false;
      createFormRef.value.resetFields();
      await refreshMaterials();
    } else {
      ElMessage.error(response.message || '创建失败');
    }
  } catch (error) {
    console.error('表单验证失败:', error);
  } finally {
    creating.value = false;
  }
};

// 编辑物料
const editMaterial = (material: Material) => {
  ElMessage.info('编辑功能开发中...');
};

// 显示入库对话框
const showStockIn = (material?: Material) => {
  if (material) {
    selectedMaterial.value = material;
    stockInForm.value.materialId = material.id;
    stockInForm.value.unitPrice = material.unitPrice;
  } else {
    selectedMaterial.value = null;
    stockInForm.value.materialId = 0;
    stockInForm.value.unitPrice = undefined;
  }
  
  stockInForm.value.quantity = 1;
  stockInForm.value.referenceNumber = '';
  stockInForm.value.notes = '';
  stockInForm.value.createdBy = authStore.user?.id || 1;
  
  showStockInDialog.value = true;
};

// 显示出库对话框
const showStockOut = (material?: Material) => {
  if (material) {
    selectedMaterial.value = material;
    stockOutForm.value.materialId = material.id;
    stockOutForm.value.unitPrice = material.unitPrice;
  } else {
    selectedMaterial.value = null;
    stockOutForm.value.materialId = 0;
    stockOutForm.value.unitPrice = undefined;
  }
  
  stockOutForm.value.quantity = 1;
  stockOutForm.value.referenceNumber = '';
  stockOutForm.value.ticketId = undefined;
  stockOutForm.value.notes = '';
  stockOutForm.value.createdBy = authStore.user?.id || 1;
  
  showStockOutDialog.value = true;
};

// 处理物料选择
const handleMaterialSelect = (materialId: number) => {
  const material = materials.value.find(m => m.id === materialId);
  if (material) {
    selectedMaterial.value = material;
    if (showStockInDialog.value) {
      stockInForm.value.unitPrice = material.unitPrice;
    } else if (showStockOutDialog.value) {
      stockOutForm.value.unitPrice = material.unitPrice;
    }
  }
};

// 执行入库操作
const performStockIn = async () => {
  if (!stockInFormRef.value) return;
  
  try {
    await stockInFormRef.value.validate();
    processingStock.value = true;
    
    const response = await materialApi.stockIn(
      stockInForm.value.materialId,
      {
        quantity: stockInForm.value.quantity,
        unitPrice: stockInForm.value.unitPrice,
        referenceNumber: stockInForm.value.referenceNumber,
        notes: stockInForm.value.notes,
        createdBy: stockInForm.value.createdBy
      }
    );
    
    if (response.success) {
      ElMessage.success('入库成功');
      showStockInDialog.value = false;
      stockInFormRef.value.resetFields();
      selectedMaterial.value = null;
      await refreshMaterials();
    } else {
      ElMessage.error(response.message || '入库失败');
    }
  } catch (error) {
    console.error('表单验证失败:', error);
  } finally {
    processingStock.value = false;
  }
};

// 执行出库操作
const performStockOut = async () => {
  if (!stockOutFormRef.value) return;
  
  try {
    await stockOutFormRef.value.validate();
    processingStock.value = true;
    
    const response = await materialApi.stockOut(
      stockOutForm.value.materialId,
      {
        quantity: stockOutForm.value.quantity,
        unitPrice: stockOutForm.value.unitPrice,
        referenceNumber: stockOutForm.value.referenceNumber,
        ticketId: stockOutForm.value.ticketId,
        notes: stockOutForm.value.notes,
        createdBy: stockOutForm.value.createdBy
      }
    );
    
    if (response.success) {
      ElMessage.success('出库成功');
      showStockOutDialog.value = false;
      stockOutFormRef.value.resetFields();
      selectedMaterial.value = null;
      await refreshMaterials();
    } else {
      ElMessage.error(response.message || '出库失败');
    }
  } catch (error) {
    console.error('表单验证失败:', error);
  } finally {
    processingStock.value = false;
  }
};

// 组件挂载时加载数据
onMounted(() => {
  refreshMaterials();
});
</script>

<style scoped>
.material-list-view {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

/* 页面标题 */
.page-header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  margin-bottom: 8px;
}

.header-left h1 {
  font-size: 24px;
  font-weight: 600;
  color: #1f2937;
  margin: 0 0 4px 0;
}

.header-left p {
  font-size: 14px;
  color: #6b7280;
  margin: 0;
}

.header-right {
  display: flex;
  gap: 12px;
}

/* 统计卡片 */
.stats-cards {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(240px, 1fr));
  gap: 16px;
}

.stat-card {
  border-radius: 12px;
  border: 1px solid #e5e7eb;
}

.stat-content {
  display: flex;
  align-items: center;
  gap: 16px;
}

.stat-icon {
  width: 56px;
  height: 56px;
  border-radius: 12px;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 24px;
}

.stat-icon .el-icon {
  color: white;
}

.total-materials {
  background-color: #3b82f6;
}

.low-stock {
  background-color: #f59e0b;
}

.out-of-stock {
  background-color: #ef4444;
}

.stock-value {
  background-color: #10b981;
}

.stat-info {
  flex: 1;
}

.stat-value {
  font-size: 24px;
  font-weight: 700;
  color: #1f2937;
  line-height: 1;
}

.stat-label {
  font-size: 14px;
  color: #6b7280;
  margin-top: 4px;
}

/* 筛选工具栏 */
.filter-card {
  border-radius: 12px;
  border: 1px solid #e5e7eb;
}

.filter-toolbar {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

@media (min-width: 768px) {
  .filter-toolbar {
    flex-direction: row;
    align-items: center;
  }
}

.search-input {
  flex: 1;
}

.filter-actions {
  display: flex;
  gap: 12px;
  flex-wrap: wrap;
}

/* 物料表格卡片 */
.materials-table-card {
  border-radius: 12px;
  border: 1px solid #e5e7eb;
}

.table-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.table-header h3 {
  margin: 0;
  font-size: 18px;
  font-weight: 600;
  color: #1f2937;
}

/* 物料单元格 */
.material-cell {
  display: flex;
  flex-direction: column;
}

.material-name {
  font-weight: 500;
  color: #1f2937;
}

.material-desc {
  font-size: 12px;
  color: #6b7280;
  margin-top: 2px;
}

/* 库存单元格 */
.stock-cell {
  display: flex;
  align-items: center;
  gap: 4px;
}

.stock-normal {
  color: #10b981;
  font-weight: 600;
}

.stock-low {
  color: #f59e0b;
  font-weight: 600;
}

.stock-out {
  color: #ef4444;
  font-weight: 600;
}

.stock-unit {
  font-size: 12px;
  color: #6b7280;
}

/* 操作按钮 */
.action-buttons {
  display: flex;
  gap: 8px;
}

/* 分页 */
.pagination-container {
  display: flex;
  justify-content: flex-end;
  margin-top: 24px;
  padding-top: 16px;
  border-top: 1px solid #e5e7eb;
}

/* 交易记录卡片 */
.transactions-card {
  border-radius: 12px;
  border: 1px solid #e5e7eb;
}

.transactions-card h3 {
  margin: 0;
  font-size: 18px;
  font-weight: 600;
  color: #1f2937;
}

.transactions-list {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.transaction-item {
  display: flex;
  align-items: center;
  gap: 16px;
  padding: 12px;
  border-radius: 8px;
  background-color: #f9fafb;
  border: 1px solid #e5e7eb;
}

.transaction-icon {
  width: 40px;
  height: 40px;
  border-radius: 8px;
  background-color: #3b82f6;
  color: white;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 18px;
  flex-shrink: 0;
}

.transaction-icon .el-icon {
  color: white;
}

.transaction-content {
  flex: 1;
}

.transaction-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 4px;
}

.transaction-type {
  font-weight: 500;
  padding: 2px 8px;
  border-radius: 4px;
  font-size: 12px;
}

.transaction-in {
  background-color: #d1fae5;
  color: #065f46;
}

.transaction-out {
  background-color: #fee2e2;
  color: #991b1b;
}

.transaction-amount {
  font-weight: 600;
  color: #1f2937;
}

.transaction-details {
  display: flex;
  justify-content: space-between;
  font-size: 12px;
  color: #6b7280;
}

.empty-transactions {
  padding: 40px 0;
}

/* 对话框 */
.dialog-footer {
  display: flex;
  justify-content: flex-end;
  gap: 12px;
}

.form-hint {
  margin-left: 8px;
  color: #6b7280;
  font-size: 12px;
}
</style>
