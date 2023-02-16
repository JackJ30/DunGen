extends RefCounted
class_name Delaunay3D

var vertices : Array[Vertex]
var edges : Array[Edge]
var triangles = []
var tetrahedra = []

func triangulate(verts):
	vertices = verts
	
	var minX = vertices[0].position.x
	var minY = vertices[0].position.y
	var minZ = vertices[0].position.z
	var maxX = minX
	var maxY = minY
	var maxZ = minZ
	
	for vertex in vertices:
		if (vertex.position.x < minX):	minX = vertex.position.x;
		if (vertex.position.x > maxX):	maxX = vertex.position.x;
		if (vertex.position.y < minY):	minY = vertex.position.y;
		if (vertex.position.y > maxY):	maxY = vertex.position.y;
		if (vertex.position.z < minZ):	minZ = vertex.position.z;
		if (vertex.position.z > maxZ):	maxZ = vertex.position.z;
	
	var dx = maxX - minX
	var dy = maxY - minY
	var dz = maxZ - minZ
	var delta_max = max(dx,dy,dz) * 2
	
	var p1 = Vertex.new(Vector3(minX - 1, minY - 1, minZ - 1))
	var p2 = Vertex.new(Vector3(maxX + delta_max, minY - 1, minZ - 1))
	var p3 = Vertex.new(Vector3(minX - 1, maxY + delta_max, minZ - 1))
	var p4 = Vertex.new(Vector3(minX - 1, minY - 1, maxZ + delta_max))
	
	tetrahedra.append(Tetrahedron.new(p1,p2,p3,p4))
	
	for vertex in vertices:
		var triangles2 = []
		
		for tetrahedron in tetrahedra:
			if tetrahedron.circumcircle_contains(vertex.position):
				tetrahedron.is_bad = true # COULD BE FUTURE ISSUE IF LOOP IS DUPLICATING (UNLIKELY)
				triangles2.append(Triangle.new(tetrahedron.a, tetrahedron.b, tetrahedron.c))
				triangles2.append(Triangle.new(tetrahedron.a, tetrahedron.b, tetrahedron.d))
				triangles2.append(Triangle.new(tetrahedron.a, tetrahedron.c, tetrahedron.d))
				triangles2.append(Triangle.new(tetrahedron.b, tetrahedron.c, tetrahedron.d))
		
		for i in range(triangles2.size()):
			for j in range(i + 1, triangles2.size()):
				if triangles2[i].equals(triangles2[j]):
					triangles2[i].is_bad = true
					triangles2[j].is_bad = true
		
		tetrahedra = tetrahedra.filter(func(n): return !n.is_bad)
		triangles2 = triangles2.filter(func(n): return !n.is_bad)
		
		for triangle in triangles2:
			tetrahedra.append(Tetrahedron.new(triangle.u, triangle.v, triangle.w, vertex))
	
	tetrahedra = tetrahedra.filter(func(n): return !(n.contains_vertex(p1) || n.contains_vertex(p2) || n.contains_vertex(p3) || n.contains_vertex(p4)))
	
	for tetrahedron in tetrahedra:
		var abc = Triangle.new(tetrahedron.a, tetrahedron.b, tetrahedron.c)
		var abd = Triangle.new(tetrahedron.a, tetrahedron.b, tetrahedron.d)
		var acd = Triangle.new(tetrahedron.a, tetrahedron.c, tetrahedron.d)
		var bcd = Triangle.new(tetrahedron.b, tetrahedron.c, tetrahedron.d)
		
		if !triangles.has(abc): triangles.append(abc)
		if !triangles.has(abd): triangles.append(abd)
		if !triangles.has(acd): triangles.append(acd)
		if !triangles.has(bcd): triangles.append(bcd)
		
		var ab = Edge.new(tetrahedron.a, tetrahedron.b)
		var bc = Edge.new(tetrahedron.b, tetrahedron.c)
		var ca = Edge.new(tetrahedron.c, tetrahedron.a)
		var da = Edge.new(tetrahedron.d, tetrahedron.a)
		var db = Edge.new(tetrahedron.d, tetrahedron.b)
		var dc = Edge.new(tetrahedron.d, tetrahedron.c)
		
		if !edges.has(ab): edges.append(ab)
		if !edges.has(bc): edges.append(bc)
		if !edges.has(ca): edges.append(ca)
		if !edges.has(da): edges.append(da)
		if !edges.has(db): edges.append(db)
		if !edges.has(dc): edges.append(dc)

class Vertex:
	var position : Vector3
	var data
	
	func _init(position : Vector3 = Vector3(0,0,0)):
		self.position = position
		
	func equals(other : Vertex) -> bool:
		return other.position.is_equal_approx(position)

