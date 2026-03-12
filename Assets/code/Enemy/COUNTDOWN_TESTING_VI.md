# 🧪 COUNTDOWN TIMER - HƯỚNG DẪN TEST

## 📌 Mục tiêu
Xác minh rằng tất cả event trigger đúng thời gian theo countdown timer.

---

## 🔧 Chuẩn Bị

### 1. Mở Scene chứa EventEnemySpawner

### 2. Tìm GameObject có component EventEnemySpawner

### 3. Trong Inspector, bật Debug Mode:
```
Debug
├─ Show Debug Info: ✓ (bật)
```

### 4. Mở Console
Ctrl + Shift + C

---

## 🎮 TEST 1: Kiểm tra Timeline

### Bước 1: Chạy game

### Bước 2: Quan sát Console

Bạn sẽ thấy các log theo thứ tự:

```
[EventEnemySpawner] CheckEventTrigger: 20.00 phút, isEventActive=False, pendingEvents=0
[EventEnemySpawner] CheckEventTrigger: 19.99 phút, isEventActive=False, pendingEvents=0
...
[EventEnemySpawner] CheckEventTrigger: 16.00 phút, isEventActive=False, pendingEvents=0
[EventEnemySpawner] ✓ Trigger event 5 tại 16 phút!
[EventEnemySpawner] Event bắt đầu!
```

### Bước 3: Chờ đến 13 phút

Bạn sẽ thấy:
```
[EventEnemySpawner] CheckEventTrigger: 13.00 phút, isEventActive=True, pendingEvents=0
[EventEnemySpawner] ✓ Trigger event 4 tại 13 phút!
[EventEnemySpawner] Event đang chạy, thêm event 4 vào queue
```

### Bước 4: Chờ đến 10 phút

Bạn sẽ thấy:
```
[EventEnemySpawner] CheckEventTrigger: 10.00 phút, isEventActive=True, pendingEvents=1
[EventEnemySpawner] ✓ Trigger event 3 tại 10 phút!
[EventEnemySpawner] Event đang chạy, thêm event 3 vào queue
```

### ✅ Kết quả mong đợi

| Thời gian | Sự kiện | Log |
|-----------|---------|-----|
| 16:00 | Event 5 trigger | ✓ Trigger event 5 |
| 13:00 | Event 4 vào queue | Event đang chạy, thêm event 4 |
| 10:00 | Event 3 vào queue | Event đang chạy, thêm event 3 |
| 7:00 | Event 2 vào queue | Event đang chạy, thêm event 2 |
| 4:00 | Event 1 vào queue | Event đang chạy, thêm event 1 |
| 1:00 | Event 0 vào queue | Event đang chạy, thêm event 0 |

---

## 🎮 TEST 2: Kiểm tra Queue hoạt động

### Bước 1: Theo dõi pendingEvents

Mỗi khi một event mới trigger:
- `pendingEvents` tăng lên 1
- Ví dụ: `pendingEvents=0` → `pendingEvents=1` → `pendingEvents=2` → ...

### Bước 2: Khi Event 5 kết thúc (25 giây sau)

```
[EventSpawner] Event Loại 1 kết thúc! Xử lý 30 enemy.
[EventSpawner] Event kết thúc, trigger event tiếp theo từ queue
[EventEnemySpawner] Event bắt đầu!
```

- `pendingEvents` giảm đi 1
- Event tiếp theo trigger

### ✅ Kết quả mong đợi

```
Thời gian    pendingEvents    Sự kiện
─────────────────────────────────────────
16:00        0                Event 5 trigger
13:00        1                Event 4 vào queue
10:00        2                Event 3 vào queue
7:00         3                Event 2 vào queue
4:00         4                Event 1 vào queue
1:00         5                Event 0 vào queue
(Event 5 kết thúc)
             4                Event 4 trigger
(Event 4 kết thúc)
             3                Event 3 trigger
(Event 3 kết thúc)
             2                Event 2 trigger
(Event 2 kết thúc)
             1                Event 1 trigger
(Event 1 kết thúc)
             0                Event 0 trigger
(Event 0 kết thúc)
             0                Không có event nào
```

---

## 🎮 TEST 3: Kiểm tra Enemy spawn

### Bước 1: Chờ đến 16:00 (Event 5 trigger)

Bạn sẽ thấy:
- ⚠️ Warning nhấp nháy (trái-phải hoặc trên-dưới)
- 👾 Enemy bắt đầu spawn từ các hướng

### Bước 2: Quan sát Enemy

- ✅ Enemy di chuyển theo hướng được chỉ định
- ✅ Enemy có máu cao hơn bình thường
- ✅ Enemy di chuyển nhanh hơn

