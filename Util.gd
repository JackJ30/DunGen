extends Object
class_name Util

class DictionarySet:
	var array = {}

	func add(value):
		if (contains(value)):
			return false
		array[value] = 1
		return true
	
	func add_array(array):
		for value in array:
			add(value)

	func contains(value):
		return array.has(value)

	func size() -> int: return array.size()

	func clear():
		array.clear()
	
	func replace(items):
		clear()
		
		for item in items:
			add(item)
