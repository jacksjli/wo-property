#!/usr/bin/env python3
"""
WO-Property 中文数据验证脚本
使用方法: python3 validate_chinese_data.py [table]

不传参数时检查所有关键表。
"""
import mysql.connector
import sys

TABLES_TO_CHECK = [
    ('Tickets', ['Title', 'Description', 'Location']),
    ('FinanceRecords', ['description', 'relatedParty', 'handler']),
    ('CleaningRecords', ['personName', 'space', 'description']),
    ('CommunityActivities', ['activityName', 'organizer', 'location', 'description']),
    ('RenovationRequests', ['applicantName', 'location', 'description']),
    ('ExpressRecords', ['senderName', 'receiverName', 'description']),
    ('DeliveryRequests', ['description']),
    ('Devices', ['name', 'model', 'manufacturer', 'location']),
    ('Contracts', ['contractName', 'contractNo']),
    ('Materials', ['name', 'description']),
    ('Complaints', ['title', 'description', 'complainant']),
    ('Inspections', ['inspectionPoint', 'result', 'remark']),
    ('Keys', ['keyName', 'location']),
    ('ParkingRecords', ['plateNumber', 'ownerName']),
    ('Payments', ['description']),
    ('Visitors', ['visitorName', 'company']),
    ('Residents', ['name']),
    ('Personnel', ['name']),
    ('TakeoutOrders', ['description']),
]

def check_table(table, columns):
    try:
        conn = mysql.connector.connect(
            host="localhost",
            user="woproperty",
            password="WOProperty2026!",
            database="wo_property",
            charset="utf8mb4",
            init_command="SET NAMES utf8mb4"
        )
        cursor = conn.cursor()
        errors = []
        for col in columns:
            try:
                cursor.execute(f"SELECT COUNT(*) FROM `{table}` WHERE `{col}` IS NOT NULL AND (`{col}` LIKE '%Ã%' OR `{col}` LIKE '%â%' OR `{col}` LIKE '%Â%' OR `{col}` LIKE '%Ã§%' OR `{col}` LIKE '%Å%')")
                count = cursor.fetchone()[0]
                if count > 0:
                    errors.append(f"  {table}.{col}: {count} 条乱码")
            except mysql.Error:
                pass
        cursor.close()
        conn.close()
        return errors
    except Exception as e:
        return [f"  {table}: 检查失败 - {e}"]

if __name__ == '__main__':
    target_tables = None
    if len(sys.argv) > 1:
        target_tables = [sys.argv[1]]

    all_errors = []
    for table, cols in TABLES_TO_CHECK:
        if target_tables and table not in target_tables:
            continue
        errors = check_table(table, cols)
        if errors:
            all_errors.extend(errors)

    if all_errors:
        print("❌ 发现乱码数据：")
        for e in all_errors:
            print(e)
        sys.exit(1)
    else:
        print("✅ 所有数据正常，无乱码")
        sys.exit(0)