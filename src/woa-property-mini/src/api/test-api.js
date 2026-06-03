/**
 * 小程序 API 集成测试
 * 运行方式: node src/api/test-api.js
 */

const API_BASE = 'http://192.168.1.3:5000/api'

let passed = 0, failed = 0

function pass(name) {
  console.log(`✅ ${name}`)
  passed++
}
function fail(name, msg) {
  console.log(`❌ ${name}: ${msg}`)
  failed++
}

async function api(path, method = 'GET', data = null, headers = {}) {
  const opts = { method, headers: { 'Content-Type': 'application/json', ...headers } }
  if (data) opts.body = JSON.stringify(data)
  const res = await fetch(`${API_BASE}${path}`, opts)
  const text = await res.text()
  try { return JSON.parse(text) } catch { return { success: false, error: `非JSON响应: ${text.substring(0, 100)}` } }
}

async function run() {
  console.log('============================================')
  console.log('    微信小程序 API 集成测试')
  console.log('============================================\n')

  // 1. 登录
  console.log('【登录 API】')
  const login = await api('/auth/login-by-person', 'POST', {
    name: '张维修', phone: '15923123408', tenantCode: 'wo_property'
  })
  if (!login.success) { fail('登录', login.message); process.exit(1) }
  pass('登录返回 success')
  if (!login.data?.token) { fail('token', '无token'); process.exit(1) }
  if (!login.data.token.startsWith('eyJ')) { fail('JWT格式', login.data.token.substring(0,20)); process.exit(1) }
  pass('token 为 JWT 格式')
  if (!Array.isArray(login.data.projects) || login.data.projects.length === 0) {
    fail('projects', '非数组或为空')
  } else {
    pass('返回 projects 数组')
    if (!login.data.projects[0].projectCode) fail('projectCode', '缺失')
    else pass('projects 含 projectCode')
    console.log(`  projects: ${login.data.projects.map(p => p.projectCode).join(', ')}`)
  }
  const token = login.data.token
  const h = { 'Authorization': `Bearer ${token}`, 'Tenant-Code': 'wo_property' }

  // 2. 工单类型
  console.log('\n【工单类型 API】')
  const types = await api('/ticket-types', 'GET', null, h)
  if (!types.success) fail('工单类型', types.message)
  else pass('返回 success')
  if (!types.data?.length) fail('工单类型数据', '为空')
  else pass(`返回 ${types.data.length} 条工单类型`)
  if (!types.data[0]?.id || !types.data[0]?.name) fail('字段完整', '缺失')
  else pass('每条含 id/name/code')

  // 3. 工种
  console.log('\n【工种 API】')
  const jobs = await api('/job-types', 'GET', null, h)
  if (!jobs.success) fail('工种', jobs.message)
  else pass('返回 success')
  if (!jobs.data?.length) fail('工种数据', '为空')
  else pass(`返回 ${jobs.data.length} 条工种`)
  if (typeof jobs.data[0]?.ticket_type_id !== 'number') fail('ticket_type_id', '缺失或非数字')
  else pass('工种含 ticket_type_id')

  // 4. 区域
  console.log('\n【区域 API】')
  const areas = await api('/areas', 'GET', null, h)
  if (!areas.success) fail('区域', areas.message)
  else pass('返回 success')
  if (!areas.data?.length) fail('区域数据', '为空')
  else pass(`返回 ${areas.data.length} 个区域`)

  // 5. 楼栋
  console.log('\n【楼栋 API】')
  const buildings = await api('/buildings', 'GET', null, h)
  if (!buildings.success) fail('楼栋', buildings.message)
  else pass('返回 success')
  if (!buildings.data?.length) fail('楼栋数据', '为空')
  else pass(`返回 ${buildings.data.length} 个楼栋`)

  // 6. 创建工单
  console.log('\n【创建工单 API】')
  const createRes = await fetch(`${API_BASE}/tickets`, {
    method: 'POST',
    headers: {
      ...h,
      'X-Project': 'YGHY001',
      'Content-Type': 'application/json',
      'Host': '192.168.1.3:5000'
    },
    body: JSON.stringify({
      title: '【自动化测试】API单元测试工单',
      description: '自动化测试描述',
      ticketTypeId: 1, jobTypeId: 1, areaId: 8, buildingId: 11,
      room: '101', location: '东区 1栋 101',
      contactPersonName: '测试员', contactPhone: '13800138001',
      projectCode: 'YGHY001', priority: 'Medium'
    })
  })
  const create = await createRes.json()
  if (!create.success) fail('创建工单', create.message)
  else pass('返回 success')
  const ticketCode = create.data?.ticketCode
  if (!ticketCode) fail('ticketCode', '无编号')
  else {
    console.log(`  工单编号: ${ticketCode}`)
    if (!ticketCode.startsWith('YGHY001-WO-')) fail('项目前缀', ticketCode)
    else pass('ticketCode 含项目前缀 YGHY001-WO-')
    if (!/^YGHY001-WO-\d{6}-\d{5}$/.test(ticketCode)) fail('编号格式', ticketCode)
    else pass('编号格式正确 YGHY001-WO-YYYYMM-NNNNN')
  }

  // 7. 查询工单
  console.log('\n【查询工单 API】')
  const list = await api('/tickets?page=1&pageSize=10', 'GET', null, { ...h, 'X-Project': 'YGHY001' })
  if (!list.success) fail('查询', list.message)
  else pass('返回 success')
  if (!Array.isArray(list.data)) fail('工单列表', '非数组')
  else {
    pass(`返回 ${list.data.length} 条工单`)
    if (list.data.length > 0 && list.data[0].projectCode !== 'YGHY001') {
      fail('projectCode过滤', list.data[0].projectCode)
    } else pass('projectCode 过滤正确')
  }

  // 8. 工单详情
  console.log('\n【工单详情 API】')
  const ticketId = list.data?.find(t => t.ticketCode?.includes('YGHY001-WO-'))?.id || list.data?.[0]?.id
  if (!ticketId) { console.log('  无工单，跳过'); }
  else {
    const detail = await api(`/tickets/${ticketId}`, 'GET', null, { ...h, 'X-Project': 'YGHY001' })
    if (!detail.success) fail('详情', detail.message)
    else pass('返回 success')
    if (!detail.data?.ticketCode) fail('ticketCode', '无编号')
    else pass('详情含 ticketCode')
  }

  console.log('\n============================================')
  console.log(`    测试结果: ${passed} 通过, ${failed} 失败`)
  console.log('============================================')
  process.exit(failed > 0 ? 1 : 0)
}

run().catch(e => { console.error(e); process.exit(1) })