# 乱码问题排查 Checklist

当发现任意模块出现中文乱码时，按照以下顺序执行：

---

## 第一步：确认是乱码，不是其他问题

```bash
# 1. 通过 API 读取原始响应
curl -s http://localhost:5019/api/{module}?page=1&pageSize=1

# 2. 检查是否包含乱码特征字节
# mojibake 典型字节：c3a5 (=Ã¥), c3a6 (=Ã¦), c2bc (=Â¼), c2a0 (=Â )
python3 -c "import urllib.request; raw=urllib.request.urlopen('URL').read(); print('c3a5' in raw, 'c3a6' in raw)"

# 3. MySQL 直接查询 HEX
mysql -u woproperty -pWOProperty2026! wo_property -e "SELECT id, HEX(name) FROM {table} LIMIT 1"
# 对比正确 UTF-8：
# 张 = E5BCA0
# 正 = E6ADA3
# 如果看到 C3A5 或其他非标准开头 = 乱码
```

**结论**：
- API raw bytes 含 c3a5 → 乱码（传输层问题）
- MySQL HEX 不是 E5/E6/EF 开头 → 乱码（存储层问题）
- 两者都正常 → 不是乱码，是其他显示问题

---

## 第二步：确定乱码来源

### 如果是新建模块（含新数据）
→ 问题在连接字符串。检查：
```bash
grep -rn "MySqlConnection\|CharSet\|Pooling" src/WO.Property.MasterDataService/Program.cs
```
缺失 `CharSet=utf8mb4;Pooling=false` → 修复并重启服务

### 如果是历史数据
→ 历史写入时连接字符串有问题。无法自动修复，直接重建数据：
```bash
mysql -u woproperty -pWOProperty2026! wo_property -e "TRUNCATE TABLE {table}"
# 通过 API 重新插入干净数据
for name in "张三" "李四" ...; do
    curl -X POST http://localhost:5019/api/{module} -d "{\"name\":\"$name\"}"
done
```

---

## 第三步：修复后验证

```bash
# 运行诊断工具
python3 scripts/check_garbled.py {module}

# 或手动验证
curl -s http://localhost:5019/api/{module}?page=1 | python3 -c "
import sys, json
data = json.load(sys.stdin)
for item in data['data'][:3]:
    for key in ['name', 'title', 'description']:
        if key in item:
            print(f'{key}={item[key]!r}')
"
```

预期：返回正确中文，无乱码字节

---

## 快速参考

| 症状 | 根因 | 修复 |
|------|------|------|
| API 返回 `å¼\xa0` | 连接未设 charset | 加 CharSet=utf8mb4;Pooling=false |
| MySQL HEX 是 C3A5... | 写入时 latin1 编码 | 重建数据 |
| MySQL HEX 是 E5BCA0 | 存储正常，传输问题 | 检查 API 编码 |

---

_最后更新：2026-05-09_