class Edge:
	var u : Vertex
	var v : Vertex
	var is_bad : bool
	var distance : float
	
	func _init(u : Vertex = Vertex.new(), v : Vertex = Vertex.new()):
		self.u = u
		self.v = v
		distance = u.position.distance_to(v.position)
	
	static func Minimum_Spanning_Tree(edges : Array[Edge], start : Vertex) -> Array[Edge]:
		var open_set = []
		var closed_set = []
		
		for edge in edges:
			open_set.append(edge.u)
			open_set.append(edge.v)
		
		closed_set.append(start)
		
		var results : Array[Edge]
		
		while open_set.size() > 0:
			var chosen = false
			var chosen_edge : Edge = null
			var min_weight = INF
			
			for edge in edges:
				var closed_vertices = 0
				if !closed_set.has(edge.u): closed_vertices += 1
				if !closed_set.has(edge.v): closed_vertices += 1
				if closed_vertices != 1: continue
			
				if (edge.distance < min_weight):
					chosen_edge = edge
					chosen = true
					min_weight = edge.distance
			
			if !chosen: break
			
			results.append(chosen_edge)
			open_set.erase(chosen_edge.u)
			open_set.erase(chosen_edge.v)
			closed_set.append(chosen_edge.u)
			closed_set.append(chosen_edge.v)

		return results
	
	func equals(other : Edge) -> bool:
		return (u.equals(other.u) || u.equals(other.v)) && (v.equals(other.u) || v.equals(other.v))
	
	func notequals(other : Edge) -> bool:
		return !equals(other)
	
class Triangle:
	var u : Vertex
	var v : Vertex
	var w : Vertex
	var is_bad : bool
	
	func _init(u : Vertex = Vertex.new(), v : Vertex = Vertex.new(), w : Vertex = Vertex.new()):
		self.u = u
		self.v = v
		self.w = w
	
	func equals(other : Triangle) -> bool:
		return (u.equals(other.u) || u.equals(other.v) || u.equals(other.w)) && (v.equals(other.u) || v.equals(other.v) || v.equals(other.w)) && (w.equals(other.u) || w.equals(other.v) || w.equals(other.w))
	
	func notequals(other : Triangle) -> bool:
		return !equals(other)

class Tetrahedron:
	var a : Vertex
	var b : Vertex
	var c : Vertex
	var d : Vertex
	var is_bad : bool
	
	var circumcenter : Vector3
	var circumradius_squared : float
	
	func _init(a : Vertex = Vertex.new(), b : Vertex = Vertex.new(), c : Vertex = Vertex.new(), d : Vertex = Vertex.new()):
		self.a = a
		self.b = b
		self.c = c
		self.d = d
		
		calculate_circumsphere()
	
	func calculate_circumsphere():
		var det1 = MatrixMath.determinant_4x4(
			Vector4(a.position.x,b.position.x,c.position.x,d.position.x),
			Vector4(a.position.y,b.position.y,c.position.y,d.position.y),
			Vector4(a.position.z,b.position.z,c.position.z,d.position.z),
			Vector4.ONE
		)
		
		var a_pos_sqr = a.position.length_squared()
		var b_pos_sqr = b.position.length_squared()
		var c_pos_sqr = c.position.length_squared()
		var d_pos_sqr = d.position.length_squared()
		
		var Dx = MatrixMath.determinant_4x4(
			Vector4(a_pos_sqr,b_pos_sqr,c_pos_sqr,d_pos_sqr),
			Vector4(a.position.y,b.position.y,c.position.y,d.position.y),
			Vector4(a.position.z,b.position.z,c.position.z,d.position.z),
			Vector4.ONE
		)
		
		var Dy = -1.0 * MatrixMath.determinant_4x4(
			Vector4(a_pos_sqr,b_pos_sqr,c_pos_sqr,d_pos_sqr),
			Vector4(a.position.x,b.position.x,c.position.x,d.position.x),
			Vector4(a.position.z,b.position.z,c.position.z,d.position.z),
			Vector4.ONE
		)
		
		var Dz = MatrixMath.determinant_4x4(
			Vector4(a_pos_sqr,b_pos_sqr,c_pos_sqr,d_pos_sqr),
			Vector4(a.position.x,b.position.x,c.position.x,d.position.x),
			Vector4(a.position.y,b.position.y,c.position.y,d.position.y),
			Vector4.ONE
		)
		
		var det2 = MatrixMath.determinant_4x4(
			Vector4(a_pos_sqr,b_pos_sqr,c_pos_sqr,d_pos_sqr),
			Vector4(a.position.x,b.position.x,c.position.x,d.position.x),
			Vector4(a.position.y,b.position.y,c.position.y,d.position.y),
			Vector4(a.position.z,b.position.z,c.position.z,d.position.z)
		)
		
		circumcenter = Vector3(
			Dx / (2.0 * det1),
			Dy / (2.0 * det1),
			Dz / (2.0 * det1)
		)
		
		circumradius_squared = ((Dx * Dx) + (Dy * Dy) + (Dz * Dz) - (4.0 * det1 * det2)) / (4.0 * det1 * det1) * 1.0
	
	func contains_vertex(other : Vertex) -> bool:
		return a.equals(other) || b.equals(other) || c.equals(other) || d.equals(other)
		
	func circumcircle_contains(position : Vector3) -> bool:
		var dist = position - circumcenter
		return dist.length_squared() <= circumradius_squared
	
	func equals(other : Tetrahedron) -> bool:
		return (
			(a.equals(other.a) || a.equals(other.b) || a.equals(other.c) || a.equals(other.d)) &&
			(b.equals(other.a) || b.equals(other.b) || b.equals(other.c) || b.equals(other.d)) &&
			(c.equals(other.a) || c.equals(other.b) || c.equals(other.c) || c.equals(other.d)) &&
			(d.equals(other.a) || d.equals(other.b) || d.equals(other.c) || d.equals(other.d))
		)
		
	func notequals(other : Tetrahedron) -> bool:
		return !equals(other)
