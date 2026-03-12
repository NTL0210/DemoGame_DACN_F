using UnityEngine;

/// <summary>
/// Điều khiển một pointer duy nhất bay quanh Player theo 8 hướng từ input.
/// - Đặt script này trên GameObject Player (hoặc bất kỳ đối tượng nào) và gán "pointer" là con trỏ cần quay quanh.
/// - Gọi SetInput từ hệ thống di chuyển (PlayerMoveNew) để cập nhật hướng.
/// </summary>
public class PointerController : MonoBehaviour
{
	[Header("Pointer settings")]
	[SerializeField] private Transform pointer; // đối tượng mũi tên duy nhất (pointer 1)
	[SerializeField] private float radius = 1.5f; // bán kính quay quanh player
	[SerializeField] private float snapThreshold = 0.15f; // ngưỡng coi như không có input

	[System.Serializable]
	private enum PositionMode { Radius, Table }
	[SerializeField] private PositionMode positionMode = PositionMode.Table;

	[Header("Table mode (local positions around player)")]
	[SerializeField] private Vector3[] localPositions = new Vector3[8]
	{
		new Vector3(0f, 0f, 0f),        // 1
		new Vector3(0.43f, 1.07f, 0f),  // 2
		new Vector3(1.47f, 1.46f, 0f),  // 3
		new Vector3(2.56f, 1.07f, 0f),  // 4
		new Vector3(2.96f, -0.02f, 0f), // 5
		new Vector3(2.52f, -1.10f, 0f), // 6
		new Vector3(1.47f, -1.49f, 0f), // 7
		new Vector3(0.40f, -1.08f, 0f)  // 8
	};

	[SerializeField] private float[] zAnglesTable = new float[8]
	{
		0f,   // 1
		-45f, // 2
		-90f, // 3
		-135f, // 4
		-180f,// 5
		135f, // 6
		90f,  // 7
		45f   // 8
	};

	// Z rotation cho 8 hướng khi dùng Radius mode
	// Map index (1..8) -> zEuler
	private readonly float[] zAnglesByIndex = new float[9]
	{
		0f,
		0f,     // 1
		-45f,   // 2
		-90f,   // 3
		-135f,   // 4
		-180f,  // 5
		135f,   // 6
		90f,    // 7
		45f     // 8
	};

	private Vector2 lastInput; // lưu lại để khi input = 0 vẫn giữ hướng trước đó

	/// <summary>
	/// Nhận input di chuyển (Vector2 từ Input System) để điều khiển pointer.
	/// </summary>
	public void SetInput(Vector2 input)
	{
		lastInput = input;
	}

	private void Reset()
	{
		// Nếu bỏ trống, cố gắng tìm con trỏ theo tên thường dùng
		if (pointer == null)
		{
			var child = transform.Find("Pointer") ?? transform.Find("pointer") ?? transform.Find("pointer 1");
			if (child != null) pointer = child;
		}
	}

	private void LateUpdate()
	{
		if (pointer == null) return;

		Vector2 input = lastInput;
		if (input.magnitude < snapThreshold && input != Vector2.zero)
		{
			input.Normalize();
		}

		// Khi không có input, giữ nguyên vị trí/hướng hiện tại
		if (input.sqrMagnitude < snapThreshold * snapThreshold)
		{
			return;
		}

		// Quy đổi input -> 8 hướng theo thứ tự người dùng vẽ:
		// 1: (-1,0), 2: (-1,1), 3: (0,1), 4: (1,1), 5: (1,0), 6: (1,-1), 7: (0,-1), 8: (-1,-1)
		int dirIndex = GetEightDirectionIndex(input);

		if (positionMode == PositionMode.Radius)
		{
			// Tính vị trí tròn quanh player theo vector hướng đã snap
			Vector2 snappedDir = GetUnitVectorByIndex(dirIndex);
			Vector3 localOffset = new Vector3(snappedDir.x, snappedDir.y, 0f) * radius;
			SetPointerPositionLocal(localOffset);
			float z = zAnglesByIndex[dirIndex];
			SetPointerRotationZ(z);
		}
		else
		{
			// Dùng bảng vị trí/rotation do người dùng chỉ định
			int i = Mathf.Clamp(dirIndex - 1, 0, 7);
			SetPointerPositionLocal(localPositions[i]);
			SetPointerRotationZ(zAnglesTable[i]);
		}
	}

	private void SetPointerPositionLocal(Vector3 localPos)
	{
		if (pointer.parent == transform)
		{
			pointer.localPosition = localPos;
		}
		else
		{
			pointer.position = transform.position + localPos;
		}
	}

	private void SetPointerRotationZ(float z)
	{
		var euler = pointer.eulerAngles;
		pointer.rotation = Quaternion.Euler(euler.x, euler.y, z);
	}

	private static int GetEightDirectionIndex(Vector2 input)
	{
		// Ưu tiên chéo khi cả x,y đều có biên độ đáng kể
		float x = input.x;
		float y = input.y;
		const float dead = 0.2f;

		bool left = x <= -dead;
		bool right = x >= dead;
		bool down = y <= -dead;
		bool up = y >= dead;

		if (left && !up && !down) return 1;         // A
		if (left && up) return 2;                   // A+W
		if (!left && !right && up) return 3;       // W
		if (right && up) return 4;                  // W+D
		if (right && !up && !down) return 5;       // D
		if (right && down) return 6;                // D+S
		if (!left && !right && down) return 7;     // S
		if (left && down) return 8;                 // S+A

		// Nếu rơi ngoài ngưỡng, fallback theo góc
		float angle = Mathf.Atan2(y, x) * Mathf.Rad2Deg; // -180..180
		if (angle < -157.5f || angle >= 157.5f) return 1;      // left
		if (angle >= 112.5f) return 2;                         // up-left
		if (angle >= 67.5f) return 3;                          // up
		if (angle >= 22.5f) return 4;                          // up-right
		if (angle >= -22.5f) return 5;                         // right
		if (angle >= -67.5f) return 6;                         // down-right
		if (angle >= -112.5f) return 7;                        // down
		return 8;                                             // down-left
	}

	private static Vector2 GetUnitVectorByIndex(int index)
	{
		switch (index)
		{
			case 1: return new Vector2(-1f, 0f);
			case 2: return new Vector2(-1f, 1f).normalized;
			case 3: return new Vector2(0f, 1f);
			case 4: return new Vector2(1f, 1f).normalized;
			case 5: return new Vector2(1f, 0f);
			case 6: return new Vector2(1f, -1f).normalized;
			case 7: return new Vector2(0f, -1f);
			case 8: return new Vector2(-1f, -1f).normalized;
			default: return Vector2.left;
		}
	}
}


