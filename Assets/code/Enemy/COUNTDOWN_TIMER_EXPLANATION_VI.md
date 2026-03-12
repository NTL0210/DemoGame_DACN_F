# ⏱️ COUNTDOWN TIMER - GIẢI THÍCH TIMELINE

## 📌 Tóm tắt
TimeManager của bạn đếm **từ 20 phút xuống 0** (countdown), không phải đếm lên từ 0.

---

## 🔄 So sánh hai loại Timer

### Timer Loại 1: Count Up (Đếm lên)
```
0 → 1 → 2 → 3 → 4 → ... → 19 → 20
```
- Kiểm tra: `currentMinutes >= eventMinutes[i]`
- Ví dụ: Khi đạt 1 phút, trigger event 0

### Timer Loại 2: Count Down (Đếm xuống) ← **Của bạn**
```
20 → 19 → 18 → 17 → 16 → ... → 1 → 0
```
- Kiểm tra: `currentMinutes <= eventMinutes[i]`
- Ví dụ: Khi xuống 16 phút, trigger event 5

---

## 📊 Timeline của bạn

### Event Minutes
```
Element 0: 1 phút
Element 1: 4 phút
Element 2: 7 phút
Element 3: 10 phút
Element 4: 13 phút
Element 5: 16 phút
```

### Timeline Countdown
```
Thời gian    Sự kiện
─────────────────────────────────────
20:00        Game bắt đầu
19:00        Chưa có event nào
18:00        Chưa có event nào
17:00        Chưa có event nào
16:00        ✓ Event 5 trigger (16 <= 16)
15:00        Chưa có event nào
14:00        Chưa có event nào
13:00        ✓ Event 4 trigger (13 <= 13)
12:00        Chưa có event nào
11:00        Chưa có event nào
10:00        ✓ Event 3 trigger (10 <= 10)
9:00         Chưa có event nào
8:00         Chưa có event nào
7:00         ✓ Event 2 trigger (7 <= 7)
6:00         Chưa có event nào
5:00         Chưa có event nào
4:00         ✓ Event 1 trigger (4 <= 4)
3:00         Chưa có event nào
2:00         Chưa có event nào
1:00         ✓ Event 0 trigger (1 <= 1)
0:00         Game kết thúc
```

---

## 🔍 Cách hoạt động

### Bước 1: Timer đếm xuống
```
20:00 → 19:59 → 19:58 → ... → 16:01 → 16:00
```

### Bước 2: Khi đạt 16:00
```
currentMinutes = 16.00
eventMinutes[5] = 16

Kiểm tra: currentMinutes <= eventMinutes[5]
          16.00 <= 16 ✓ TRUE
          
→ Event 5 trigger!
```

### Bước 3: Timer tiếp tục đếm xuống
```
16:00 → 15:59 → 15:58 → ... → 13:01 → 13:00
```

### Bước 4: Khi đạt 13:00
```
currentMinutes = 13.00
eventMinutes[4] = 13

Kiểm tra: currentMinutes <= eventMinutes[4]
          13.00 <= 13 ✓ TRUE
          
→ Event 4 trigger!
```

---

## 📈 Biểu đồ Timeline

```
Thời gian (phút)
20 ──────────────────────────────────────── 0
│                                          │
│                                          ▼
│  Event 5    Event 4    Event 3    Event 2    Event 1    Event 0
│  (16)       (13)       (10)       (7)        (4)        (1)
│   ▼          ▼          ▼          ▼          ▼          ▼
└──●──────────●──────────●──────────●──────────●──────────●──
  16         13         10         7          4          1
```

---

## 🔧 Code Logic

### Cũ (Sai - Count Up)
```csharp
if (currentMinutes >= eventMinutes[i] && !triggeredEvents.Contains(i))
{
    // Trigger event
}
```

### Mới (Đúng - Count Down)
```csharp
if (currentMinutes <= eventMinutes[i] && !triggeredEvents.Contains(i))
{
    // Trigger event
}
```

---

## 📝 Ví dụ chi tiết

### Scenario: Event 5 trigger

**Trước:**
```
currentMinutes = 16.00
eventMinutes[5] = 16

Kiểm tra: 16.00 >= 16 ✓ TRUE → Trigger ✓
```

