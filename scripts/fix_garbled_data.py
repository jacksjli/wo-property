#!/usr/bin/env python3
"""批量修复 MySQL 双重编码乱码（修正列名版本）"""
import mysql.connector

conn = mysql.connector.connect(
    host='localhost',
    user='woproperty',
    password='WOProperty2026!',
    database='wo_property',
    charset='utf8mb4',
    init_command='SET NAMES utf8mb4'
)
cursor = conn.cursor()

# 使用实际数据库列名
tables_to_fix = [
    ('Residents', 'Id', ['Name', 'Phone']),
    ('Visitors', 'Id', ['VisitorName']),  # 无 Company 列
    ('Tickets', 'Id', ['Title', 'Description', 'Location']),
    ('FinanceRecords', 'Id', ['Description', 'RelatedParty', 'Handler']),
    ('Contracts', 'Id', ['ContractName']),
    ('materials', 'Id', ['Name', 'Description']),
    ('complaints', 'Id', ['Title', 'Description']),
    ('CleaningRecords', 'Id', ['CleanerName', 'CleaningArea']),  # 无 PersonName/Space
    ('CommunityActivities', 'Id', ['ActivityTitle', 'Organizer', 'Description']),
    ('RenovationRequests', 'Id', ['ApplicantName', 'Description']),
    ('ExpressRecords', 'Id', ['RecipientName']),
    ('DeliveryRequests', 'Id', ['ItemDescription']),
    ('Devices', 'Id', ['DeviceName', 'Location']),
    ('InspectionRecords', 'Id', ['InspectionTitle', 'Result']),
    ('keys', 'Id', ['Name', 'Location']),  # 无 KeyName
    ('ParkingRecords', 'Id', ['LicensePlate']),  # 无 OwnerName
    ('PaymentRecords', 'Id', ['Remarks']),
    ('Personnel', 'Id', ['Name']),
    ('TakeoutOrders', 'Id', ['Description']),
]

total_fixed = 0

for table, id_col, fields in tables_to_fix:
    for field in fields:
        try:
            cursor.execute(f"SELECT `{id_col}`, `{field}` FROM `{table}` WHERE `{field}` IS NOT NULL AND `{field}` != ''")
            rows = cursor.fetchall()
            for row in rows:
                record_id, value = row
                if value is None:
                    continue
                # 检查是否是乱码（双重编码特征）
                if not any(x in value for x in ['Ã', 'â', 'Â', 'Å', 'Ã§']):
                    continue
                try:
                    raw_bytes = value.encode('latin1')
                    corrected = raw_bytes.decode('utf-8')
                    cursor.execute(f"UPDATE `{table}` SET `{field}`=%s WHERE `{id_col}`=%s", (corrected, record_id))
                    print(f"✓ {table}.{field} ID={record_id}: {corrected!r}")
                    total_fixed += 1
                except Exception:
                    pass
        except Exception as e:
            pass

conn.commit()
cursor.close()
conn.close()
print(f"\n修复完成: {total_fixed} 条")