#!/usr/bin/env python3
"""
乱码快速诊断工具
使用方法: python3 check_garbled.py [module1,module2,...]

不传参数时检查所有关键模块
"""
import urllib.request
import json
import sys

MODULES = [
    ('keys', 'http://localhost:5019/api/keys?page=1&pageSize=1'),
    ('residents', 'http://localhost:5019/api/residents?page=1&pageSize=1'),
    ('visitors', 'http://localhost:5019/api/visitors?page=1&pageSize=1'),
    ('materials', 'http://localhost:5019/api/materials?page=1&pageSize=1'),
    ('complaints', 'http://localhost:5019/api/complaints?page=1&pageSize=1'),
    ('contracts', 'http://localhost:5019/api/contracts?page=1&pageSize=1'),
    ('cleaning', 'http://localhost:5019/api/cleaning-records?page=1&pageSize=1'),
    ('tickets', 'http://localhost:5002/api/tickets?page=1&pageSize=1'),
    ('finance', 'http://localhost:5019/api/finance-records?page=1&pageSize=1'),
]

# 乱码特征字节（双重编码 mojibake）
GARBLED_PATTERNS = [b'c3a5', b'c3a6', b'c2bc', b'c2a0', b'c3b8']  # Ã¥, Ã¦, Â¼, Â , Ã¸

def check_module(name, url):
    """检查单个模块是否有乱码"""
    try:
        req = urllib.request.Request(url)
        req.add_header('Accept', 'application/json')
        with urllib.request.urlopen(req, timeout=5) as resp:
            raw = resp.read()
            
        # 检查原始字节是否有乱码特征
        has_garbled = any(pattern in raw for pattern in GARBLED_PATTERNS)
        
        # 尝试解析 JSON 看数据
        try:
            data = json.loads(raw)
            first_item = None
            if 'data' in data and len(data['data']) > 0:
                first_item = data['data'][0]
                # 找第一个文本字段
                for key in ['name', 'Name', 'title', 'Title', 'description', 'Description']:
                    if key in first_item and first_item[key]:
                        text_val = first_item[key]
                        if 'Ã' in text_val or 'â' in text_val:
                            has_garbled = True
                        break
        except:
            pass
        
        return (name, url, has_garbled, len(raw), None)
    except Exception as e:
        return (name, url, None, 0, str(e))

def main():
    target_modules = None
    if len(sys.argv) > 1:
        target_modules = set(sys.argv[1].split(','))
    
    print("=== WO-Property 乱码快速诊断 ===")
    print()
    
    results = []
    for name, url in MODULES:
        if target_modules and name not in target_modules:
            continue
        
        module_name, _, has_garbled, size, error = check_module(name, url)
        
        if error:
            print(f"  [{name}] ❌ 连接失败: {error}")
        elif has_garbled:
            print(f"  [{name}] ❌ 存在乱码 (响应 {size}B)")
        elif has_garbled is False:
            print(f"  [{name}] ✅ 数据正常 (响应 {size}B)")
        else:
            print(f"  [{name}] ⚠️ 未知")
        
        results.append((module_name, has_garbled))
    
    print()
    
    # 汇总
    garbled_count = sum(1 for _, g in results if g is True)
    clean_count = sum(1 for _, g in results if g is False)
    
    if garbled_count > 0:
        print(f"❌ {garbled_count} 个模块存在乱码，需要修复连接字符串")
        sys.exit(1)
    else:
        print(f"✅ 所有 {clean_count} 个模块数据正常")
        sys.exit(0)

if __name__ == '__main__':
    main()