### Bước 3: Chờ đến 13:00 (Event 4 trigger)

Bạn sẽ thấy:
- ⚠️ Warning nhấp nháy lại
- 👾 Enemy event 4 bắt đầu spawn

### ✅ Kết quả mong đợi

- ✅ Tất cả 6 event (0-5) đều spawn enemy
- ✅ Không có event nào bị bỏ qua
- ✅ Enemy event được destroy khi event kết thúc

---

## 🎮 TEST 4: Kiểm tra Countdown Logic

### Bước 1: Kiểm tra điều kiện

Mở code `EventEnemySpawner.cs` và tìm:

```csharp
if (currentMinutes <= eventMinutes[i] && !triggeredEvents.Contains(i))
```

✅ Phải là `<=` chứ không phải `>=`

### Bước 2: Kiểm tra cả hai hàm

- `CheckEventTrigger()` - dòng ~115
- `TryTriggerCatchUp()` - dòng ~335

Cả hai phải dùng `<=`

### ✅ Kết quả mong đợi

- ✅ Cả hai hàm dùng `<=`
- ✅ Event trigger theo countdown timer
- ✅ Không có event nào trigger sớm

---

## 🎮 TEST 5: Kiểm tra Warning

### Bước 1: Chờ đến 16:00 (Event 5 trigger)

Bạn sẽ thấy:
- ⚠️ Warning GameObject nhấp nháy 7 giây

### Bước 2: Quan sát Warning

- Nếu `currentSpawnDirection = LeftRight`: Warning trái-phải nhấp nháy
- Nếu `currentSpawnDirection = TopBottom`: Warning trên-dưới nhấp nháy

### Bước 3: Sau 7 giây

- ⚠️ Warning tắt
- 👾 Enemy bắt đầu spawn

### ✅ Kết quả mong đợi

- ✅ Warning nhấp nháy đúng 7 giây
- ✅ Warning tắt trước khi enemy spawn
- ✅ Tất cả warning đều tắt khi event kết thúc

---

## 📊 Bảng Kiểm Tra

### Checklist trước test

- [ ] EventEnemySpawner được gán đúng
- [ ] TimerManager được tìm thấy
- [ ] Player được tìm thấy
- [ ] SpawnEnemy được gán (nếu dùng mode SpawnNew)
- [ ] eventMinutes được cấu hình: [1, 4, 7, 10, 13, 16]
- [ ] Warning GameObject được gán
- [ ] showDebugInfo bật
- [ ] Console mở

### Checklist sau test

- [ ] Event 5 trigger ở 16:00 ✓
- [ ] Event 4 trigger ở 13:00 ✓
- [ ] Event 3 trigger ở 10:00 ✓
- [ ] Event 2 trigger ở 7:00 ✓
- [ ] Event 1 trigger ở 4:00 ✓
- [ ] Event 0 trigger ở 1:00 ✓
- [ ] Queue hoạt động đúng ✓
- [ ] Enemy spawn đúng ✓
- [ ] Warning hiển thị đúng ✓
- [ ] Không có lỗi trong Console ✓

---

## [object Object]ESHOOTING

### Vấn đề 1: Event không trigger

**Nguyên nhân:**
- `TimerManager` không được gán
- `eventMinutes` không được cấu hình đúng
- Code chưa được sửa

**Giải pháp:**
1. Kiểm tra `TimerManager` được gán
2. Kiểm tra `eventMinutes = [1, 4, 7, 10, 13, 16]`
3. Kiểm tra code dùng `<=` chứ không phải `>=`

### Vấn đề 2: Event trigger sớm

**Nguyên nhân:**
- Dùng `>=` thay vì `<=`
- Logic countdown sai

**Giải pháp:**
1. Kiểm tra `CheckEventTrigger()` dùng `<=`
2. Kiểm tra `TryTriggerCatchUp()` dùng `<=`
3. Restart Unity

### Vấn đề 3: Event trigger muộn

**Nguyên nhân:**
- Timer không đếm xuống đúng
- Event Minutes sai

**Giải pháp:**
1. Kiểm tra Timer đếm từ 20 xuống 0
2. Kiểm tra Event Minutes: [1, 4, 7, 10, 13, 16]

### Vấn đề 4: Enemy không spawn

**Nguyên nhân:**
- `SpawnEnemy` không được gán
- `Player` không được tìm thấy

**Giải pháp:**
1. Kiểm tra `SpawnEnemy` được gán
2. Kiểm tra Player có tag "Player"

---

## ✅ Kết Luận

Nếu tất cả các test đều pass, sửa lỗi đã thành công! 🎉

**Tất cả event sẽ trigger đúng thời gian theo countdown timer.**

---

**Chúc bạn test thành công!** 🚀

