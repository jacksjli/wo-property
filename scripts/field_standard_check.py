#!/usr/bin/env python3
"""
field_standard_check.py - 字段标准化自动检查脚本

执行方式：
  python3 scripts/field_standard_check.py --check-module ticket
  python3 scripts/field_standard_check.py --full-report
  python3 scripts/field_standard_check.py --fix-missing

功能：
  1. 检查新增模块是否已注册 FieldDefinitions
  2. 检查等价映射完整性（从数据库实时读）
  3. 检查 OwnerModule + IsEditable 配置
  4. 自动补全缺失的模块配置（--fix-missing）
"""

import argparse
import mysql.connector

DB = {
    "host": "localhost",
    "user": "woproperty",
    "password": "WOProperty2026!",
    "database": "wo_property",
    "charset": "utf8mb4"
}

STANDARD_MODULES = [
    "region", "area", "building", "room", "department", "jobType", "deviceType", "supplier",
    "fieldDefinition", "key", "accessControl", "resident", "parking", "renovation",
    "finance", "payment", "contract", "ticket", "inspection", "dispatch", "timeout",
    "ticketType", "device", "material", "cleaning", "projectTracking", "express",
    "visitor", "community", "personnel", "notification", "statistics", "project"
]

REFERENCE_MODULES = ["region", "area", "building", "room", "department", "jobType", "deviceType"]


def get_conn():
    return mysql.connector.connect(**DB)


def check_missing_modules():
    conn = get_conn()
    cursor = conn.cursor()
    cursor.execute("SELECT DISTINCT Module FROM FieldDefinitions")
    configured = set(r[0] for r in cursor)
    conn.close()
    return [m for m in STANDARD_MODULES if m not in configured]


def check_owner_module_config():
    conn = get_conn()
    cursor = conn.cursor()
    issues = []
    cursor.execute("""
        SELECT Module, OwnerModule, IsEditable, COUNT(*) as cnt
        FROM ModuleFields
        WHERE OwnerModule IS NOT NULL
        GROUP BY Module, OwnerModule, IsEditable
    """)
    for module, owner, editable, cnt in cursor:
        if module in REFERENCE_MODULES and editable == 1:
            issues.append(f"  [WARN] {module}: 应该 IsEditable=0，实际={editable}")
        elif module not in REFERENCE_MODULES and editable == 0:
            issues.append(f"  [WARN] {module}: 应该 IsEditable=1，实际={editable}")
    conn.close()
    return issues


def check_equivalence_completeness():
    """从数据库实时读 FieldDefinitions，检查哪些字段还未进等价表"""
    conn = get_conn()
    cursor = conn.cursor()
    # 取已进等价表的字段
    cursor.execute("SELECT EquivalentFieldKey FROM FieldDefinitionEquivalents")
    in_eq = {r[0] for r in cursor}
    # 取所有字段定义
    cursor.execute("SELECT FieldKey FROM FieldDefinitions")
    all_fields = [r[0] for r in cursor]
    conn.close()

    phone_kw, name_kw, status_kw = ["phone", "Phone"], ["name", "Name"], ["status", "Status"]
    issues = []
    for f in all_fields:
        if f in in_eq or f in ["phone", "phone_number", "name", "person_name", "status"]:
            continue
        if any(k in f for k in phone_kw):
            issues.append(f"  [INFO] phone_number <- {f} (未在等价表)")
        elif any(k in f for k in name_kw):
            issues.append(f"  [INFO] person_name <- {f} (未在等价表)")
        elif any(k in f for k in status_kw):
            issues.append(f"  [INFO] status <- {f} (未在等价表)")
    return issues


def fix_missing_modules(missing_modules):
    if not missing_modules:
        print("  无需补全")
        return
    conn = get_conn()
    cursor = conn.cursor()
    for module in missing_modules:
        is_edit = 0 if module in REFERENCE_MODULES else 1
        cursor.execute("""
            INSERT INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, IsShared, Module, Status, CreatedAt)
            VALUES (%s, %s, 'text', %s, 0, %s, 'active', NOW())
        """, (f"{module}_name", f"{module}名称", module, module))
        cursor.execute("SELECT LAST_INSERT_ID()")
        fd_id = cursor.fetchone()[0]
        cursor.execute("""
            INSERT INTO ModuleFields (Module, FieldDefinitionId, IsVisible, IsActive, SortOrder, OwnerModule, IsEditable, CreatedAt)
            VALUES (%s, %s, 1, 1, 1, %s, %s, NOW())
        """, (module, fd_id, module, is_edit))
        print(f"  [FIX] {module}: IsEditable={is_edit}")
    conn.commit()
    conn.close()


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--check-module", help="检查指定模块")
    parser.add_argument("--full-report", action="store_true", help="完整报告")
    parser.add_argument("--fix-missing", action="store_true", help="自动补全缺失")
    args = parser.parse_args()

    print("=" * 56)
    print("  字段标准化检查")
    print("=" * 56)
    print()

    if args.check_module:
        conn = get_conn()
        cursor = conn.cursor()
        cursor.execute("SELECT COUNT(*) FROM FieldDefinitions WHERE Module = %s", (args.check_module,))
        cnt = cursor.fetchone()[0]
        cursor.execute("SELECT COUNT(*) FROM ModuleFields WHERE Module = %s", (args.check_module,))
        mf = cursor.fetchone()[0]
        conn.close()
        print(f"[模块] {args.check_module}")
        print(f"  FieldDefinitions: {cnt} 条")
        print(f"  ModuleFields:     {mf} 条")
        return

    if args.full_report or args.fix_missing:
        print("[1/3] 检查缺失模块...")
        missing = check_missing_modules()
        if missing:
            print(f"  缺失 {len(missing)} 个: {missing}")
            if args.fix_missing:
                print("  补全中...")
                fix_missing_modules(missing)
        else:
            print("  ✅ 34个模块已全部配置")

        print()
        print("[2/3] 检查 OwnerModule + IsEditable...")
        issues = check_owner_module_config()
        if issues:
            for i in issues: print(i)
        else:
            print("  ✅ OwnerModule 配置正确")

        print()
        print("[3/3] 检查等价映射完整性...")
        issues = check_equivalence_completeness()
        if issues:
            for i in issues: print(i)
        else:
            print("  ✅ 等价映射完整")

        return

    parser.print_help()


if __name__ == "__main__":
    main()