**Sau:**
```
currentMinutes = 16.00
eventMinutes[5] = 16

Kiểm tra: 16.00 <= 16 ✓ TRUE → Trigger ✓
```

**Kết quả:** Cả hai đều trigger, nhưng logic khác nhau!

---

## ⚠️ Vấn đề nếu không sửa

### Nếu dùng `>=` với countdown timer

```
Thời gian    currentMinutes    eventMinutes[5]    currentMinutes >= 16?
─────────────────────────────────────────────────────────────────────
20:00        20.00            16                 20 >= 16 ✓ → Trigger!
19:00        19.00            16                 19 >= 16 ✓ → Trigger!
18:00        18.00            16                 18 >= 16 ✓ → Trigger!
17:00        17.00            16                 17 >= 16 ✓ → Trigger!
16:00        16.00            16                 16 >= 16 ✓ → Trigger!
15:00        15.00            16                 15 >= 16 ✗ → Không trigger
14:00        14.00            16                 14 >= 16 ✗ → Không trigger
```

**Vấn đề:** Event 5 trigger **ngay lập tức** khi game bắt đầu (20:00), không phải ở 16:00! ❌

---

## ✅ Giải pháp: Dùng `<=`

```
Thời gian    currentMinutes    eventMinutes[5]    currentMinutes <= 16?
─────────────────────────────────────────────────────────────────────
20:00        20.00            16                 20 <= 16 ✗ → Không trigger
19:00        19.00            16                 19 <= 16 ✗ → Không trigger
18:00        18.00            16                 18 <= 16 ✗ → Không trigger
17:00        17.00            16                 17 <= 16 ✗ → Không trigger
16:00        16.00            16                 16 <= 16 ✓ → Trigger! ✓
15:00        15.00            16                 15 <= 16 ✓ → Nhưng đã trigger
14:00        14.00            16                 14 <= 16 ✓ → Nhưng đã trigger
```

**Giải pháp:** Dùng `triggeredEvents.Contains(i)` để tránh trigger lại! ✓

---

## [object Object]ết luận

### Countdown Timer
- **Thời gian:** 20 → 0
- **Kiểm tra:** `currentMinutes <= eventMinutes[i]`
- **Trigger:** Khi lần đầu tiên vượt qua mốc (từ trên xuống)

### Count Up Timer
- **Thời gian:** 0 → 20
- **Kiểm tra:** `currentMinutes >= eventMinutes[i]`
- **Trigger:** Khi lần đầu tiên vượt qua mốc (từ dưới lên)

---

## 📊 Bảng so sánh

| Tính năng | Count Up | Count Down |
|----------|----------|-----------|
| Thời gian | 0 → 20 | 20 → 0 |
| Kiểm tra | >= | <= |
| Event 0 | Lúc 1 phút | Lúc 1 phút |
| Event 5 | Lúc 16 phút | Lúc 16 phút |
| Trigger | Từ dưới lên | Từ trên xuống |

---

## ✅ Hoàn thành

Code đã được sửa để hoạt động với **countdown timer**! 🎉

**Tất cả event sẽ trigger đúng thời gian theo timeline của bạn.**

---

## 🧪 Test

### Bước 1: Mở Console
Ctrl + Shift + C

### Bước 2: Chạy game

### Bước 3: Quan sát log
```
[EventEnemySpawner] CheckEventTrigger: 16.00 phút, isEventActive=False
[EventEnemySpawner] ✓ Trigger event 5 tại 16 phút!

[EventEnemySpawner] CheckEventTrigger: 13.00 phút, isEventActive=True
[EventEnemySpawner] ✓ Trigger event 4 tại 13 phút!
[EventEnemySpawner] Event đang chạy, thêm event 4 vào queue

[EventEnemySpawner] CheckEventTrigger: 10.00 phút, isEventActive=True
[EventEnemySpawner] ✓ Trigger event 3 tại 10 phút!
[EventEnemySpawner] Event đang chạy, thêm event 3 vào queue

...
```

### ✅ Kết quả mong đợi
- ✓ Event 5 trigger ở 16 phút
- ✓ Event 4 trigger ở 13 phút
- ✓ Event 3 trigger ở 10 phút
- ✓ Event 2 trigger ở 7 phút
- ✓ Event 1 trigger ở 4 phút
- ✓ Event 0 trigger ở 1 phút

---

**Chúc bạn thành công!** 🚀

