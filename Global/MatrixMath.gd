extends Object
class_name MatrixMath

static func determinant_4x4(a : Vector4, b : Vector4, c : Vector4, d : Vector4) -> float:
	var dets = Vector4(0,0,0,0)
	dets.x = (a.x * determinant_3x3(Vector3(b.y,b.z,b.w),Vector3(c.y,c.z,c.w),Vector3(d.y,d.z,d.w)))
	dets.y = (a.y * determinant_3x3(Vector3(b.x,b.z,b.w),Vector3(c.x,c.z,c.w),Vector3(d.x,d.z,d.w)))
	dets.z = (a.z * determinant_3x3(Vector3(b.x,b.y,b.w),Vector3(c.x,c.y,c.w),Vector3(d.x,d.y,d.w)))
	dets.w = (a.w * determinant_3x3(Vector3(b.x,b.y,b.z),Vector3(c.x,c.y,c.z),Vector3(d.x,d.y,d.z)))
	
	return dets.x - dets.y + dets.z - dets.w
		
static func determinant_3x3(a : Vector3, b : Vector3, c : Vector3) -> float:
	return (a.x * determinant_2x2(Vector2(b.y, b.z),Vector2(c.y,c.z))) - (a.y * determinant_2x2(Vector2(b.x, b.z),Vector2(c.x,c.z))) + (a.z * determinant_2x2(Vector2(b.x, b.y),Vector2(c.x,c.y)))
	
static func determinant_2x2(a : Vector2, b : Vector2) -> float:
	return (a.x * b.y) - (a.y * b.x)
